using Cysharp.Threading.Tasks;
using UnityEngine.Events;

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

            await scenarioPlayer.PlayScenario(currentScenario);
        }

    }

    private void Init()
    {
        sceneReferencer = SceneReferencer.Instance;
        sceneReferencer.generalInstructions.Hide();
    }

    private async UniTask WaitForVRButtonPress(VR_Button button)
    {
        var tcs = new UniTaskCompletionSource();
        UnityAction listener = () => tcs.TrySetResult();
        button.VRButtonPressed.AddListener(listener);
        await tcs.Task;
        button.VRButtonPressed.RemoveListener(listener);
    }
}
