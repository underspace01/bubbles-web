using Bubbles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyRevision
{
    public static string Run()
    {
        var tuning = AssetDatabase.LoadAssetAtPath<BubbleTuning>("Assets/Bubbles/Generated/PlayTuning.asset");
        tuning.refillMaximumCount = 3;
        tuning.tapSquash = .24f;
        tuning.tapDecay = 1.8f;
        tuning.tapFrequency = 18;
        EditorUtility.SetDirty(tuning);
        var background = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bubbles/Generated/Quiet background.mat");
        background.SetColor("_Top", Palette.GardenSky);
        background.SetColor("_Bottom", Palette.GardenHaze);
        background.SetColor("_Meadow", Palette.Meadow);
        background.SetColor("_Foliage", Palette.Foliage);
        background.SetColor("_Sunlight", Palette.Sunlight);
        EditorUtility.SetDirty(background);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Bubbles/Generated/ParentFont.asset");
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters("設定", out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(font);
        var ui = UnityEngine.Object.FindAnyObjectByType<ParentSettings>();
        if (ui)
        {
            ui.gameObject.name = "Settings";
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(missing))
        {
            throw new System.Exception("Missing settings glyphs: " + missing);
        }

        return "Random initial 1–3, stronger tap response, neutral settings and garden background saved";
    }
}
