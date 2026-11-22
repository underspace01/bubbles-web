using System;
using Bubbles;
using UnityEngine;

public static class VerifyWindRange
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        var original = w.PlaySize;
        int checks = 0;
        var sizeProperty = typeof(BubbleWorld).GetProperty("PlaySize");
        void Check(bool ok, string message)
        {
            if (!ok)
            {
                throw new Exception(message);
            }

            checks++;
        }

        try
        {
            foreach (var size in new[]
            {
                new Vector2(390, 844),
                new Vector2(844, 390),
                new Vector2(1200, 800)
            }

            )
            {
                w.ResetBubbles();
                sizeProperty.SetValue(w, size);
                var centre = size * .5f;
                float radius = w.WindInfluenceRadius;
                Check(Mathf.Approximately(radius, Mathf.Min(size.x, size.y) / 4), "Radius tracks short side");
                var b = w.Spawn(centre, w.tuning.minimumRadius);
                bool queued = false;
                float Sample(float distance)
                {
                    b.position = centre;
                    b.velocity = Vector2.zero;
                    b.visualOffset = Vector2.zero;
                    b.windVelocity = Vector2.zero;
                    b.hasWind = false;
                    b.drifting = true;
                    var line = centre - new Vector2(0, distance);
                    w.ApplyWind(line - new Vector2(5, 0), line + new Vector2(5, 0), 1f / 60);
                    queued = b.hasWind;
                    w.Tick(1f / 60, Time.unscaledTime);
                    return b.velocity.x;
                }

                float near = Sample(0), half = Sample(radius * .5f);
                Check(near > 0 && half > 0 && half < near, "Distance falloff preserved");
                Check(Mathf.Abs(half / near - .5f) < .001f, "Linear half-range influence");
                Check(Sample(radius) == 0, "Radius boundary receives no force");
                Check(Sample(radius * 1.01f) == 0 && !queued, "Outside radius receives no force");
                Sample(radius * .99f);
                Check(queued, "Inside boundary receives force even below settling velocity cutoff");
            }

            return $"{checks} checks passed; 390x844 and 844x390 radius = 97.5px";
        }
        finally
        {
            sizeProperty.SetValue(w, original);
            w.ResetBubbles();
        }
    }
}
