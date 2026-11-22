using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyTapLimit
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int original = w.BubbleLimit, checks = 0;
        void Check(bool ok, string label)
        {
            if (!ok)
            {
                throw new Exception(label);
            }

            checks++;
        }

        var p = w.PlaySize * .5f;
        float now = Time.unscaledTime;
        try
        {
            Check(w.BubbleLimit == 20, "Default limit twenty");
            Check(w.SoundEnabled && w.SoundVolume == 1, "Audio fixed on at full master volume");
            w.ResetBubbles();
            w.BeginPointer(701, p, now);
            w.EndPointer(701, p, now + .1f);
            var tap = w.Pool.Single(x => x.active);
            Check(tap.radius == w.tuning.minimumRadius && !tap.growing, "Empty tap makes minimum-sized bubble");
            Check(tap.wobble == 1, "Tap birth wobbles");
            tap.popAfterTaps = 6;
            w.BeginPointer(702, tap.position, now);
            w.EndPointer(702, tap.position, now + .1f);
            Check(w.ActiveCount == 1 && tap.hits == 1, "Existing bubble tap stays a tap");
            w.ResetBubbles();
            w.BeginPointer(703, p, now);
            w.MovePointer(703, p + new Vector2(40, 0), now + .1f);
            w.EndPointer(703, p, now + .2f);
            Check(w.ActiveCount == 0, "Wind creates no bubble");
            w.BeginPointer(704, p, now);
            w.EndPointer(704, p, now + .1f, true);
            Check(w.ActiveCount == 0, "Cancelled touch creates nothing");
            w.BeginPointer(705, p, now);
            w.Tick(.1f, now + .4f);
            w.EndPointer(705, p, now + .5f);
            Check(w.ActiveCount == 1 && w.Pool.Single(x => x.active).radius > w.tuning.minimumRadius, "Long hold still grows exactly one");
            w.SetBubbleLimit(1, false);
            Check(w.BubbleLimit == 10, "Minimum cap ten");
            w.ResetBubbles();
            for (int i = 0; i < 10; i++)
            {
                w.Spawn(p, 25);
            }

            long oldest = w.Pool.Where(x => x.active).Min(x => x.birthOrder);
            w.Droplets.Clear();
            w.Spawn(p, 25);
            Check(w.ActiveCount == 10 && !w.Pool.Any(x => x.active && x.birthOrder == oldest), "Oldest replaced at limit");
            Check(w.Droplets.particleCount >= 32, "Eviction bursts into droplets");
            w.SetBubbleLimit(999, false);
            Check(w.BubbleLimit == 100 && w.Pool.Count == 100, "Maximum cap and pool hundred");
            w.ResetBubbles();
            for (int i = 0; i < 120; i++)
            {
                w.Spawn(p, 25);
            }

            Check(w.ActiveCount == 100, "Hundred-bubble replacement works");
            var ids = w.Pool.Where(x => x.active).Select(x => x.birthOrder).OrderBy(x => x).ToArray();
            Check(ids.Last() - ids.First() == 99, "Newest hundred retained");
            w.SetBubbleLimit(10, false);
            Check(w.ActiveCount == 100, "Lower limit waits for next addition");
            w.Spawn(p, 25);
            Check(w.ActiveCount == 10, "Next addition enforces lowered limit");
            w.ResetBubbles();
            w.BeginPointer(706, p, now);
            w.Tick(.02f, now + .4f);
            for (int i = 0; i < 9; i++)
            {
                w.Spawn(p, 25);
            }

            var replacement = w.Spawn(p, 25, true);
            w.EndPointer(706, p, now + .6f);
            Check(replacement.active && replacement.growing, "Evicted pointer cannot finish reused bubble");
            w.ResetBubbles();
            w.SetBubbleLimit(20, false);
            w.settingsUI.Open();
            var labels = w.settingsUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            Check(!labels.Any(x => x.text.Contains("音量") || x.text.Contains("音を鳴らす")), "Audio controls removed");
            var slider = w.settingsUI.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Single();
            Check(slider.minValue == 10 && slider.maxValue == 100 && slider.wholeNumbers, "Integer count slider 10 to 100");
            Check(w.settingsUI.font.HasCharacters("上限個"), "Count-setting glyphs");
            return $"{checks} checks passed";
        }
        finally
        {
            w.settingsUI.Close();
            w.SetBubbleLimit(original, false);
            w.ResetBubbles();
        }
    }
}
