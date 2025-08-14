#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PiperManifestGenerator
{
    [MenuItem("Piper/Write StreamingAssets Manifests")]
    public static void WriteAll()
    {
        string sa = Path.Combine(Application.dataPath, "StreamingAssets");
        string piperRoot = Path.Combine(sa, "piper");
        string voicesRoot = Path.Combine(piperRoot, "voices");
        string espeakRoot = Path.Combine(piperRoot, "espeak-ng-data");

        if (!Directory.Exists(sa)) { Debug.LogWarning("No StreamingAssets folder yet."); return; }

        // Voices: write a manifest in each voice folder (relative paths)
        if (Directory.Exists(voicesRoot))
        {
            foreach (var voiceDir in Directory.GetDirectories(voicesRoot))
            {
                string[] files = Directory.GetFiles(voiceDir, "*", SearchOption.TopDirectoryOnly)
                                          .Select(p => Path.GetFileName(p)).ToArray();
                File.WriteAllLines(Path.Combine(voiceDir, ".manifest.txt"), files);
            }
        }

        // espeak-ng-data: single manifest listing all files recursively (relative to espeak root)
        if (Directory.Exists(espeakRoot))
        {
            var files = Directory.GetFiles(espeakRoot, "*", SearchOption.AllDirectories)
                                 .Select(p => p.Replace(espeakRoot + Path.DirectorySeparatorChar, "")
                                               .Replace("\\", "/"))
                                 .ToArray();
            File.WriteAllLines(Path.Combine(espeakRoot, ".manifest.txt"), files);
        }

        AssetDatabase.Refresh();
        Debug.Log("Piper manifests written.");
    }
}
#endif
