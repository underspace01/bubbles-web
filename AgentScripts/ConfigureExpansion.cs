using Bubbles;
using UnityEditor;
using UnityEngine;

public static class ConfigureExpansion
{
    public static string Run()
    {
        var tuning = AssetDatabase.LoadAssetAtPath<BubbleTuning>("Assets/Bubbles/Generated/PlayTuning.asset");
        tuning.maximumDiameterWidthFraction = 2f / 3;
        tuning.idleSwayPixels = 10;
        EditorUtility.SetDirty(tuning);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bubbles/Generated/Film droplets.mat");
        var shader = Shader.Find("Bubbles/Water Droplet");
        if (!shader)
        {
            throw new System.Exception("Water droplet shader not imported");
        }

        material.shader = shader;
        material.SetColor("_Edge", Palette.Horizon);
        material.SetColor("_Shine", Palette.Window);
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return "2/3 screen-width diameter, 10px sway and water-droplet material saved";
    }
}
