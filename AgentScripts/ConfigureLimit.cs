using System;
using Bubbles;
using TMPro;
using UnityEditor;

public static class ConfigureLimit
{
    public static string Run()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        w.tuning.maximumCount = 100;
        EditorUtility.SetDirty(w.tuning);
        var font = w.settingsUI.font;
        var text = "シャボン玉の上限個空いているところをタップで玉が出ます。長押しで育て、なぞると風が起きます。";
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters(text, out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(font);
        if (!font.HasCharacters(text))
        {
            throw new Exception(missing);
        }

        AssetDatabase.SaveAssets();
        return "100 pooled bubbles and count-setting glyphs saved";
    }
}
