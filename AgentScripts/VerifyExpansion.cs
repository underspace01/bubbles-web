using System;
using System.Linq;
using System.Threading.Tasks;
using Bubbles;
using UnityEngine;

public static class VerifyExpansion
{
    public static async Task<string> Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition)
            {
                throw new Exception(label);
            }

            checks++;
        }

        Vector2 Empty()
        {
            for (int i = 0; i < 100; i++)
            {
                var candidate = new Vector2(UnityEngine.Random.Range(40, w.PlaySize.x - 40), UnityEngine.Random.Range(80, w.PlaySize.y - 100));
                if (w.Hit(candidate) == null)
                {
                    return candidate;
                }
            }

            throw new Exception("No empty area");
        }

        try
        {
            w.ResetBubbles();
            w.Spawn(w.PlaySize * .5f, 30);
            Check(w.Pool.Where(item => item.active).All(item => item.wobble > .9f && item.hits == 0), "New bubbles wobble without spending a tap");
            var p = Empty();
            float now = Time.unscaledTime;
            w.BeginPointer(810, p, now);
            w.Tick(.02f, now + .4f);
            var b = w.Pool.First(x => x.active && x.growing);
            Check(w.GrowthAudio.loop && w.GrowthAudio.isPlaying, "Inflation starts looping SE");
            float first = w.GrowthAudio.time;
            await Task.Delay(1300);
            Check(w.GrowthAudio.isPlaying && Mathf.Abs(w.GrowthAudio.time - first) > .01f, "Inflation SE continues past one clip length");
            var p2 = Empty();
            w.BeginPointer(811, p2, Time.unscaledTime);
            w.Tick(.02f, Time.unscaledTime + .4f);
            Check(w.Pool.Count(x => x.active && x.growing) == 2, "Two simultaneous inflations");
            w.EndPointer(810, p, Time.unscaledTime);
            Check(w.GrowthAudio.isPlaying, "Another finger keeps the loop playing");
            Check(!b.growing && b.wobble >= .99f && b.hits == 0, "Release starts tap-like wobble without incrementing hits");
            w.EndPointer(811, p2, Time.unscaledTime);
            Check(!w.GrowthAudio.isPlaying, "Last release stops loop immediately");
            var anchor = b.position;
            Vector2 min = b.transform.position, max = min;
            for (int i = 0; i < 600; i++)
            {
                w.Tick(.02f, Time.unscaledTime + i * .02f);
                Vector2 position = b.transform.position;
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            Check((max.x - min.x) * 100 > 8 && (max.y - min.y) * 100 > 8, "Visible horizontal and vertical sway");
            Check(b.position == anchor && b.velocity == Vector2.zero, "Sway remains bounded around the same anchor");
            p = Empty();
            w.BeginPointer(812, p, Time.unscaledTime);
            w.Tick(.02f, Time.unscaledTime + .4f);
            var grown = w.Pool.First(x => x.active && x.growing);
            w.Tick(20, Time.unscaledTime + 21);
            Check(Mathf.Abs(grown.radius - w.MaximumRadius) < .01f, "Growth reaches responsive maximum");
            Check(grown.radius * 2 <= w.PlaySize.x * (2f / 3) + .1f, "Diameter capped at two-thirds of screen width");
            Check(Mathf.Abs(w.MaximumRadius - Mathf.Min(w.PlaySize.x / 3, w.PlaySize.y * .44f / (1 + w.tuning.tapSquash))) < .01f, "Landscape height limit keeps bubble on screen");
            Check(w.SoundEnabled && w.SoundVolume == 1, "Sound is always on at full master volume");
            Check(w.GrowthAudio.isPlaying, "Inflation remains audible");
            w.SetPaused(true);
            Check(!w.GrowthAudio.isPlaying && !grown.growing, "Opening settings finishes growth and stops loop");
            w.SetPaused(false);
            w.ResetBubbles();
            var popped = w.Spawn(w.PlaySize * .5f, 60);
            int systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>().Length;
            w.Droplets.Clear();
            for (int i = 0; i < popped.popAfterTaps; i++)
            {
                w.Tap(popped);
            }

            Check(!popped.active && w.Droplets.particleCount >= 32, "Pop becomes a visible droplet burst");
            Check(w.Droplets.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name == "Bubbles/Water Droplet", "Water droplet shape and highlights used");
            var particles = new ParticleSystem.Particle[w.Droplets.main.maxParticles];
            int n = w.Droplets.GetParticles(particles);
            Check(particles.Take(n).All(x => x.startSize >= .05f && x.velocity.magnitude >= .8f), "Droplets have readable size and outward speed");
            for (int i = 0; i < 10; i++)
            {
                var extra = w.Spawn(w.PlaySize * .5f, 60);
                for (int tap = 0; tap < extra.popAfterTaps; tap++)
                {
                    w.Tap(extra);
                }
            }

            Check(UnityEngine.Object.FindObjectsByType<ParticleSystem>().Length == systems, "Repeated bursts reuse the same particle system");
            w.ResetBubbles();
            Check(!w.GrowthAudio.isPlaying && w.Droplets.particleCount == 0, "Reset clears sound and droplets");
            Debug.Log($"[Expansion verification] {checks} checks passed: continuous audio, newborn wobble, sway, responsive size and droplet bursts.");
            return $"{checks} checks passed";
        }
        finally
        {
            w.SetPaused(false);
            w.ResetBubbles();
        }
    }
}
