using System;
using System.IO;
using Bubbles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

public static class BubbleProject
{
    private const string Root = "Assets/Bubbles/Generated";
    [MenuItem("Bubbles/Create or rebuild play scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying)
        {
            throw new Exception("Stop Play mode before creating scene.");
        }

        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.vSyncCount = 0;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (!urp)
        {
            throw new Exception("URP template required.");
        }

        urp.supportsHDR = true;
        urp.msaaSampleCount = 4;
        urp.renderScale = 1;
        urp.supportsCameraOpaqueTexture = false;
        urp.supportsCameraDepthTexture = false;
        EditorUtility.SetDirty(urp);
        var camGo = new GameObject("Bubble camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        camGo.tag = "MainCamera";
        var camera = camGo.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.22f;
        camera.transform.position = new Vector3(0, 0, -12);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Palette.Deep;
        camera.allowHDR = true;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 40;
        var cameraData = camGo.GetComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask = 1;
        cameraData.antialiasing = AntialiasingMode.None;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Palette.Sky;
        RenderSettings.ambientEquatorColor = Palette.Horizon;
        RenderSettings.ambientGroundColor = Palette.Deep;
        var lightGo = new GameObject("Soft window key", typeof(Light));
        var key = lightGo.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = Palette.Window;
        key.intensity = 1.1f;
        key.shadows = LightShadows.None;
        lightGo.transform.rotation = Quaternion.Euler(40, -30, 0);
        var volumeGo = new GameObject("Gentle lens", typeof(Volume));
        var volume = volumeGo.GetComponent<Volume>();
        volume.isGlobal = true;
        var profile = GetOrCreate<VolumeProfile>(Root + "/GentleLens.asset");
        profile.components.Clear();
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.value = TonemappingMode.Neutral;
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = .15f;
        bloom.threshold.value = 1.1f;
        bloom.scatter.value = .6f;
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = .1f;
        vignette.smoothness.value = .5f;
        vignette.color.value = Palette.Deep;
        var grade = profile.Add<ColorAdjustments>(true);
        grade.saturation.value = 3;
        grade.contrast.value = 2;
        foreach (var component in profile.components)
        {
            if (!AssetDatabase.Contains(component))
            {
                AssetDatabase.AddObjectToAsset(component, profile);
            }
        }

        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        var film = Material("Soap film", "Bubbles/Soap Film");
        film.SetFloat("_IndexOfRefraction", 1.333f);
        film.SetFloat("_RefractionStrength", .85f);
        film.SetFloat("_Dispersion", .012f);
        EditorUtility.SetDirty(film);
        var background = Material("Quiet background", "Bubbles/Quiet Background");
        EditorUtility.SetDirty(background);
        var particle = Material("Film droplets", "Bubbles/Water Droplet");
        particle.shader = Shader.Find("Bubbles/Water Droplet");
        particle.SetColor("_Edge", Palette.Horizon);
        particle.SetColor("_Shine", Palette.Window);
        EditorUtility.SetDirty(particle);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/SoapSphere.asset");
        if (!mesh)
        {
            mesh = Sphere();
            AssetDatabase.CreateAsset(mesh, Root + "/SoapSphere.asset");
        }

        var tuning = GetOrCreate<BubbleTuning>(Root + "/PlayTuning.asset");
        var uiGo = new GameObject("Settings", typeof(RectTransform), typeof(ParentSettings));
        var ui = uiGo.GetComponent<ParentSettings>();
        ui.font = CreateFont();
        var game = new GameObject("Bubble Play", typeof(BubbleWorld));
        var world = game.GetComponent<BubbleWorld>();
        world.tuning = tuning;
        world.filmMaterial = film;
        world.backgroundMaterial = background;
        world.dropletMaterial = particle;
        world.sphereMesh = mesh;
        world.playCamera = camera;
        world.settingsUI = ui;
        world.environments = new[]
        {
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bubbles/Environments/rooitou_park_2k.hdr"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bubbles/Environments/kiara_1_dawn_2k.hdr"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bubbles/Environments/venice_sunset_2k.hdr")
        };
        world.SetBackground(0, false);
        PlayerSettings.companyName = "Bubble Play";
        PlayerSettings.productName = "シャボン玉あそび";
        PlayerSettings.defaultScreenWidth = 390;
        PlayerSettings.defaultScreenHeight = 844;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.WebGL.template = "PROJECT:Bubbles";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.WebGL.maximumMemorySize = 512;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BubblePlay.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/BubblePlay.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Bubbles] Scene created. Thin-film material, 3D mesh, local settings and pooled feedback ready.");
    }

    private static T GetOrCreate<T>(string path)
        where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!a)
        {
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
        }

        return a;
    }

    private static Material Material(string name, string shaderName)
    {
        string path = Root + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(shaderName);
        if (!shader)
        {
            throw new Exception("Missing shader: " + shaderName);
        }

        if (!m)
        {
            m = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(m, path);
        }

        return m;
    }

    private static TMP_FontAsset CreateFont()
    {
        string path = Root + "/ParentFont.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset)
        {
            return asset;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Bubbles/Fonts/NotoSansJP.ttf");
        if (!font)
        {
            throw new Exception("Bundled Noto Sans JP font not imported.");
        }

        asset = TMP_FontAsset.CreateFontAsset(font, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        asset.name = "Parent Japanese";
        const string chars = "BUBLE PAY設定背景公園夜明けの山夕暮れ水辺。、音を鳴らすオンフ量割れるまでタップ回数空いてところ長押し玉が育ちますなぞ風揺シャボンリセットあそびに戻0123456789%　";
        asset.TryAddCharacters(chars, out string missing);
        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("Font missing: " + missing);
        }

        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        asset.isMultiAtlasTexturesEnabled = false;
        AssetDatabase.CreateAsset(asset, path);
        foreach (var texture in asset.atlasTextures)
        {
            texture.name = "Japanese SDF atlas";
            AssetDatabase.AddObjectToAsset(texture, asset);
        }

        asset.material.name = "Japanese SDF";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static Mesh Sphere()
    {
        const int slices = 96, rings = 64;
        var vertices = new Vector3[(slices + 1) * (rings + 1)];
        var normals = new Vector3[vertices.Length];
        var triangles = new int[slices * rings * 6];
        int n = 0;
        for (int y = 0; y <= rings; y++)
        {
            for (int x = 0; x <= slices; x++)
            {
                float phi = y * Mathf.PI / rings, theta = x * 2 * Mathf.PI / slices;
                var normal = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                int i = y * (slices + 1) + x;
                vertices[i] = normal * .5f;
                normals[i] = normal;
            }
        }

        for (int y = 0; y < rings; y++)
        {
            for (int x = 0; x < slices; x++)
            {
                int a = y * (slices + 1) + x, b = a + slices + 1;
                triangles[n++] = a;
                triangles[n++] = a + 1;
                triangles[n++] = b;
                triangles[n++] = a + 1;
                triangles[n++] = b + 1;
                triangles[n++] = b;
            }
        }

        var mesh = new Mesh
        {
            name = "Smooth soap sphere",
            vertices = vertices,
            normals = normals,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    [MenuItem("Bubbles/Build Web")]
    public static void BuildWeb()
    {
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Build/Web");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/BubblePlay.unity" }, locationPathName = "Build/Web", target = BuildTarget.WebGL, options = BuildOptions.None });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception("Web build failed: " + report.summary.result);
        }

        Debug.Log($"[Bubbles] Web build succeeded: {report.summary.totalSize} bytes, {report.summary.totalTime}.");
    }
}
