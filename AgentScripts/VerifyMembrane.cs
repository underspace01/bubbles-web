using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyMembrane
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value)
            {
                throw new Exception(label);
            }

            checks++;
        }

        Check(w.SizeAudioGain(w.tuning.minimumRadius) > 0, "Small bubble is audible");
        Check(w.SizeAudioGain(w.MaximumRadius) > w.SizeAudioGain(w.tuning.minimumRadius) * 3, "Large bubble is louder");
        float previous = 0;
        for (int i = 0; i <= 10; i++)
        {
            float gain = w.SizeAudioGain(Mathf.Lerp(w.tuning.minimumRadius, w.MaximumRadius, i / 10f));
            Check(gain >= previous, "Gain grows monotonically");
            previous = gain;
        }

        Check(previous <= 1, "Gain is bounded");
        w.ResetBubbles();
        var b = w.Spawn(w.PlaySize * .5f, w.tuning.minimumRadius, true);
        w.Tick(0, Time.unscaledTime);
        float quiet = w.GrowthAudio.volume;
        b.radius = w.MaximumRadius;
        w.Tick(0, Time.unscaledTime);
        Check(w.GrowthAudio.volume > quiet * 3, "Actual inflation source grows louder");
        b.growing = false;
        w.ResetBubbles();
        Check(Mathf.Approximately(w.WindInfluenceRadius, Mathf.Min(w.PlaySize.x, w.PlaySize.y) * .25f), "Quarter-short-side wind radius");
        b = w.Spawn(w.PlaySize * .5f, 30);
        b.position = w.PlaySize * .5f;
        b.velocity = Vector2.zero;
        var point = b.position - new Vector2(0, w.WindInfluenceRadius * .5f);
        w.ApplyWind(point - new Vector2(20, 0), point + new Vector2(20, 0), .1f);
        w.Tick(.1f, Time.unscaledTime);
        Check(b.velocity.x > 0, "Wind reaches within local radius");
        Check(w.filmMaterial.GetFloat("_PatternStrength") > .5f, "Visible membrane pattern enabled");
        w.ResetBubbles();
        Debug.Log($"[Membrane verification] {checks} checks passed");
        return $"{checks} checks passed";
    }
}
