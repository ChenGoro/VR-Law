using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainExperiment : TXRSingleton<MainExperiment>
{
    /// <summary>
    /// main experiment flow runner. it uses scenario manager to load the scenarios, and scenarioplayer to play each scenario.
    /// runs the general instruction for the begiining of the experiment.
    /// </summary>
    private SceneReferencer sceneReferencer;


    private int scenarioIndex = 0;
    public int ScenarioIndex => scenarioIndex;

    private async void Start()
    {
        Init();
        await UniTask.Yield();

        await sceneReferencer.generalInstructions.ShowUntilConfirm();
        TXRDataManager.Instance.LogLineToFile("confirmed starting instructions");

        ScenarioManager scenarioManager = sceneReferencer.scenarioManager;
        ScenarioPlayer scenarioPlayer = sceneReferencer.scenarioPlayer;

        while (scenarioManager.HasNextScenario())
        {
            ScenarioData currentScenario = scenarioManager.GetNextScenario();

            scenarioIndex = currentScenario.ScenarioIndex;
            TXRDataManager.Instance.ReportScenarioInfo(currentScenario);

            bool isLastScenario = IsLastScenario(scenarioIndex);
            await scenarioPlayer.PlayScenario(currentScenario, isLastScenario);
        }

        await sceneReferencer.endOfExperimentInstructions.ShowUntilConfirm();

        // quit application
        Application.Quit();
    }

    private void Init()
    {
        sceneReferencer = SceneReferencer.Instance;
        sceneReferencer.generalInstructions.Hide();
        sceneReferencer.endOfExperimentInstructions.Hide();
    }

    private bool IsLastScenario(int index)
    {
        return !sceneReferencer.scenarioManager.HasNextScenario();
    }
}
