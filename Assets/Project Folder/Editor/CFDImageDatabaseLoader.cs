using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CFDImageDatabaseLoader
{
    private const string ResourcesPath = "CFD_Images";
    private const string DatabasePath = "Assets/Project Folder/CFDImageDatabase.asset";

    [MenuItem("Tools/VR-Law/Load CFD Images")]
    public static void LoadCFDImages()
    {
        Object[] loaded = Resources.LoadAll(ResourcesPath, typeof(Sprite));
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogWarning($"[CFD Loader] No Sprites found at Resources/{ResourcesPath}. Ensure assets are in Assets/Resources/CFD_Images and imported as Sprite (Texture Type: Sprite).");
            return;
        }

        var entries = new List<CFDImageEntry>();
        foreach (Object asset in loaded)
        {
            if (!(asset is Sprite sprite))
                continue;

            string name = sprite.name;
            if (!TryParseCFDName(name, out Gender gender, out Race race, out int attractiveness, out string expression))
            {
                Debug.LogWarning($"[CFD Loader] Skipped invalid name: {name}");
                continue;
            }

            entries.Add(new CFDImageEntry(name, gender, race, attractiveness, expression, sprite));
        }

        CFDImageDatabase database = GetOrCreateDatabase();
        database.SetEntries(entries);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CFD Loader] Loaded {entries.Count} images into {database.name}. Re-open the asset in the Inspector to review.");
    }

    private static bool TryParseCFDName(string name, out Gender gender, out Race race, out int attractiveness, out string expression)
    {
        gender = Gender.Male;
        race = Race.White;
        attractiveness = 0;
        expression = "";

        string[] parts = name.Split('-');
        if (parts.Length < 5)
            return false;

        string rg = parts[1].ToUpperInvariant();
        if (rg.Length != 2)
            return false;

        char r = rg[0], g = rg[1];
        race = (r == 'B') ? Race.Black : Race.White;
        gender = (g == 'F') ? Gender.Female : Gender.Male;

        if (!int.TryParse(parts[3], out attractiveness))
            return false;

        expression = parts[4];
        return true;
    }

    private static CFDImageDatabase GetOrCreateDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<CFDImageDatabase>(DatabasePath);
        if (database != null)
            return database;

        database = ScriptableObject.CreateInstance<CFDImageDatabase>();
        AssetDatabase.CreateAsset(database, DatabasePath);
        return database;
    }
}
