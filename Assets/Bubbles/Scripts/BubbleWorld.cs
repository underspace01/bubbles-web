using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bubbles
{
    public sealed partial class BubbleWorld : MonoBehaviour
    {
        public BubbleTuning tuning;
        public Material filmMaterial, backgroundMaterial, dropletMaterial;
        public Mesh sphereMesh;
        public Texture2D[] environments;
        public string[] environmentNames =
        {
            "公園",
            "夜明けの山",
            "室内",
            "夜景"
        };
        public int BackgroundIndex { get; private set; }

        public Camera playCamera;
        public ParentSettings settingsUI;
        public bool SoundEnabled => true;
        public float SoundVolume => 1;
        public int BubbleLimit { get; private set; } = 20;
        public bool Paused { get; private set; }
        public float SimulationTime { get; private set; }
        public int RefillTargetCount { get; private set; }

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var b in pool)
                {
                    if (b.active)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public Vector2 PlaySize { get; private set; }
        public IReadOnlyList<Bubble> Pool => pool;

        public sealed class Bubble
        {
            public Transform transform;
            public Renderer renderer;
            public MaterialPropertyBlock properties;
            public bool active, growing;
            public Vector2 position, velocity;
            public float radius, phase, wobble, depth, tapTime, freeSince;
            public float growthElapsed;
            public int hits, popAfterTaps;
            public long birthOrder;
            public Vector2 windVelocity, visualOffset;
            public bool hasWind, drifting, returningSway;
            public bool shapeFrozen, hasShapeSnapshot;
            public float shapeBlend = 1, frozenShapeTime, frozenWobble, renderShapeTime, renderWobble;
            public Vector3 frozenStretch;
            public Quaternion frozenRotation;
        }

        public enum GestureMode
        {
            Pending,
            Growing,
            Wind
        }

        public sealed class Gesture
        {
            public Vector2 start, previous;
            public float started, last, maxDistance;
            public GestureMode mode;
            public Bubble target, created;
        }

        private readonly List<Bubble> pool = new();
        private readonly Dictionary<int, Gesture> gestures = new();
        private readonly Dictionary<int, int> touchSlots = new();
        private AudioSource audioSource, growthAudio;
        private AudioClip growClip, tapClip, popClip;
        public AudioSource GrowthAudio => growthAudio;
        public ParticleSystem Droplets => droplets;

        private ParticleSystem droplets;
        private GameObject backdrop;
        private float autoTime, pixelScale = 1;
        private long spawnSequence;
        private static readonly int ClockID = Shader.PropertyToID("_BubbleTime"), ShapeClockID = Shader.PropertyToID("_ShapeTime"), WobbleID = Shader.PropertyToID("_Wobble"), SeedID = Shader.PropertyToID("_Seed");
        private
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float BubbleCssScale();
        [DllImport("__Internal")] static extern float BubbleLoadSetting(int index,float fallback);
        [DllImport("__Internal")] static extern void BubbleSaveLimit(int count);
        [DllImport("__Internal")] static extern void BubbleSaveBackground(int index);
#endif
        void Awake()
        {
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : 60;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                BubbleLimit=Mathf.Clamp((int)BubbleLoadSetting(4,20),10,100);
#else
                BubbleLimit = Mathf.Clamp(PlayerPrefs.GetInt("bubble.limit", 20), 10, 100);
#endif
            }
            catch
            {
            }

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                BackgroundIndex=(int)BubbleLoadSetting(3,0);
#else
                BackgroundIndex = PlayerPrefs.GetInt("bubble.background", 0);
#endif
            }
            catch
            {
            }

            SetBackground(BackgroundIndex, false);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            growClip = MakeGrowthLoop();
            tapClip = MakeClip("Water wobble", .18f, 470, 330, false);
            popClip = MakeClip("Film pop", .15f, 680, 150, true);
            growthAudio = gameObject.AddComponent<AudioSource>();
            growthAudio.playOnAwake = false;
            growthAudio.spatialBlend = 0;
            growthAudio.loop = true;
            growthAudio.clip = growClip;
            for (int i = 0; i < 100; i++)
            {
                var go = new GameObject($"Soap bubble {i + 1:00}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform);
                go.GetComponent<MeshFilter>().sharedMesh = sphereMesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = filmMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                pool.Add(new Bubble { transform = go.transform, renderer = renderer, properties = new MaterialPropertyBlock() });
                go.SetActive(false);
            }

            backdrop = new GameObject("Quiet water gradient", typeof(MeshFilter), typeof(MeshRenderer));
            var quad = new Mesh
            {
                name = "Background quad",
                vertices = new[]
                {
                    new Vector3(-.5f, -.5f, 0),
                    new Vector3(-.5f, .5f, 0),
                    new Vector3(.5f, .5f, 0),
                    new Vector3(.5f, -.5f, 0)
                },
                uv = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                },
                triangles = new[]
                {
                    0,
                    1,
                    2,
                    0,
                    2,
                    3
                }
            };
            quad.RecalculateBounds();
            backdrop.GetComponent<MeshFilter>().sharedMesh = quad;
            backdrop.transform.SetParent(transform);
            backdrop.transform.position = new Vector3(0, 0, 5);
            backdrop.GetComponent<Renderer>().sharedMaterial = backgroundMaterial;
            BuildDroplets();
            Resize();
            ResetBubbles();
            settingsUI.Initialize(this);
        }

        private void Resize()
        {
            var previousSize = PlaySize;
            pixelScale = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            pixelScale=BubbleCssScale();
#endif
            PlaySize = new Vector2(Screen.width, Screen.height) * pixelScale;
            playCamera.orthographicSize = PlaySize.y / 200;
            backdrop.transform.localScale = new Vector3(PlaySize.x / 100, PlaySize.y / 100, 1);
            foreach (var b in pool)
            {
                if (b.active)
                {
                    if (previousSize.x > 0 && previousSize.y > 0)
                    {
                        b.position = b.position / previousSize * PlaySize;
                    }

                    b.radius = Mathf.Min(b.radius, MaximumRadius);
                    KeepInside(b);
                    ApplyTransform(b);
                }
            }
        }

        public float MaximumRadius => Mathf.Min(PlaySize.x * tuning.maximumDiameterWidthFraction * .5f, PlaySize.y * .44f / (1 + tuning.tapSquash));

        public Bubble Spawn(Vector2 position, float radius, bool growing = false)
        {
            while (ActiveCount >= BubbleLimit)
            {
                Bubble oldest = null;
                foreach (var item in pool)
                {
                    if (item.active && (oldest == null || item.birthOrder < oldest.birthOrder))
                    {
                        oldest = item;
                    }
                }

                if (oldest == null)
                {
                    break;
                }

                Pop(oldest);
            }

            var b = pool.Find(x => !x.active);
            if (b == null)
            {
                return null;
            }

            b.birthOrder = ++spawnSequence;
            b.active = true;
            b.growing = growing;
            b.position = position;
            b.radius = Mathf.Min(radius, MaximumRadius);
            b.velocity = Vector2.zero;
            b.windVelocity = b.visualOffset = Vector2.zero;
            b.hasWind = b.drifting = b.returningSway = false;
            b.phase = UnityEngine.Random.Range(0f, 6.283f);
            b.growthElapsed = 0;
            b.hits = 0;
            b.popAfterTaps = UnityEngine.Random.Range(1, 7);
            b.wobble = growing ? 0 : 1;
            b.tapTime = b.freeSince = SimulationTime;
            b.transform.gameObject.SetActive(true);
            b.shapeFrozen = b.hasShapeSnapshot = false;
            b.shapeBlend = 1;
            b.properties.SetFloat(SeedID, b.phase);
            b.properties.SetFloat("_FilmThickness", UnityEngine.Random.Range(320f, 520f));
            KeepInside(b);
            RefreshDepthOrder();
            return b;
        }

        public void ResetBubbles()
        {
            CancelGestures();
            foreach (var b in pool)
            {
                b.active = false;
                b.transform.gameObject.SetActive(false);
            }

            droplets.Clear();
            autoTime = 0;
            RefillTargetCount = UnityEngine.Random.Range(1, Mathf.Clamp(tuning.refillMaximumCount, 1, 3) + 1);
            SyncGrowthAudio();
        }

        public void SetBubbleLimit(int count, bool save = true)
        {
            BubbleLimit = Mathf.Clamp(count, 10, 100);
            if (!save)
            {
                return;
            }

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                BubbleSaveLimit(BubbleLimit);
#else
                PlayerPrefs.SetInt("bubble.limit", BubbleLimit);
                PlayerPrefs.Save();
#endif
            }
            catch
            {
            }
        }

        public void SetBackground(int index, bool save = true)
        {
            if (environments == null || environments.Length == 0)
            {
                return;
            }

            BackgroundIndex = Mathf.Clamp(index, 0, environments.Length - 1);
            float[] yaw =
            {
                .20f,
                .05f,
                .0f,
                .0f
            };
            float[] exposure =
            {
                -.3f,
                .2f,
                -.2f,
                .4f
            };
            foreach (var material in new[]
            {
                backgroundMaterial,
                filmMaterial
            }

            )
            {
                material.SetTexture("_HDRI", environments[BackgroundIndex]);
                material.SetFloat("_EnvironmentYaw", yaw[BackgroundIndex % yaw.Length]);
                material.SetFloat("_Exposure", exposure[BackgroundIndex % exposure.Length]);
                material.SetFloat("_ViewTanHalfFov", .85f);
            }

            if (!save)
            {
                return;
            }

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                BubbleSaveBackground(BackgroundIndex);
#else
                PlayerPrefs.SetInt("bubble.background", BackgroundIndex);
                PlayerPrefs.Save();
#endif
            }
            catch
            {
            }
        }

        public void SetPaused(bool paused)
        {
            CancelGestures();
            Paused = paused;
            foreach (var b in pool)
            {
                b.hasWind = false;
                b.windVelocity = Vector2.zero;
            }

            SyncGrowthAudio();
            if (paused)
            {
                droplets.Pause();
                audioSource.Stop();
            }
            else
            {
                droplets.Play();
            }
        }

        private void FinishGrowth(Bubble b)
        {
            if (!b.active || !b.growing)
            {
                return;
            }

            b.growing = false;
            b.freeSince = SimulationTime;
            Wobble(b);
        }

        private void Wobble(Bubble b)
        {
            b.wobble = 1;
            b.tapTime = SimulationTime;
            b.shapeFrozen = b.hasShapeSnapshot = false;
            b.shapeBlend = 1;
            ApplyTransform(b);
        }

        public void Tap(Bubble b)
        {
            if (!b.active)
            {
                return;
            }

            b.hits++;
            Wobble(b);
            if (b.hits >= b.popAfterTaps)
            {
                Pop(b);
            }
            else
            {
                ApplyTransform(b);
                PlaySound(tapClip, SizeAudioGain(b.radius));
            }
        }

        private void Pop(Bubble b)
        {
            if (!b.active)
            {
                return;
            }

            EmitPop(b);
            b.active = false;
            b.growing = false;
            b.transform.gameObject.SetActive(false);
            PlaySound(popClip, SizeAudioGain(b.radius));
            foreach (var gesture in gestures.Values)
            {
                if (gesture.created == b)
                {
                    gesture.created = null;
                    gesture.mode = GestureMode.Wind;
                }

                if (gesture.target == b)
                {
                    gesture.target = null;
                    gesture.mode = GestureMode.Wind;
                }
            }

            RefreshDepthOrder();
            SyncGrowthAudio();
        }

        private void Update()
        {
            float currentScale = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            currentScale=BubbleCssScale();
#endif
            if (Mathf.Abs(currentScale - pixelScale) > .0001f || Mathf.Abs(Screen.width * currentScale - PlaySize.x) > .5f || Mathf.Abs(Screen.height * currentScale - PlaySize.y) > .5f)
            {
                Resize();
            }

            PollInput();
            if (Paused)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, .04f), Time.unscaledTime);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelGestures();
                touchSlots.Clear();
                settingsUI.CancelHold();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelGestures();
                touchSlots.Clear();
                settingsUI.CancelHold();
            }
        }

        private void OnDestroy()
        {
            foreach (var clip in new[]
            {
                growClip,
                tapClip,
                popClip
            }

            )
            {
                if (clip)
                {
                    Destroy(clip);
                }
            }
        }
    }
}
