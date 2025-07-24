using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioLoader : TXRSingleton<ScenarioLoader>
{
    [ReadOnly]
    [InfoBox("auto populated with scenarios")]
    public List<Scenario> scenarioList;
    private List<int> scenarioIndexList;

    private int currentIndex;


    private void Start()
    {
        InitIndexList();
        currentIndex = 0;
    }

    public Scenario GetNextScenario()
    {
        Scenario nextScenario = scenarioList[currentIndex];
        currentIndex++;
        return nextScenario;
    }

    public bool HasNext()
    {
        return currentIndex < scenarioList.Count;
    }

    private void InitIndexList()
    {
        int numOfScenarios = scenarioList.Count;
        scenarioIndexList = new List<int>();

        // Fill the list with indices
        for (int i = 0; i < numOfScenarios; i++)
        {
            scenarioIndexList.Add(i);
        }

        scenarioIndexList = ShuffleList(scenarioIndexList);
    }

    private List<int> ShuffleList(List<int> list)
    {
        // Shuffle the list using Fisher-Yates algorithm
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }
}
