using UnityEngine;

namespace Bubbles
{
    // 膨張音・揺れ音・破裂音と雫を生成する。
    public sealed partial class BubbleWorld
    {
        private void BuildDroplets()
        {
            var go = new GameObject("Pooled film droplets");
            go.transform.SetParent(transform);
            droplets = go.AddComponent<ParticleSystem>();
            var main = droplets.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = .85f;
            main.startSpeed = 0;
            main.startSize = .08f;
            main.maxParticles = 3072;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .22f;
            var emission = droplets.emission;
            emission.enabled = false;
            var shape = droplets.shape;
            shape.enabled = false;
            var size = droplets.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 1), new Keyframe(.45f, .85f), new Keyframe(1, 0)));
            var color = droplets.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Palette.Window, 0), new GradientColorKey(Palette.Mint, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .5f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = dropletMaterial;
            droplets.Play();
        }

        private void EmitPop(Bubble b)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(b.radius * .5f), 32, 72);
            for (int i = 0; i < count; i++)
            {
                float a = i * 6.283185f / count + UnityEngine.Random.Range(-.04f, .04f);
                var direction = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var emit = new ParticleSystem.EmitParams
                {
                    position = b.transform.position + direction * b.radius / 100 * UnityEngine.Random.Range(.65f, 1),
                    velocity = direction * UnityEngine.Random.Range(.85f, 2.1f),
                    startSize = UnityEngine.Random.Range(.055f, .11f),
                    startLifetime = UnityEngine.Random.Range(.65f, 1.05f),
                    rotation = UnityEngine.Random.Range(-40f, 40f),
                    startColor = Color.Lerp(Palette.Mint, Palette.Window, UnityEngine.Random.value)
                };
                droplets.Emit(emit, 1);
            }
        }

        private void SyncGrowthAudio()
        {
            if (!growthAudio)
            {
                return;
            }

            int count = 0;
            float radius = 0, gainSquares = 0;
            foreach (var b in pool)
            {
                if (b.active && b.growing)
                {
                    count++;
                    radius += b.radius;
                    float gain = SizeAudioGain(b.radius);
                    gainSquares += gain * gain;
                }
            }

            if (Paused || !SoundEnabled || SoundVolume <= 0 || count == 0)
            {
                growthAudio.Stop();
                return;
            }

            growthAudio.volume = SoundVolume * .32f * Mathf.Min(1.3f, Mathf.Sqrt(gainSquares));
            growthAudio.pitch = Mathf.Lerp(1.1f, .84f, Mathf.Clamp01(radius / count / Mathf.Max(MaximumRadius, 1)));
            if (!growthAudio.isPlaying)
            {
                growthAudio.Play();
            }
        }

        private static AudioClip MakeGrowthLoop()
        {
            const int hz = 44100;
            var samples = new float[hz];
            for (int i = 0; i < hz; i++)
            {
                float t = (float)i / hz;
                float phase = 6.283185f * 220 * t + 1.3f * Mathf.Sin(6.283185f * 3 * t);
                float air = Mathf.Sin(6.283185f * 733 * t) * .11f + Mathf.Sin(6.283185f * 1093 * t) * .07f;
                float wave = Mathf.Sin(phase) * .55f + Mathf.Sin(phase * 2) * .08f + air;
                samples[i] = wave * (.55f + .12f * Mathf.Sin(6.283185f * 2 * t)) * .38f;
            }

            var clip = AudioClip.Create("Continuous inflation", hz, 1, hz, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public float SizeAudioGain(float radius)
        {
            float size = Mathf.InverseLerp(tuning.minimumRadius, Mathf.Max(tuning.minimumRadius + 1, MaximumRadius), radius);
            return Mathf.Lerp(.22f, 1, Mathf.Pow(size, .8f));
        }

        private void PlaySound(AudioClip clip, float sizeGain = 1)
        {
            if (SoundEnabled && SoundVolume > 0)
            {
                audioSource.pitch = UnityEngine.Random.Range(.96f, 1.04f);
                audioSource.PlayOneShot(clip, SoundVolume * .42f * sizeGain);
            }
        }

        private static AudioClip MakeClip(string name, float duration, float f0, float f1, bool noise)
        {
            const int hz = 44100;
            var data = new float[(int)(hz * duration)];
            float phase = 0, filtered = 0;
            var random = new System.Random(75);
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / hz, u = t / duration;
                phase += 6.283185f * Mathf.Lerp(f0, f1, u) / hz;
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2 - 1, .3f);
                float envelope = Mathf.Min(t / .006f, 1) * Mathf.Min((duration - t) / .008f, 1) * Mathf.Exp(-u * 5);
                data[i] = (Mathf.Sin(phase) + (noise ? filtered * .7f : Mathf.Sin(phase * 2) * .07f)) * envelope * .35f;
            }

            var clip = AudioClip.Create(name, data.Length, 1, hz, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
