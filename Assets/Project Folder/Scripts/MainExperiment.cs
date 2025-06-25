using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using Oculus.Interaction.Samples;

public class MainExperiment : MonoBehaviour
{
    SceneReferencer sceneReferencer;
    private async void Start()
    {
        Init();
        await UniTask.Yield();

        await sceneReferencer.generalInstructions.ShowUntilConfirm();

        ScenarioLoader scenarioLoader = sceneReferencer.scenarioLoader;
        while (scenarioLoader.HasNext())
        {
            Scenario currentScenario = scenarioLoader.GetNextScenario();
            await currentScenario.PlayScenario();
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
