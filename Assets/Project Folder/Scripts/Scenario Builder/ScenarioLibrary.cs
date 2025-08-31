using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;


public class ScenarioLibrary : MonoBehaviour
{
    /// <summary>
    /// scenario templates library. in charge of loading the templates from the csv file and outting them in ScenarioTemplate objects.
    /// </summary>
    public List<ScenarioTemplate> Templates { get; private set; }

    public string csvFilePath = "scenarios"; // without .csv, in Resources

    public void Init()
    {
        Templates = LoadTemplatesFromCSV(csvFilePath);
    }

    public ScenarioTemplate GetRandomTemplate()
    {
        if (Templates.Count == 0) return null;
        return Templates[UnityEngine.Random.Range(0, Templates.Count)];
    }

    private List<ScenarioTemplate> LoadTemplatesFromCSV(string resourcePath)
    {
        List<ScenarioTemplate> templates = new List<ScenarioTemplate>();

        TextAsset csvData = Resources.Load<TextAsset>(resourcePath);
        if (csvData == null)
        {
            Debug.LogError($"CSV file not found at Resources/{resourcePath}");
            return templates;
        }

#if !UNITY_EDITOR
        SaveCsvCopyToPersistentData(csvData, resourcePath); // Save a copy for Analytics, only in builds
#endif

        string[] lines = csvData.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = SplitCSVLine(lines[i]);

            if (fields.Length < 5)
            {
                Debug.LogWarning($"Skipping malformed line {i + 1}: {lines[i]}");
                continue;
            }

            try
            {
                string desc = fields[0];

                if (!Enum.TryParse(fields[1], out ScenarioType type))
                    throw new System.Exception($"Invalid ScenarioType value: '{fields[1]}'");

                if (!Enum.TryParse(fields[2], out CrimeType crime))
                    throw new System.Exception($"Invalid CrimeType value: '{fields[2]}'");

                string prosecutor = fields[3];
                string attorney = fields[4];

                if (!float.TryParse(fields[5], out float income))
                    throw new System.Exception($"Invalid AnnualIncome value: '{fields[5]}'");

                templates.Add(new ScenarioTemplate(desc, type, crime, prosecutor, attorney, income, i));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing line {i + 1}: {ex.Message}");
            }

        }
        Debug.Log($"[ScenarioLibrary] Loaded {templates.Count} scenario templates from CSV.");
        return templates;
    }

    private void SaveCsvCopyToPersistentData(TextAsset csvData, string resourcePath)
    {
        try
        {
            // Use the last segment of the Resources path as the "original name"
            // e.g., "configs/scenarios" -> "scenarios"
            string originalName = Path.GetFileName(resourcePath);

            // Unique run id from your TXRDataManager
            string uid = TXRDataManager.UniqueParticipantId;

            string fileName = $"{uid}_ScenariosCSV_{originalName}.csv";
            string outPath = Path.Combine(Application.persistentDataPath, fileName);

            // Write exact bytes to preserve encoding exactly as in the asset
            File.WriteAllBytes(outPath, csvData.bytes);

            Debug.Log($"[ScenarioLibrary] Saved CSV copy to: {outPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScenarioLibrary] Failed to save CSV copy: {ex.Message}");
        }
    }


    // Handles commas inside quotes
    private string[] SplitCSVLine(string line)
    {
        var matches = Regex.Matches(line, @"(?:^|,)(?:""(?<val>(?:[^""]|"""")*)""|(?<val>[^"",]*))");
        return matches.Cast<Match>()
                      .Select(m => m.Groups["val"].Value.Replace("\"\"", "\"")) // unescape double quotes
                      .ToArray();
    }
}
