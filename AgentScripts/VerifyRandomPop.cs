using System;
using System.Linq;
using Bubbles;
using UnityEngine;

public static class VerifyRandomPop
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int checks = 0;
        void Check(bool ok, string message)
        {
            if (!ok)
            {
                throw new Exception(message);
            }

            checks++;
        }

        var saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState(205);
        var counts = new System.Collections.Generic.HashSet<int>();
        try
        {
            for (int i = 0; i < 120; i++)
            {
                var b = w.Spawn(w.PlaySize * .5f, 30);
                int target = b.popAfterTaps;
                counts.Add(target);
                Check(target >= 1 && target <= 6, "Random count in 1–6");
                for (int tap = 1; tap < target; tap++)
                {
                    w.Tap(b);
                    Check(b.active && b.popAfterTaps == target, "Count stable and no premature pop");
                }

                w.Tap(b);
                Check(!b.active && b.hits == target, "Pops at assigned count");
            }

            Check(counts.Count == 6, "All six counts occur");
            w.settingsUI.Open();
            var root = w.settingsUI.transform;
            Check(root.Find("Settings modal/Settings card/Viewport/Content/Tap choices") == null, "Tap setting removed");
            Check(!root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(x => x.text.Contains("タップ回数")), "No count label");
            var card = root.Find("Settings modal/Settings card").GetComponent<UnityEngine.UI.Image>();
            var view = root.Find("Settings modal/Settings card/Viewport");
            Check(card.color.a == 1 && view.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic, "Opaque settings card");
            Check(root.Find("Safe area/Settings hold").GetComponent<UnityEngine.UI.Image>().color.a == 1, "Opaque settings button");
            w.settingsUI.Close();
            return $"{checks} checks passed; all counts 1–6 observed";
        }
        finally
        {
            UnityEngine.Random.state = saved;
            w.settingsUI.Close();
            w.ResetBubbles();
        }
    }
}
