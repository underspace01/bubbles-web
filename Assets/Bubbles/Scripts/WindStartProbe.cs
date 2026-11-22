#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Bubbles
{
    // ローカル開発用の測定。通常のプレイヤービルドには含めない。
    public sealed class WindStartProbe : MonoBehaviour
    {
        [Serializable]
        public sealed class Sample
        {
            public string direction;
            public float age, smallCentre, largeCentre, smallRim, largeRim;
        }

        [Serializable]
        public sealed class Report
        {
            public bool passed;
            public string error;
            public int width, height;
            public float transmissionScale, coastSmall, coastLarge;
            public List<Sample> samples = new();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BubbleWindProbeResult(string json);
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("Local Wind Probe").AddComponent<WindStartProbe>();
        }

        public void Run(string unused)
        {
            var report = Measure(UnityEngine.Object.FindAnyObjectByType<BubbleWorld>());
            var json = JsonUtility.ToJson(report);
#if UNITY_WEBGL && !UNITY_EDITOR
            BubbleWindProbeResult(json);
#else
            Debug.Log(json);
#endif
        }

        public static Report Measure(BubbleWorld w)
        {
            var report = new Report
            {
                width = Mathf.RoundToInt(w.PlaySize.x),
                height = Mathf.RoundToInt(w.PlaySize.y),
                transmissionScale = w.filmMaterial.GetFloat("_TransmissionScale")
            };
            bool enabled = w.enabled;
            w.enabled = false;
            try
            {
                var centre = w.PlaySize * .5f;
                const float dt = 1f / 60;
                float Edge(BubbleWorld.Bubble b, Vector2 axis)
                {
                    var box = b.renderer.bounds;
                    return Vector2.Dot(box.center, axis) + Vector2.Dot(box.extents, new Vector2(Mathf.Abs(axis.x), Mathf.Abs(axis.y)));
                }

                foreach (var axis in new[]
                {
                    Vector2.right,
                    Vector2.left,
                    Vector2.up,
                    Vector2.down
                }

                )
                {
                    foreach (float age in new[]
                    {
                        0f,
                        .13f,
                        .65f,
                        1.6f
                    }

                    )
                    {
                        w.ResetBubbles();
                        var small = w.Spawn(centre, w.tuning.minimumRadius);
                        var large = w.Spawn(centre, w.MaximumRadius);
                        small.phase = large.phase = 0;
                        w.Tick(age, Time.unscaledTime);
                        float s0 = Edge(small, axis), l0 = Edge(large, axis);
                        var ss = small.transform.localScale;
                        var ls = large.transform.localScale;
                        var sr = small.transform.localRotation;
                        var lr = large.transform.localRotation;
                        float shapeTime = large.renderShapeTime;
                        w.ApplyWind(centre - axis * 5, centre + axis * 5, dt);
                        w.Tick(dt, Time.unscaledTime);
                        var sample = new Sample
                        {
                            direction = axis.ToString(),
                            age = age,
                            smallCentre = Vector2.Dot(small.velocity, axis),
                            largeCentre = Vector2.Dot(large.velocity, axis),
                            smallRim = (Edge(small, axis) - s0) * 100 / dt,
                            largeRim = (Edge(large, axis) - l0) * 100 / dt
                        };
                        report.samples.Add(sample);
                        if (!(sample.smallRim > sample.largeRim && sample.largeRim > 0))
                        {
                            throw new Exception("Rim velocity inverted: " + JsonUtility.ToJson(sample));
                        }

                        if (Mathf.Abs(sample.smallCentre - sample.smallRim) > .04f || Mathf.Abs(sample.largeCentre - sample.largeRim) > .04f)
                        {
                            throw new Exception("Rim drift differs from centre");
                        }

                        if (Vector3.Distance(ss, small.transform.localScale) > .00001f || Vector3.Distance(ls, large.transform.localScale) > .00001f || Quaternion.Angle(sr, small.transform.localRotation) > .001f || Quaternion.Angle(lr, large.transform.localRotation) > .001f || large.renderShapeTime != shapeTime)
                        {
                            throw new Exception("Automatic shape moved at wind onset");
                        }
                    }
                }

                w.ResetBubbles();
                var a = w.Spawn(centre, w.tuning.minimumRadius);
                var b = w.Spawn(centre, w.MaximumRadius);
                a.velocity = b.velocity = new Vector2(100, 0);
                for (int i = 0; i < 100; i++)
                {
                    a.position = b.position = centre;
                    w.Tick(.01f, Time.unscaledTime);
                }

                report.coastSmall = a.velocity.x;
                report.coastLarge = b.velocity.x;
                if (report.coastLarge <= report.coastSmall)
                {
                    throw new Exception("Coasting inertia changed");
                }

                report.passed = true;
            }
            catch (Exception e)
            {
                report.error = e.Message;
            }
            finally
            {
                w.ResetBubbles();
                w.enabled = enabled;
            }

            return report;
        }
    }
}
#endif
