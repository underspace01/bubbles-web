using System;
using System.Linq;
using System.Threading.Tasks;
using Bubbles;
using UnityEngine;

public static class VerifyIndoor
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        int checks = 0;
        void Check(bool ok, string text)
        {
            if (!ok)
            {
                throw new Exception(text);
            }

            checks++;
        }

        int original = w.BackgroundIndex;
        try
        {
            Check(Mathf.Approximately(w.tuning.settingsHoldSeconds, .75f), "Half-length settings hold");
            w.settingsUI.Close();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var start = typeof(ParentSettings).GetField("heldSince", flags);
            var update = typeof(ParentSettings).GetMethod("Update", flags);
            w.settingsUI.StartHold();
            start.SetValue(w.settingsUI, Time.unscaledTime - .5f);
            update.Invoke(w.settingsUI, null);
            Check(!w.settingsUI.IsOpen, "Does not open early");
            start.SetValue(w.settingsUI, Time.unscaledTime - .8f);
            update.Invoke(w.settingsUI, null);
            Check(w.settingsUI.IsOpen, "Opens after 0.75 seconds");
            w.settingsUI.Close();
            w.settingsUI.StartHold();
            w.settingsUI.CancelHold();
            update.Invoke(w.settingsUI, null);
            Check(!w.settingsUI.IsOpen, "Short press cancels");
            w.SetBackground(2, false);
            Check(w.environmentNames[2] == "室内" && w.environments[2].name == "lebombo_2k", "Indoor replaces sunset in same slot");
            Check(w.environments[2].format == TextureFormat.RGBAHalf, "Indoor remains HDR");
            Check(w.filmMaterial.GetTexture("_HDRI") == w.environments[2] && w.backgroundMaterial.GetTexture("_HDRI") == w.environments[2], "Indoor shared by reflection and background");
            Check(w.settingsUI.font.HasCharacters("室内"), "Indoor label glyphs");
            Check(!w.settingsUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(x => x.text.Contains("夕暮れ")), "No old sunset label");
            return $"{checks} checks passed";
        }
        finally
        {
            w.settingsUI.Close();
            w.SetBackground(original, false);
            w.ResetBubbles();
        }
    }
}
