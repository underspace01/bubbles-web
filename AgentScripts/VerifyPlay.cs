using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyPlay
{
    public static string Run()
    {
        var world = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        if (!world)
        {
            throw new Exception("No live game");
        }

        int checks = 0;
        void Check(bool ok, string label)
        {
            if (!ok)
            {
                throw new Exception(label);
            }

            checks++;
        }

        world.ResetBubbles();
        Check(world.ActiveCount == 0, "Empty initial scene");
        world.Spawn(world.PlaySize * .5f, 30);
        var anchored = world.Pool.First(x => x.active);
        anchored.position = world.PlaySize * .5f;
        var anchor = anchored.position;
        for (int i = 0; i < 1200; i++)
        {
            world.Tick(1f / 60, Time.unscaledTime + i / 60f);
        }

        Check(Vector2.Distance(anchor, anchored.position) < .01f, "No idle translation or automatic ascent after 20 seconds");
        world.ApplyWind(anchor - new Vector2(0, 60), anchor + new Vector2(0, 60), .1f);
        world.Tick(.1f, 40);
        Check(anchored.position.y > anchor.y, "Upward wind raises bubble");
        for (int i = 0; i < 600; i++)
        {
            world.Tick(.02f, 40 + i * .02f);
        }

        var settled = anchored.position;
        world.Tick(.1f, 53);
        Check(anchored.velocity == Vector2.zero && anchored.position == settled, "Wind settles without upward force or edge kick");
        Check(world.environments.Length == 4 && world.environmentNames.Length == 4, "Four HDR backgrounds");
        int original = world.BackgroundIndex;
        for (int i = 0; i < 4; i++)
        {
            world.SetBackground(i, false);
            Check(world.environments[i].format == TextureFormat.RGBAHalf || world.environments[i].format == TextureFormat.RGB9e5Float, "HDR floating-point data retained");
            Check(world.backgroundMaterial.GetTexture("_HDRI") == world.environments[i] && world.filmMaterial.GetTexture("_HDRI") == world.environments[i], "HDR background and reflection switch together");
        }

        world.SetBackground(original, false);
        Check(world.filmMaterial.GetFloat("_IndexOfRefraction") > 1.3f && world.filmMaterial.GetFloat("_RefractionStrength") > .5f, "Visible refraction configured");
        world.ResetBubbles();
        world.Spawn(world.PlaySize * .5f, 30);
        Check(Mathf.Abs(BubbleWorld.SegmentDistance(new Vector2(5, 3), Vector2.zero, new Vector2(10, 0)) - 3) < .001f, "Swept wind geometry");
        var target = world.Pool.First(x => x.active);
        var point = target.position;
        int hits = target.hits;
        world.BeginPointer(91, point, 10);
        world.MovePointer(91, point + new Vector2(40, 0), 10.05f);
        world.MovePointer(91, point, 10.1f);
        world.EndPointer(91, point, 10.15f);
        Check(target.hits == hits, "Wind stays wind after returning to origin");
        world.ApplyWind(Vector2.zero, new Vector2(10000, 10000), .001f);
        world.Tick(.02f, Time.unscaledTime);
        Check(world.Pool.Where(x => x.active).All(x => x.velocity.magnitude <= world.tuning.maximumSpeed + .01f), "Wind speed cap");
        var growing = world.Spawn(world.PlaySize * .5f, 25, true);
        var velocity = growing.velocity;
        world.ApplyWind(Vector2.zero, world.PlaySize, .01f);
        world.Tick(.02f, Time.unscaledTime);
        Check(growing.velocity == velocity, "Growing bubble ignores wind");
        growing.growing = false;
        var p = world.PlaySize * new Vector2(.5f, .45f);
        for (int i = 0; i < 100 && world.Hit(p) != null; i++)
        {
            p = new Vector2(UnityEngine.Random.Range(30, world.PlaySize.x - 30), UnityEngine.Random.Range(30, world.PlaySize.y - 80));
        }

        int count = world.ActiveCount;
        world.BeginPointer(92, p, 20);
        world.Tick(.02f, 20.4f);
        Check(world.ActiveCount == count + 1, "Long hold creates bubble");
        var made = world.Pool.First(x => x.active && x.growing);
        float radius = made.radius;
        world.Tick(.1f, 20.5f);
        Check(made.radius > radius, "Hold grows bubble");
        world.EndPointer(92, p, 20.6f);
        Check(!made.growing, "Release detaches bubble");
        var before = world.SimulationTime;
        world.settingsUI.Open();
        world.Tick(.1f, 21);
        Check(world.Paused && world.SimulationTime == before, "Settings freeze simulation");
        world.BeginPointer(93, p, 21);
        world.settingsUI.Close();
        world.Tick(.02f, 22);
        Check(!world.Paused, "Resume simulation");
        var popped = world.Pool.First(x => x.active);
        int remaining = popped.popAfterTaps - popped.hits;
        for (int i = 0; i < remaining; i++)
        {
            world.Tap(popped);
        }

        Check(!popped.active, "Assigned random tap count pops bubble");
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>();
        Check(renderers.All(r => r.sharedMaterial != null && r.sharedMaterial.name != "Default-Material"), "No default materials");
        for (int i = 0; i < 100; i++)
        {
            world.Spawn(p, 25);
        }

        Check(world.ActiveCount == world.BubbleLimit, "Bounded bubble pool");
        world.ResetBubbles();
        Check(world.ActiveCount == 0, "Reset restores empty scene");
        world.Spawn(world.PlaySize * .5f, 30);
        var bubble = world.Pool.First(x => x.active);
        bubble.popAfterTaps = 6;
        world.Tap(bubble);
        float scale = bubble.transform.localScale.x / bubble.transform.localScale.y;
        Check(scale > 1.5f, "Tap gives a strong immediate squash");
        world.Tick(.1f, 30);
        float changed = bubble.transform.localScale.x / bubble.transform.localScale.y;
        Check(Mathf.Abs(changed - scale) > .2f, "Tap shape oscillates");
        world.Tick(4, 34);
        Check(bubble.wobble < .001f, "Tap motion settles");
        var labels = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include);
        Check(!labels.Any(t => t.text.Contains("保護者") || t.text.Contains("子さま") || t.text.Contains("子ども")), "Neutral settings copy");
        var randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(812);
        var counts = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 12; i++)
        {
            world.ResetBubbles();
            counts.Add(world.RefillTargetCount);
            Check(world.ActiveCount == 0 && world.RefillTargetCount >= 1 && world.RefillTargetCount <= 3, "Empty reset with random refill target");
        }

        UnityEngine.Random.state = randomState;
        Check(counts.Count > 1, "Refill target varies");
        Debug.Log($"[Bubble verification] {checks} checks passed: gestures, wind, growth, pause, pop, pool and rendering.");
        return $"{checks} checks passed";
    }
}
