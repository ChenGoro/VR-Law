using System.Collections.Generic;
using UnityEngine;

public class ScenarioLibrary : MonoBehaviour
{
    public List<ScenarioTemplate> Templates { get; private set; }

    public string csvFilePath = "scenarios"; // without .csv, in Resources

    private void Awake()
    {
        Templates = LoadTemplatesFromCSV(csvFilePath);
    }

    public ScenarioTemplate GetRandomTemplate()
    {
        if (Templates.Count == 0) return null;
        return Templates[Random.Range(0, Templates.Count)];
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
                ScenarioType type = (ScenarioType)System.Enum.Parse(typeof(ScenarioType), fields[1]);
                CrimeType crime = (CrimeType)System.Enum.Parse(typeof(CrimeType), fields[2]);
                string prosecutor = fields[3];
                string attorney = fields[4];

                templates.Add(new ScenarioTemplate(desc, type, crime, prosecutor, attorney));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error parsing line {i + 1}: {ex.Message}");
            }
        }

        return templates;
    }

    // Handles commas inside quotes
    private string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '\"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current); // Add last one
        return result.ToArray();
    }
}
