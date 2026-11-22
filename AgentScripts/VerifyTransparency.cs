using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyTransparency
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int limit = w.BubbleLimit, checks = 0;
        void Check(bool ok, string text)
        {
            if (!ok)
            {
                throw new Exception(text);
            }

            checks++;
        }

        try
        {
            w.ResetBubbles();
            Check(w.filmMaterial.GetFloat("_IndexOfRefraction") > 1.3f && !w.filmMaterial.HasProperty("_Opacity"), "Fresnel membrane uses refractive index without fixed opacity");
            Check(w.filmMaterial.renderQueue >= 3000, "Transparent render queue");
            var p = w.PlaySize * .5f;
            var oldest = w.Spawn(p, 60);
            var middle = w.Spawn(p, 55);
            var newest = w.Spawn(p, 50);
            Check(oldest.renderer.sortingOrder < middle.renderer.sortingOrder && middle.renderer.sortingOrder < newest.renderer.sortingOrder, "Old to new render order");
            Check(oldest.depth > middle.depth && middle.depth > newest.depth, "Old to new physical depth");
            Check(w.Hit(p) == newest, "Frontmost new bubble receives tap");
            middle.popAfterTaps = 1;
            w.Tap(middle);
            var reused = w.Spawn(p, 55);
            Check(ReferenceEquals(middle, reused) && reused.birthOrder > newest.birthOrder, "Pooled hole gets new birth sequence");
            Check(reused.renderer.sortingOrder > newest.renderer.sortingOrder && w.Hit(p) == reused, "Reused slot renders and hits in front");
            w.SetBubbleLimit(100, false);
            w.ResetBubbles();
            for (int i = 0; i < 105; i++)
            {
                w.Spawn(p, 25);
            }

            var active = w.Pool.Where(x => x.active).OrderBy(x => x.birthOrder).ToArray();
            Check(active.Length == 100, "Hundred-bubble stack");
            for (int i = 1; i < active.Length; i++)
            {
                Check(active[i].renderer.sortingOrder > active[i - 1].renderer.sortingOrder && active[i].depth < active[i - 1].depth, "Stack order after capacity replacement");
            }

            Check(w.Hit(p) == active.Last(), "Newest of hundred receives tap");
            return $"{checks} checks passed";
        }
        finally
        {
            w.SetBubbleLimit(limit, false);
            w.ResetBubbles();
        }
    }
}
