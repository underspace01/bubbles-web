using UnityEngine;

namespace Bubbles
{
    // 風・成長・補充と画面内の移動を計算する。
    public sealed partial class BubbleWorld
    {
        public static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            var d = b - a;
            float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, d) / d.sqrMagnitude) : 0;
            return Vector2.Distance(point, a + d * t);
        }

        public float InertiaFactor(float radius)
        {
            float size = Mathf.InverseLerp(tuning.minimumRadius, MaximumRadius, radius);
            return 1 + 2 * size * size;
        }

        public float WindInfluenceRadius => Mathf.Max(1, Mathf.Min(PlaySize.x, PlaySize.y) * tuning.windRadiusScreenFraction);

        public void ApplyWind(Vector2 a, Vector2 z, float dt)
        {
            var d = z - a;
            if (Paused || dt <= 0 || d.sqrMagnitude < .0001f)
            {
                return;
            }

            var airVelocity = d / Mathf.Max(dt, .0001f) * tuning.windGain;
            foreach (var b in pool)
            {
                if (b.active && !b.growing)
                {
                    float influence = Mathf.Max(0, 1 - SegmentDistance(b.position + b.visualOffset, a, z) / WindInfluenceRadius);
                    if (influence <= 0)
                    {
                        continue;
                    }

                    // 表示中のずれを基準位置へ移し、揺れを停止しても
                    // 風の開始時に中心位置が飛ばないようにする。
                    if (!b.drifting)
                    {
                        b.position += b.visualOffset;
                        b.visualOffset = Vector2.zero;
                        b.drifting = true;
                    }

                    if (!b.shapeFrozen)
                    {
                        b.frozenStretch = b.transform.localScale / Mathf.Max(b.radius / 50, .001f);
                        b.frozenRotation = b.transform.localRotation;
                        b.frozenShapeTime = b.renderShapeTime;
                        b.frozenWobble = b.renderWobble;
                        b.shapeFrozen = b.hasShapeSnapshot = true;
                        b.shapeBlend = 0;
                    }

                    b.windVelocity += airVelocity * influence;
                    b.hasWind = true;
                }
            }
        }

        public void Tick(float dt, float now)
        {
            if (Paused)
            {
                return;
            }

            SimulationTime += dt;
            UpdateGrowth(dt, now);
            UpdateBubbles(dt);
            UpdateRefill(dt);
            SyncGrowthAudio();
        }

        private void UpdateGrowth(float dt, float now)
        {
            foreach (var p in gestures.Values)
            {
                if (p.mode == GestureMode.Pending && p.target == null && now - p.started >= tuning.holdSeconds)
                {
                    p.created = Spawn(p.start, tuning.minimumRadius, true);
                    p.mode = GestureMode.Growing;
                }

                if (p.created != null)
                {
                    p.created.growthElapsed += dt;
                    float duration = Mathf.Max(.1f, (MaximumRadius - tuning.minimumRadius) / tuning.growthPerSecond);
                    float t = Mathf.Clamp01(p.created.growthElapsed / duration);
                    p.created.radius = Mathf.Lerp(tuning.minimumRadius, MaximumRadius, 1 - Mathf.Pow(1 - t, 3));
                }
            }
        }

        private void UpdateBubbles(float dt)
        {
            foreach (var b in pool)
            {
                if (b.active)
                {
                    b.wobble *= Mathf.Exp(-dt * tuning.tapDecay);
                    if (!b.growing)
                    {
                        if (!b.hasWind)
                        {
                            b.shapeFrozen = false;
                        }

                        if (!b.shapeFrozen && b.hasShapeSnapshot)
                        {
                            b.shapeBlend = Mathf.Min(1, b.shapeBlend + dt / .25f);
                        }

                        var target = b.hasWind ? b.windVelocity : Vector2.zero;
                        float rate = Mathf.Max(0, b.hasWind ? tuning.windResponse : tuning.drag) / InertiaFactor(b.radius);
                        // 相対的な風速に指数応答する。風または静止空気による減衰を
                        // 一度だけ積分してから、共通の速度上限を適用する。
                        b.velocity = Vector2.Lerp(b.velocity, target, 1 - Mathf.Exp(-rate * dt));
                        b.velocity = Vector2.ClampMagnitude(b.velocity, tuning.maximumSpeed);
                        if (b.velocity.sqrMagnitude < .04f)
                        {
                            b.velocity = Vector2.zero;
                        }

                        if (b.drifting && !b.hasWind && b.velocity == Vector2.zero)
                        {
                            b.drifting = false;
                            b.returningSway = true;
                            b.freeSince = SimulationTime;
                        }

                        b.position += b.velocity * dt;
                        KeepInside(b);
                    }

                    b.hasWind = false;
                    b.windVelocity = Vector2.zero;
                    if (b.growing)
                    {
                        KeepInside(b);
                    }

                    ApplyTransform(b);
                }
            }
        }

        private void UpdateRefill(float dt)
        {
            autoTime += dt;
            if (autoTime >= tuning.autoSpawnSeconds)
            {
                autoTime = 0;
                if (ActiveCount < RefillTargetCount)
                {
                    Spawn(new Vector2(UnityEngine.Random.Range(30, PlaySize.x - 30), 35), UnityEngine.Random.Range(MaximumRadius / 3, MaximumRadius));
                }
            }
        }

        private void KeepInside(Bubble b)
        {
            float m = b.radius * (1 + tuning.tapSquash) + tuning.idleSwayPixels + 3;
            if (b.position.x < m)
            {
                b.position.x = m;
                b.velocity.x = Mathf.Abs(b.velocity.x) * tuning.edgeRestitution;
            }

            if (b.position.x > PlaySize.x - m)
            {
                b.position.x = PlaySize.x - m;
                b.velocity.x = -Mathf.Abs(b.velocity.x) * tuning.edgeRestitution;
            }

            if (b.position.y < m)
            {
                b.position.y = m;
                b.velocity.y = Mathf.Abs(b.velocity.y) * tuning.edgeRestitution;
            }

            if (b.position.y > PlaySize.y - m)
            {
                b.position.y = PlaySize.y - m;
                b.velocity.y = -Mathf.Abs(b.velocity.y) * tuning.edgeRestitution;
            }
        }
    }
}
