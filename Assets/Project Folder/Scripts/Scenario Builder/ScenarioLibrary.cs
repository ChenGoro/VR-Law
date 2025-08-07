using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System;

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

                templates.Add(new ScenarioTemplate(desc, type, crime, prosecutor, attorney));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing line {i + 1}: {ex.Message}");
            }

        }

        return templates;
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
