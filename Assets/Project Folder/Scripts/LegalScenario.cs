using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LegalScenario : MonoBehaviour
{
    [SerializeField] private VR_Button confirmButton;
    [SerializeField] private TextMeshPro scenarioText;

    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        scenarioText.text = scenarioData.ScenarioDescription;
    }

    public async UniTask ShowUntilConfirm()
    {
        gameObject.SetActive(true);
        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Shown");
        TtsCaller.I.SpeakNow(scenarioText.text);

        await UniTask.Delay(1000);

        var tcs = new UniTaskCompletionSource();
        UnityAction onPressed = () => tcs.TrySetResult();

        confirmButton.VRButtonPressed.AddListener(onPressed);
        await tcs.Task;
        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Confirmed");
        confirmButton.VRButtonPressed.RemoveListener(onPressed);
        TtsCaller.I.Cancel(); // in case TTS is still speaking, stop it.


        gameObject.SetActive(false);
        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Hidden");
    }

    public void Hide() => gameObject.SetActive(false);
}
