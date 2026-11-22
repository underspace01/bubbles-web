using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Runtime.InteropServices;

namespace Bubbles
{
    public sealed class ParentSettings : MonoBehaviour
    {
        public TMP_FontAsset font;
        public bool IsOpen => world != null && world.Paused;

        private BubbleWorld world;
        private RectTransform safeRoot, gear, panel, content;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern float BubbleSafeInset(int edge);
#endif
        private GameObject shade;
        private UnityEngine.UI.Image progress;
        private TextMeshProUGUI limitLabel;
        private UnityEngine.UI.Slider limit;
        private UnityEngine.UI.Button[] backgrounds;
        private float heldSince = -1;
        private Sprite disk;
        private Rect lastSafe;
        private Vector2 lastScreen;
        public void Initialize(BubbleWorld bubbleWorld)
        {
            world = bubbleWorld;
            disk = MakeDisk();
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("Touch UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            safeRoot = Rect("Safe area", transform);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            gear = Rect("Settings hold", safeRoot);
            gear.anchorMin = gear.anchorMax = gear.pivot = new Vector2(1, 1);
            gear.anchoredPosition = new Vector2(-14, -14);
            gear.sizeDelta = new Vector2(48, 48);
            var bg = gear.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = disk;
            bg.color = Palette.Horizon;
            var button = gear.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = bg;
            var hold = gear.gameObject.AddComponent<HoldSettingsButton>();
            hold.owner = this;
            var icon = Rect("Settings icon", gear);
            Stretch(icon, 11);
            var glyph = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
            glyph.sprite = MakeGear();
            glyph.color = Palette.Paper;
            glyph.raycastTarget = false;
            var ring = Rect("Hold progress", gear);
            Stretch(ring, 0);
            progress = ring.gameObject.AddComponent<UnityEngine.UI.Image>();
            progress.sprite = MakeRing();
            progress.type = UnityEngine.UI.Image.Type.Filled;
            progress.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            progress.fillOrigin = 2;
            progress.fillClockwise = true;
            progress.fillAmount = 0;
            progress.color = Palette.Mint;
            progress.raycastTarget = false;
            shade = Rect("Settings modal", transform).gameObject;
            Stretch((RectTransform)shade.transform, 0);
            var dim = shade.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(Palette.Deep.r * .4f, Palette.Deep.g * .4f, Palette.Deep.b * .4f, .78f);
            panel = Rect("Settings card", shade.transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(336, 568);
            var paper = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            paper.color = Palette.Paper;
            var viewport = Rect("Viewport", panel);
            Stretch(viewport, 0);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Palette.Paper;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(25, 25, 20, 20);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18;
            var brand = Text("Brand", content, "B U B B L E   P L A Y", 10, Palette.Horizon);
            Height(brand.gameObject, 16);
            var title = Text("Title", content, "設定", 23, Palette.Ink);
            Height(title.gameObject, 36);
            limitLabel = Text("Limit label", content, "", 13, Palette.Ink);
            Height(limitLabel.gameObject, 24);
            BuildSlider();
            var backgroundTitle = Text("Background title", content, "背景", 13, Palette.Ink);
            Height(backgroundTitle.gameObject, 24);
            backgrounds = new UnityEngine.UI.Button[world.environmentNames.Length];
            RectTransform backgroundRow = null;
            for (int i = 0; i < backgrounds.Length; i++)
            {
                if (i % 2 == 0)
                {
                    backgroundRow = Rect($"Background choices {i / 2}", content);
                    Height(backgroundRow.gameObject, 44);
                    var bgLayout = backgroundRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    bgLayout.spacing = 6;
                    bgLayout.childControlWidth = bgLayout.childControlHeight = bgLayout.childForceExpandWidth = true;
                }

                int choice = i;
                backgrounds[i] = Button($"Background {i}", backgroundRow, world.environmentNames[i], 44, Palette.Ink, Palette.Sky, out var label);
                label.fontSize = 12;
                backgrounds[i].onClick.AddListener(() =>
                {
                    world.SetBackground(choice);
                    Refresh();
                });
            }

            var hint = Text("Instructions", content, "空いているところをタップで玉が出ます。\n長押しで育て、なぞると風が起きます。", 11, Palette.Horizon);
            Height(hint.gameObject, 44);
            var reset = Button("Reset", content, "シャボン玉をリセット", 40, Palette.Ink, Palette.Paper, out _);
            reset.onClick.AddListener(world.ResetBubbles);
            var resume = Button("Resume", content, "あそびに戻る", 44, Palette.Paper, Palette.Ink, out _);
            resume.onClick.AddListener(Close);
            shade.SetActive(false);
            Refresh();
            UpdateSafeArea();
        }

        private void BuildSlider()
        {
            var root = Rect("Bubble limit slider", content);
            Height(root.gameObject, 28);
            limit = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            limit.minValue = 10;
            limit.maxValue = 100;
            limit.wholeNumbers = true;
            var track = Rect("Track", root);
            track.anchorMin = new Vector2(0, .4f);
            track.anchorMax = new Vector2(1, .6f);
            track.offsetMin = track.offsetMax = Vector2.zero;
            var image = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Palette.Sky;
            var area = Rect("Handle area", root);
            Stretch(area, 10);
            var handle = Rect("Handle", area);
            handle.sizeDelta = new Vector2(22, 22);
            var hi = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            hi.sprite = disk;
            hi.color = Palette.Horizon;
            limit.handleRect = handle;
            limit.targetGraphic = hi;
            limit.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            limit.onValueChanged.AddListener(v =>
            {
                world.SetBubbleLimit(Mathf.RoundToInt(v));
                limitLabel.text = $"シャボン玉の上限　{world.BubbleLimit}個";
            });
        }

        public bool ContainsScreenPoint(Vector2 screen) => IsOpen || (gear != null && RectTransformUtility.RectangleContainsScreenPoint(gear, screen));
        public void StartHold()
        {
            if (!IsOpen)
            {
                heldSince = Time.unscaledTime;
            }
        }

        public void CancelHold()
        {
            heldSince = -1;
            if (progress)
            {
                progress.fillAmount = 0;
            }
        }

        public void Open()
        {
            CancelHold();
            world.SetPaused(true);
            shade.SetActive(true);
            gear.gameObject.SetActive(false);
            content.anchoredPosition = Vector2.zero;
            Refresh();
        }

        public void Close()
        {
            shade.SetActive(false);
            gear.gameObject.SetActive(true);
            world.SetPaused(false);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (world == null)
            {
                return;
            }

            if (lastSafe != Screen.safeArea || lastScreen != new Vector2(Screen.width, Screen.height))
            {
                UpdateSafeArea();
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gear.gameObject;
                if (selected && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                {
                    StartHold();
                }

                if (keyboard.spaceKey.wasReleasedThisFrame || keyboard.enterKey.wasReleasedThisFrame)
                {
                    CancelHold();
                }

                if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
                {
                    Close();
                }
            }

            if (heldSince >= 0)
            {
                progress.fillAmount = (Time.unscaledTime - heldSince) / world.tuning.settingsHoldSeconds;
                if (progress.fillAmount >= 1)
                {
                    Open();
                }
            }
        }

        private void UpdateSafeArea()
        {
            lastSafe = Screen.safeArea;
            lastScreen = new Vector2(Screen.width, Screen.height);
            safeRoot.anchorMin = lastSafe.min / lastScreen;
            safeRoot.anchorMax = lastSafe.max / lastScreen;
#if UNITY_WEBGL && !UNITY_EDITOR
            var size=world.PlaySize;safeRoot.anchorMin=new Vector2(BubbleSafeInset(3)/size.x,BubbleSafeInset(2)/size.y);safeRoot.anchorMax=Vector2.one-new Vector2(BubbleSafeInset(1)/size.x,BubbleSafeInset(0)/size.y);
#endif
            var root = (RectTransform)transform;
            panel.sizeDelta = new Vector2(Mathf.Min(352, root.rect.width - 24), Mathf.Min(586, root.rect.height - 24));
        }

        private void Refresh()
        {
            limit.SetValueWithoutNotify(world.BubbleLimit);
            limitLabel.text = $"シャボン玉の上限　{world.BubbleLimit}個";
            if (backgrounds != null)
            {
                for (int i = 0; i < backgrounds.Length; i++)
                {
                    bool selected = world.BackgroundIndex == i;
                    backgrounds[i].GetComponent<UnityEngine.UI.Image>().color = selected ? Palette.Horizon : Palette.Sky;
                    backgrounds[i].GetComponentInChildren<TextMeshProUGUI>().color = selected ? Palette.Paper : Palette.Ink;
                }
            }
        }

        private RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin;
            rect.offsetMax = Vector2.one * -margin;
        }

        private static void Height(GameObject go, float value)
        {
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight = le.preferredHeight = value;
        }

        private TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            return t;
        }

        private UnityEngine.UI.Button Button(string name, Transform parent, string value, float height, Color textColor, Color bgColor, out TextMeshProUGUI label)
        {
            var r = Rect(name, parent);
            Height(r.gameObject, height);
            var im = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            im.color = bgColor;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = im;
            var colors = b.colors;
            colors.highlightedColor = new Color(.9f, .96f, .97f, 1);
            colors.pressedColor = new Color(.77f, .88f, .9f, 1);
            b.colors = colors;
            label = Text("Label", r, value, 13, textColor);
            Stretch(label.rectTransform, 0);
            label.alignment = TextAlignmentOptions.Center;
            return b;
        }

        private Sprite MakeDisk() => MakeSprite(false);
        private Sprite MakeRing() => MakeSprite(true);
        private Sprite MakeGear()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Settings gear";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + .5f - size * .5f, y + .5f - size * .5f);
                    float d = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float outer = 34 + 4 * Mathf.Cos(angle * 8);
                    float stroke = Mathf.Min(Mathf.Abs(d - outer) - 2, Mathf.Abs(d - 13) - 2);
                    var color = Palette.Window;
                    color.a = Mathf.Clamp01(.5f - stroke);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 96);
        }

        private Sprite MakeSprite(bool ring)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = ring ? "Hold ring" : "Soft circle";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
                    float alpha = ring ? Mathf.Clamp01(1 - Mathf.Abs(d - 44) / 1.7f) : Mathf.Clamp01(47 - d);
                    var color = Palette.Window;
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 96);
        }
    }
}
