using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyRefill
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int checks = 0;
        void Check(bool ok, string label)
        {
            if (!ok)
            {
                throw new Exception(label);
            }

            checks++;
        }

        w.ResetBubbles();
        Check(w.ActiveCount == 0, "Start empty");
        w.Tick(6.9f, Time.unscaledTime);
        Check(w.ActiveCount == 0, "No refill before seven seconds");
        w.Tick(.11f, Time.unscaledTime);
        Check(w.ActiveCount == 1, "First refill after seven seconds");
        var first = w.Pool.First(x => x.active);
        Check(first.radius >= w.MaximumRadius / 3 && first.radius <= w.MaximumRadius, "Refill size range");
        for (int i = 0; i < 30; i++)
        {
            w.ResetBubbles();
            w.Tick(7.01f, Time.unscaledTime);
            var b = w.Pool.First(x => x.active);
            Check(b.radius >= w.MaximumRadius / 3 && b.radius <= w.MaximumRadius, "Random refill size bounded");
            Check(b.wobble == 1 && b.hits == 0, "Refill starts with wobble and fresh hits");
        }

        w.ResetBubbles();
        w.settingsUI.Open();
        w.Tick(20, Time.unscaledTime);
        Check(w.ActiveCount == 0, "Settings pause refill");
        w.settingsUI.Close();
        w.ResetBubbles();
        float now = Time.unscaledTime;
        var point = w.PlaySize * .5f;
        w.BeginPointer(999, point, now);
        w.Tick(0, now + .4f);
        var growing = w.Pool.First(x => x.active && x.growing);
        float duration = (w.MaximumRadius - w.tuning.minimumRadius) / w.tuning.growthPerSecond;
        float previous = growing.radius, lastDelta = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            w.Tick(duration / 4, now + .4f + (i + 1) * duration / 4);
            float delta = growing.radius - previous;
            Check(delta > 0 && delta < lastDelta, "Growth slows each quarter");
            previous = growing.radius;
            lastDelta = delta;
        }

        Check(Mathf.Abs(growing.radius - w.MaximumRadius) < .01f, "Growth reaches maximum in finite time");
        w.EndPointer(999, point, now + duration + 1);
        int original = w.BackgroundIndex;
        w.SetBackground(3, false);
        Check(w.environments.Length == 4 && w.environments[3].format == TextureFormat.RGBAHalf, "Night is real HDR");
        Check(w.backgroundMaterial.GetTexture("_HDRI") == w.environments[3] && w.filmMaterial.GetTexture("_HDRI") == w.environments[3], "Night shared with reflections");
        Check(w.settingsUI.font.HasCharacters("夜景"), "Night label glyphs");
        w.SetBackground(original, false);
        w.ResetBubbles();
        return $"{checks} checks passed";
    }
}
