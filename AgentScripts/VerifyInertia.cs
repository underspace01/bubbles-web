using System;
using Bubbles;
using UnityEngine;

public static class VerifyInertia
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
        var centre = w.PlaySize * .5f;
        var small = w.Spawn(centre, w.tuning.minimumRadius);
        var large = w.Spawn(centre, w.MaximumRadius);
        void Step(float dt, float swipeSpeed)
        {
            small.position = large.position = centre;
            var half = new Vector2(swipeSpeed * dt * .5f, 0);
            w.ApplyWind(centre - half, centre + half, dt);
            w.Tick(dt, Time.unscaledTime);
        }

        void Clear()
        {
            small.velocity = large.velocity = Vector2.zero;
            small.position = large.position = centre;
            small.visualOffset = large.visualOffset = Vector2.zero;
            small.drifting = large.drifting = true;
        }

        Check(Mathf.Approximately(w.InertiaFactor(small.radius), 1), "Small reference inertia");
        Check(Mathf.Approximately(w.InertiaFactor(large.radius), 3), "Large triple inertia");
        Step(1f / 60, 600);
        float initialSmall = small.velocity.x, initialLarge = large.velocity.x;
        Check(initialSmall > initialLarge * 2.8f && initialLarge > 0, "Large accelerates more slowly");
        for (int i = 1; i < 15; i++)
        {
            Step(1f / 60, 600);
        }

        float quarterSmall = small.velocity.x, quarterLarge = large.velocity.x;
        Check(quarterSmall > quarterLarge * 2, "Visible start-up difference after quarter second");
        bool ordered = true;
        for (int i = 15; i < 1200; i++)
        {
            Step(1f / 60, 600);
            ordered &= large.velocity.x <= small.velocity.x + .002f;
        }

        Check(ordered, "No size-speed inversion under continuous wind");
        float expectedSpeed = Mathf.Min(600 * w.tuning.windGain, w.tuning.maximumSpeed);
        Check(Mathf.Abs(small.velocity.x - expectedSpeed) < .01f && Mathf.Abs(large.velocity.x - expectedSpeed) < .01f, "大小とも速度上限を考慮した同じ風速に近づく");
        for (int i = 0; i < 15; i++)
        {
            Step(1f / 60, -600);
        }

        Check(small.velocity.x < 0 && large.velocity.x > 0, "Large responds later to direction change");
        Clear();
        ordered = true;
        bool capped = true;
        for (int i = 0; i < 600; i++)
        {
            Step(1f / 60, 2000);
            ordered &= large.velocity.x <= small.velocity.x + .002f;
            capped &= small.velocity.magnitude <= w.tuning.maximumSpeed + .001f && large.velocity.magnitude <= w.tuning.maximumSpeed + .001f;
        }

        Check(ordered, "No inversion at speed limit");
        Check(capped, "Limit applied after integration");
        var steadySmall = small.velocity.x;
        var steadyLarge = large.velocity.x;
        small.velocity = large.velocity = new Vector2(100, 0);
        for (int i = 0; i < 100; i++)
        {
            small.position = large.position = centre;
            w.Tick(.01f, Time.unscaledTime);
        }

        float coastSmall = small.velocity.x, coastLarge = large.velocity.x;
        Check(coastLarge > coastSmall * 2, "Large coasts longer in still air");
        for (int i = 0; i < 2000; i++)
        {
            small.position = large.position = centre;
            w.Tick(.01f, Time.unscaledTime);
        }

        Check(small.velocity == Vector2.zero && large.velocity == Vector2.zero, "Both eventually settle");
        Vector2 AtRate(int fps)
        {
            Clear();
            for (int i = 0; i < fps; i++)
            {
                Step(1f / fps, 600);
            }

            return new Vector2(small.velocity.x, large.velocity.x);
        }

        var rate30 = AtRate(30);
        var rate60 = AtRate(60);
        var rate120 = AtRate(120);
        Check(Vector2.Distance(rate30, rate60) < .01f && Vector2.Distance(rate60, rate120) < .01f, "30/60/120 fps yield same velocities");
        Clear();
        w.ApplyWind(centre - new Vector2(5, 0), centre + new Vector2(5, 0), 1f / 60);
        w.SetPaused(true);
        w.SetPaused(false);
        w.Tick(.02f, Time.unscaledTime);
        Check(small.velocity == Vector2.zero && large.velocity == Vector2.zero, "Pause discards pending wind");
        small.velocity = large.velocity = new Vector2(100, 0);
        w.ApplyWind(centre, centre, .1f);
        w.Tick(.1f, Time.unscaledTime);
        Check(small.velocity.x > 80 && large.velocity.x > 90, "Stationary pointer allows normal coasting");
        var growing = w.Spawn(centre, 30, true);
        Step(.02f, 600);
        Check(growing.velocity == Vector2.zero && !growing.hasWind, "Growing bubbles ignore wind");
        w.ResetBubbles();
        return $"{checks} checks passed; first frame {initialSmall:F3}/{initialLarge:F3}; 0.25s {quarterSmall:F3}/{quarterLarge:F3}; capped {steadySmall:F3}/{steadyLarge:F3}; 1s coasting {coastSmall:F3}/{coastLarge:F3}; 1s at 60fps {rate60}";
    }
}
