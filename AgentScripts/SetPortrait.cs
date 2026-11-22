using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SetPortrait
{
    public static string Run()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(kind, "FixedResolution"), 390, 844, "Bubble portrait" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
        view.Repaint();
        Camera.main.ResetAspect();
        return "Game view set to 390 × 844";
    }
}
