using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    public ScenarioLibrary ScenarioLibrary;
    public PhotoManager PhotoManager;

    [SerializeField] private List<string> maleNames;
    [SerializeField] private List<string> femaleNames;
    [SerializeField] private List<string> lastNames;

    private List<ScenarioData> scenarioDataList;
    private int currentScenarioIndex = 0;

    private void Start()
    {
        ScenarioLibrary.Init();
        PhotoManager.Init();
        BuildScenarioDataList();
    }

    private void BuildScenarioDataList()
    {
        scenarioDataList = new List<ScenarioData>();

        int totalScenarios = Mathf.Min(
            ScenarioLibrary.Templates.Count,
            PhotoManager.DefendantQueue.RemainingCount,
            PhotoManager.VictimQueue.RemainingCount
        );

        for (int i = 0; i < totalScenarios; i++)
        {
            ScenarioTemplate template = ScenarioLibrary.GetRandomTemplate();

            Photo defendant = PhotoManager.DefendantQueue.Next();
            Photo victim = PhotoManager.VictimQueue.Next();

            string defFirst = GetRandomName(defendant.Gender);
            string defLast = GetRandomLastName();
            string vicFirst = GetRandomName(victim.Gender);
            string vicLast = GetRandomLastName();

            int[] layoutOrder = GenerateRandomLayoutOrder();

            template.AddNamesToStatements(defFirst, defLast, vicFirst, vicLast);


            ScenarioData scenario = new ScenarioData(
                template,
                layoutOrder,
                defendant.Sprite,
                defFirst,
                defLast,
                victim.Sprite,
                vicFirst,
                vicLast
            );

            scenarioDataList.Add(scenario);
        }

        Debug.Log($"Initialized {scenarioDataList.Count} scenarios.");
    }



    public ScenarioData GetNextScenario()
    {
        if (currentScenarioIndex >= scenarioDataList.Count)
        {
            Debug.LogWarning("No more scenarios available.");
            return null;
        }

        return scenarioDataList[currentScenarioIndex++];
    }

    public bool HasNextScenario()
    {
        return currentScenarioIndex <= scenarioDataList.Count - 1;
    }

    private int[] GenerateRandomLayoutOrder()
    {
        int[] layout = new int[] { 0, 1, 2, 3 };
        for (int i = 0; i < layout.Length; i++)
        {
            int rand = Random.Range(i, layout.Length);
            (layout[i], layout[rand]) = (layout[rand], layout[i]);
        }
        return layout;
    }

    private string GetRandomName(Gender gender)
    {
        if (gender == Gender.Male && maleNames.Count > 0)
            return maleNames[Random.Range(0, maleNames.Count)];
        else if (gender == Gender.Female && femaleNames.Count > 0)
            return femaleNames[Random.Range(0, femaleNames.Count)];
        else
            return "Alex"; // fallback
    }

    private string GetRandomLastName()
    {
        if (lastNames != null && lastNames.Count > 0)
            return lastNames[Random.Range(0, lastNames.Count)];
        else
            return "Smith"; // fallback
    }

    [Button("PrintNextScenarioDebugOnly")]
    public void PrintNextScenarioDebugOnly()
    {
        Debug.Log("Next Scenario:");
        ScenarioData scenario = GetNextScenario();
        if (scenario != null)
        {
            Debug.Log($"Description: {scenario.ScenarioDescription}");
            Debug.Log($"Defendant: {scenario.DefendantFirstName} {scenario.DefendantLastName}");
            Debug.Log($"Victim: {scenario.VictimFirstName} {scenario.VictimLastName}");
            Debug.Log($"Defendant Photo: {scenario.DefendantPhoto.name}");
            Debug.Log($"Victim Photo: {scenario.VictimPhoto.name}");
            Debug.Log($"Layout Order: {string.Join(", ", scenario.LayoutOrder)}");
        }
        else
        {
            Debug.LogWarning("No scenario available.");
        }
    }
}
