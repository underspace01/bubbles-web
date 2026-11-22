using UnityEngine;

namespace Bubbles
{
    // 生成順の描画と揺れの見た目を更新する。
    public sealed partial class BubbleWorld
    {
        private void RefreshDepthOrder()
        {
            var ordered = pool.FindAll(b => b.active);
            ordered.Sort((a, b) => a.birthOrder.CompareTo(b.birthOrder));
            for (int i = 0; i < ordered.Count; i++)
            {
                var b = ordered[i];
                b.depth = Mathf.Lerp(.35f, -.35f, ordered.Count > 1 ? (float)i / (ordered.Count - 1) : 0);
                b.renderer.sortingOrder = i;
                ApplyTransform(b);
            }
        }

        private void ApplyTransform(Bubble b)
        {
            float age = SimulationTime - b.tapTime;
            float wob = Mathf.Cos(age * tuning.tapFrequency) * b.wobble * tuning.tapSquash + Mathf.Sin(SimulationTime * 1.3f + b.phase) * .016f;
            var jiggle = Vector2.zero;
            if (!b.drifting)
            {
                jiggle = new Vector2(Mathf.Sin(age * 14), Mathf.Sin(age * 11)) * (b.wobble * b.radius * .06f);
                if (b.returningSway)
                {
                    jiggle *= Mathf.SmoothStep(0, 1, (SimulationTime - b.freeSince) / 1.2f);
                }

                if (!b.growing)
                {
                    jiggle += new Vector2(Mathf.Sin(SimulationTime * .65f + b.phase), Mathf.Sin(SimulationTime * .53f + b.phase * 2)) * tuning.idleSwayPixels * Mathf.SmoothStep(0, 1, (SimulationTime - b.freeSince) / 1.2f);
                }
            }

            b.visualOffset = jiggle;
            b.transform.position = new Vector3((b.position.x + jiggle.x - PlaySize.x * .5f) / 100, (b.position.y + jiggle.y - PlaySize.y * .5f) / 100, b.depth);
            var rotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 12) * b.wobble * 7);
            var stretch = new Vector3(1 + wob, 1 - wob, 1);
            float shapeTime = SimulationTime, renderWobble = b.wobble;
            if (b.hasShapeSnapshot)
            {
                float blend = b.shapeFrozen ? 0 : Mathf.SmoothStep(0, 1, b.shapeBlend);
                stretch = Vector3.Lerp(b.frozenStretch, stretch, blend);
                rotation = Quaternion.Slerp(b.frozenRotation, rotation, blend);
                shapeTime = Mathf.Lerp(b.frozenShapeTime, shapeTime, blend);
                renderWobble = Mathf.Lerp(b.frozenWobble, renderWobble, blend);
            }

            b.transform.localRotation = rotation;
            b.transform.localScale = stretch * b.radius / 50;
            b.renderShapeTime = shapeTime;
            b.renderWobble = renderWobble;
            b.properties.SetFloat(ClockID, SimulationTime);
            b.properties.SetFloat(ShapeClockID, shapeTime);
            b.properties.SetFloat(WobbleID, renderWobble);
            b.renderer.SetPropertyBlock(b.properties);
        }
    }
}
