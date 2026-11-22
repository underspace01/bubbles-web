using UnityEditor;
using UnityEngine;

public static class ConfigureTransparency
{
    public static string Run()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bubbles/Generated/Soap film.mat");
        var saved = new SerializedObject(m);
        var floats = saved.FindProperty("m_SavedProperties.m_Floats");
        for (int i = floats.arraySize - 1; i >= 0; i--)
        {
            if (floats.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue == "_Opacity")
            {
                floats.DeleteArrayElementAtIndex(i);
            }
        }

        saved.ApplyModifiedProperties();
        m.SetFloat("_TransmissionScale", .86f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return "Fresnel transmission scaled to 86% for readability";
    }
}
