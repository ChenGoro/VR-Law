using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScenarioLibrary : MonoBehaviour
{
    /// <summary>Resources path to the scenarios root (e.g. Scenarios-Feb26). Must contain ScenarioList.txt and one subfolder per scenario.</summary>
    public string scenariosRoot = "Scenarios-Feb26";

    public List<ScenarioTemplate> Templates { get; private set; }

    public void Init()
    {
        Templates = LoadTemplatesFromFolders(scenariosRoot);
    }

    public ScenarioTemplate GetRandomTemplate()
    {
        if (Templates == null || Templates.Count == 0)
            throw new InvalidOperationException("[ScenarioLibrary] No templates loaded. Cannot GetRandomTemplate(). Check that scenario folders and ScenarioList.txt exist and load correctly.");
        return Templates[UnityEngine.Random.Range(0, Templates.Count)];
    }

    private List<ScenarioTemplate> LoadTemplatesFromFolders(string root)
    {
        string listPath = root + "/ScenarioList";
        TextAsset listAsset = Resources.Load<TextAsset>(listPath);
        if (listAsset == null)
            throw new InvalidOperationException($"[ScenarioLibrary] Scenario list not found at Resources/{listPath}. Add ScenarioList.txt that lists scenario folder names (one per line). See ScenarioFolderFormat.md.");

        string[] folderNames = listAsset.text
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        if (folderNames.Length == 0)
            throw new InvalidOperationException($"[ScenarioLibrary] ScenarioList at Resources/{listPath} is empty. Add at least one scenario folder name per line.");

        var templates = new List<ScenarioTemplate>();
        for (int i = 0; i < folderNames.Length; i++)
        {
            string folder = folderNames[i];
            string basePath = root + "/" + folder;
            try
            {
                ScenarioTemplate t = LoadOneScenario(basePath, folder, i + 1);
                templates.Add(t);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[ScenarioLibrary] Failed to load scenario folder '{folder}' at Resources/{basePath}: {ex.Message}", ex);
            }
        }

        Debug.Log($"[ScenarioLibrary] Loaded {templates.Count} scenario(s) from folders.");
        return templates;
    }

    private ScenarioTemplate LoadOneScenario(string basePath, string folderName, int scenarioIndex)
    {
        string desc = LoadText(basePath, "Description", folderName);
        string prosecutor = LoadText(basePath, "ProsecutorStatement", folderName);
        string attorney = LoadText(basePath, "AttorneyStatement", folderName);

        ScenarioType scenarioType = LoadEnum<ScenarioType>(basePath, "ScenarioType", folderName);
        CrimeType crimeType = LoadEnum<CrimeType>(basePath, "CrimeType", folderName);
        float annualIncome = LoadFloat(basePath, "DefendantAnnualIncome", folderName);

        AudioClip descClip = LoadAudio(basePath, "Description", folderName);
        AudioClip prosecutorClip = LoadAudio(basePath, "ProsecutorStatement", folderName);
        AudioClip attorneyClip = LoadAudio(basePath, "AttorneyStatement", folderName);

        string descJson = LoadWordTimesJson(basePath, "Description", folderName);
        string prosecutorJson = LoadWordTimesJson(basePath, "ProsecutorStatement", folderName);
        string attorneyJson = LoadWordTimesJson(basePath, "AttorneyStatement", folderName);

        var descBlock = new NarrativeBlock(desc, descClip, descJson);
        var prosecutorBlock = new NarrativeBlock(prosecutor, prosecutorClip, prosecutorJson);
        var attorneyBlock = new NarrativeBlock(attorney, attorneyClip, attorneyJson);

        return new ScenarioTemplate(
            description: desc,
            scenarioType: scenarioType,
            crimeType: crimeType,
            prosecutorStatement: prosecutor,
            attorneyStatement: attorney,
            descriptionBlock: descBlock,
            prosecutorBlock: prosecutorBlock,
            attorneyBlock: attorneyBlock,
            annualIncome: annualIncome,
            scnarioIndex: scenarioIndex
        );
    }

    private static string LoadText(string basePath, string name, string folderName)
    {
        TextAsset a = Resources.Load<TextAsset>(basePath + "/" + name);
        if (a == null)
            throw new InvalidOperationException($"Missing required file: {name}.txt in scenario folder '{folderName}'.");
        return a.text;
    }

    private static T LoadEnum<T>(string basePath, string name, string folderName) where T : struct
    {
        TextAsset a = Resources.Load<TextAsset>(basePath + "/" + name);
        if (a == null)
            throw new InvalidOperationException($"Missing required file: {name}.txt in scenario folder '{folderName}'.");
        string value = a.text.Trim();
        if (!Enum.TryParse<T>(value, true, out T result))
            throw new InvalidOperationException($"Invalid {name} value in scenario folder '{folderName}': '{value}'. Expected one of: {string.Join(", ", Enum.GetNames(typeof(T)))}.");
        return result;
    }

    private static float LoadFloat(string basePath, string name, string folderName)
    {
        TextAsset a = Resources.Load<TextAsset>(basePath + "/" + name);
        if (a == null)
            throw new InvalidOperationException($"Missing required file: {name}.txt in scenario folder '{folderName}'.");
        string value = a.text.Trim();
        if (!float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result))
            throw new InvalidOperationException($"Invalid {name} value in scenario folder '{folderName}': '{value}'. Expected a number.");
        return result;
    }

    /// <summary>Loads audio if present. Logs an error and returns null when missing (scenario still loads; playback will be text-only).</summary>
    private static AudioClip LoadAudio(string basePath, string name, string folderName)
    {
        AudioClip clip = Resources.Load<AudioClip>(basePath + "/" + name);
        if (clip == null)
            Debug.LogError($"[ScenarioLibrary] Missing audio file: {name}.(mp3|wav|ogg) in scenario folder '{folderName}' at Resources/{basePath}. Continuing without audio.");
        return clip;
    }

    private static string LoadWordTimesJson(string basePath, string name, string folderName)
    {
        string path = basePath + "/" + name + "_wordtimes";
        TextAsset a = Resources.Load<TextAsset>(path);
        if (a == null)
            throw new InvalidOperationException($"Missing required file: {name}_wordtimes.json in scenario folder '{folderName}'.");
        return a.text;
    }
}
