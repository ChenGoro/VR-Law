using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using TMPro;

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
        await UniTask.Delay(1000);

        var tcs = new UniTaskCompletionSource();
        UnityAction onPressed = () => tcs.TrySetResult();

        confirmButton.VRButtonPressed.AddListener(onPressed);
        await tcs.Task;
        confirmButton.VRButtonPressed.RemoveListener(onPressed);

        gameObject.SetActive(false);
    }

    public void Hide() => gameObject.SetActive(false);
}
