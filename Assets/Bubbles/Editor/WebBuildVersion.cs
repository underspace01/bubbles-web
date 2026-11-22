using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class WebBuildVersion : IPostprocessBuildWithReport
{
    public int callbackOrder => 2000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
        {
            return;
        }

        var root = report.summary.outputPath;
        var index = Path.Combine(root, "index.html");
        var assets = Directory.GetFiles(Path.Combine(root, "Build")).OrderBy(p => p).ToArray();
        using (var hash = SHA256.Create())
        {
            var hashes = new StringBuilder();
            foreach (var path in assets)
            {
                using (var stream = File.OpenRead(path))
                {
                    hashes.Append(Path.GetFileName(path)).Append(Convert.ToBase64String(hash.ComputeHash(stream)));
                }
            }

            var id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(hashes.ToString()))).Replace("-", "").ToLowerInvariant();
            var html = File.ReadAllText(index);
            foreach (var path in assets)
            {
                var url = "Build/" + Path.GetFileName(path);
                html = html.Replace("'" + url + "'", "'" + url + "?v=" + id + "'");
            }

            html = html.Replace("const canvas=", "window.bubbleBuildId='" + id + "';\nconst canvas=");
            File.WriteAllText(index, html);
            File.WriteAllText(Path.Combine(root, "build-info.json"), "{\"buildId\":\"" + id + "\"}");
        }
    }
}
