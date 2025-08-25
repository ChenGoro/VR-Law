#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PiperManifestGenerator
{
    // Files we never want in a runtime manifest
    private static readonly string[] ExcludedFileNames = {
        ".manifest.txt", ".DS_Store", "Thumbs.db"
    };

    private static bool IsExcluded(string pathOrName)
    {
        string name = Path.GetFileName(pathOrName);
        if (ExcludedFileNames.Contains(name)) return true;

        // Exclude Unity editor artifacts
        if (name.EndsWith(".meta")) return true;

        // Exclude temporary files
        if (name.EndsWith("~")) return true;

        return false;
    }

    private static string[] ListFilesTop(string dir)
        => Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    .Where(p => !IsExcluded(p))
                    .Select(Path.GetFileName)
                    .OrderBy(s => s, System.StringComparer.Ordinal)
                    .ToArray();

    private static string[] ListFilesRecursiveRelative(string root)
        => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .Where(p => !IsExcluded(p))
                    .Select(p => p.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                    .Select(p => p.Replace("\\", "/")) // jar: URLs want forward slashes
                    .OrderBy(s => s, System.StringComparer.Ordinal)
                    .ToArray();

    [MenuItem("Piper/Write StreamingAssets Manifests")]
    public static void WriteAll()
    {
        string sa = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(sa)) { Debug.LogWarning("No StreamingAssets folder yet."); return; }

        string piperRoot = Path.Combine(sa, "piper");
        string voicesRoot = Path.Combine(piperRoot, "voices");
        string espeakRoot = Path.Combine(piperRoot, "espeak-ng-data");

        // 1) Voices: one manifest per voice dir (top-level files only)
        if (Directory.Exists(voicesRoot))
        {
            foreach (var voiceDir in Directory.GetDirectories(voicesRoot))
            {
                var files = ListFilesTop(voiceDir);
                File.WriteAllText(Path.Combine(voiceDir, ".manifest.txt"),
                    string.Join("\n", files)); // use LF newlines for consistency
            }
        }

        // 2) espeak-ng-data: single recursive manifest relative to espeak root
        if (Directory.Exists(espeakRoot))
        {
            var files = ListFilesRecursiveRelative(espeakRoot);
            File.WriteAllText(Path.Combine(espeakRoot, ".manifest.txt"),
                string.Join("\n", files));
        }

        AssetDatabase.Refresh();
        Debug.Log("Piper manifests written.");
    }
}
#endif
