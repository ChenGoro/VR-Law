using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class MainExperiment : MonoBehaviour
{
    private async void Start()
    {
        await UniTask.Yield();

        await SceneReferencer.Instance.instructions.ShowUntilConfirm();
        await SceneReferencer.Instance.legalScenario.ShowUntilConfirm();
        await SceneReferencer.Instance.boxesManager.ShowBoxesAndWaitForAll();
        await WaitForVRButtonPress(SceneReferencer.Instance.continueButton);

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
