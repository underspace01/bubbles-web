using UnityEditor;
using UnityEngine;
using Bubbles;

public static class ConfigureMembrane
{
    public static string Run()
    {
        var t = AssetDatabase.LoadAssetAtPath<BubbleTuning>("Assets/Bubbles/Generated/PlayTuning.asset");
        t.windRadiusScreenFraction = .25f;
        EditorUtility.SetDirty(t);
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bubbles/Generated/Soap film.mat");
        m.SetFloat("_PatternStrength", .65f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return "Quarter-short-side wind radius and flowing interference saved";
    }
}
