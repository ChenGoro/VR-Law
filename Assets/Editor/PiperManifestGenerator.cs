using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildPiperManifests
{
    [MenuItem("Tools/Piper/Regenerate Manifests")]
    public static void Regenerate()
    {
        string sa = Application.streamingAssetsPath.Replace("\\", "/");

        // voices
        string voicesRoot = Path.Combine(sa, "piper/voices").Replace("\\", "/");
        if (Directory.Exists(voicesRoot))
        {
            foreach (var voiceDir in Directory.GetDirectories(voicesRoot))
            {
                WriteManifestForDir(voiceDir);
            }
        }

        // espeak-ng-data
        string espeakRoot = Path.Combine(sa, "piper/espeak-ng-data").Replace("\\", "/");
        if (Directory.Exists(espeakRoot))
        {
            WriteManifestForDir(espeakRoot);
        }

        AssetDatabase.Refresh();
        Debug.Log("[Piper] Regenerated .manifest.txt files.");
    }

    private static void WriteManifestForDir(string rootDir)
    {
        var allFiles = Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories)
                                // ignore meta files
                                .Where(p => !p.EndsWith(".meta"))
                                // ignore the manifest itself (if re-running)
                                .Where(p => Path.GetFileName(p) != ".manifest.txt")
                                .Select(p => p.Replace("\\", "/"));

        string manifestPath = Path.Combine(rootDir, ".manifest.txt").Replace("\\", "/");
        string rootNorm = rootDir.Replace("\\", "/").TrimEnd('/');
        var relLines = allFiles.Select(p => p.Substring(rootNorm.Length + 1));
        File.WriteAllLines(manifestPath, relLines);
    }
}
