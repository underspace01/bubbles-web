using System;
using Bubbles;
using UnityEngine;

public static class VerifyWindRim
{
    public static string Run()
    {
        var report = WindStartProbe.Measure(UnityEngine.Object.FindAnyObjectByType<BubbleWorld>());
        if (!report.passed)
        {
            throw new Exception(report.error);
        }

        if (Mathf.Abs(report.transmissionScale - .86f) > .001f)
        {
            throw new Exception("Visibility adjustment missing");
        }

        return JsonUtility.ToJson(report);
    }
}
