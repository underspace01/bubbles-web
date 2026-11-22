using System;
using Bubbles;
using UnityEngine;

public static class VerifyWindDisplay
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

        var centre = w.PlaySize * .5f;
        float dt = 1f / 60;
        var output = new System.Text.StringBuilder();
        foreach (var axis in new[]
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down
        }

        )
        {
            w.ResetBubbles();
            var small = w.Spawn(centre, w.tuning.minimumRadius);
            var large = w.Spawn(centre, w.MaximumRadius);
            small.phase = large.phase = 0;
            w.Tick(0, Time.unscaledTime);
            var beforeSmall = small.transform.position;
            var beforeLarge = large.transform.position;
            w.ApplyWind(centre - axis * 5, centre + axis * 5, dt);
            w.Tick(dt, Time.unscaledTime);
            float s = Vector2.Dot((Vector2)(small.transform.position - beforeSmall) * 100 / dt, axis), l = Vector2.Dot((Vector2)(large.transform.position - beforeLarge) * 100 / dt, axis);
            Check(s > l * 2.8f && l > 0, "Visible centre follows inertia in every direction");
            Check(Mathf.Abs(s - Vector2.Dot(small.velocity, axis)) < .01f && Mathf.Abs(l - Vector2.Dot(large.velocity, axis)) < .01f, "Displayed and physical velocity agree");
            Check(small.visualOffset == Vector2.zero && large.visualOffset == Vector2.zero, "No translation wobble while drifting");
            output.AppendLine($"{axis}: displayed {s:F3}/{l:F3}");
        }

        w.ResetBubbles();
        var b = w.Spawn(centre, 60);
        w.Tick(.13f, Time.unscaledTime);
        Check(b.visualOffset.magnitude > .1f, "Newborn translation wobble still exists at rest");
        var visible = b.transform.position;
        w.ApplyWind(centre - new Vector2(0, 5), centre + new Vector2(0, 5), dt);
        w.Tick(0, Time.unscaledTime);
        Check(Vector3.Distance(visible, b.transform.position) < .0001f, "Wind onset does not snap centre");
        b.popAfterTaps = 6;
        w.Tap(b);
        Check(b.wobble == 1 && b.transform.localScale.x / b.transform.localScale.y > 1.5f, "Tap squash retained during drift");
        Check(b.visualOffset == Vector2.zero, "Tap does not shift drifting centre");
        b.velocity = Vector2.zero;
        var stopped = b.transform.position;
        w.Tick(dt, Time.unscaledTime);
        Check(!b.drifting && b.visualOffset == Vector2.zero && Vector3.Distance(stopped, b.transform.position) < .0001f, "Still-air sway restarts without snapping");
        var anchor = b.position;
        Vector2 min = b.transform.position, max = min;
        for (int i = 0; i < 600; i++)
        {
            w.Tick(.02f, Time.unscaledTime);
            var p = (Vector2)b.transform.position;
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Check(b.position == anchor && b.velocity == Vector2.zero, "Idle sway does not move physical anchor");
        Check((max.x - min.x) * 100 > 8 && (max.y - min.y) * 100 > 8, "Idle sway returns after stopping");
        w.ResetBubbles();
        return $"{checks} checks passed\n" + output;
    }
}
