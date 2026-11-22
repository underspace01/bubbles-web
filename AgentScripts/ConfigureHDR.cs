using System;
using Bubbles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureHDR
{
    public static string Run()
    {
        string root = "Assets/Bubbles/Environments/";
        string[] files =
        {
            "rooitou_park_2k.hdr",
            "kiara_1_dawn_2k.hdr",
            "lebombo_2k.hdr",
            "shanghai_bund_2k.hdr"
        };
        var textures = new Texture2D[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string path = root + files[i];
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            var options = importer.GetDefaultPlatformTextureSettings();
            options.format = TextureImporterFormat.RGBAHalf;
            options.textureCompression = TextureImporterCompression.Uncompressed;
            options.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(options);
            foreach (var platform in new[]
            {
                "Standalone",
                "WebGL"
            }

            )
            {
                var explicitFormat = importer.GetPlatformTextureSettings(platform);
                explicitFormat.overridden = true;
                explicitFormat.format = TextureImporterFormat.RGBAHalf;
                explicitFormat.maxTextureSize = 2048;
                explicitFormat.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(explicitFormat);
            }

            importer.SaveAndReimport();
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        var world = UnityEngine.Object.FindAnyObjectByType<BubbleWorld>();
        world.environments = textures;
        world.environmentNames = new[]
        {
            "公園",
            "夜明けの山",
            "室内",
            "夜景"
        };
        world.SetBackground(0, false);
        EditorUtility.SetDirty(world);
        world.tuning.drag = 1.25f;
        world.tuning.settingsHoldSeconds = .75f;
        EditorUtility.SetDirty(world.tuning);
        world.filmMaterial.SetFloat("_IndexOfRefraction", 1.333f);
        world.filmMaterial.SetFloat("_RefractionStrength", .85f);
        world.filmMaterial.SetFloat("_Dispersion", .012f);
        world.filmMaterial.SetFloat("_Reflection", 1.1f);
        EditorUtility.SetDirty(world.filmMaterial);
        EditorUtility.SetDirty(world.backgroundMaterial);
        var font = world.settingsUI.font;
        string missing = string.Empty;
        if (!font.HasCharacters("背景公園夜明けの山室内景"))
        {
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.TryAddCharacters("背景公園夜明けの山室内景", out missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
        }

        if (!font.HasCharacters("背景公園夜明けの山室内景"))
        {
            throw new Exception("Missing glyphs: " + missing);
        }

        EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        EditorSceneManager.SaveScene(world.gameObject.scene);
        AssetDatabase.SaveAssets();
        return string.Join(", ", Array.ConvertAll(textures, t => $"{t.name}: {t.width}×{t.height} {t.format}"));
    }
}
