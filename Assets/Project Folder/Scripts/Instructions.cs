using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Instructions : MonoBehaviour
{
    [SerializeField] private VR_Button confirmButton;
    [SerializeField] private TextMeshPro instructionText;


    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        Debug.Log($"[Instructions] instructionText = {instructionText}, instructionText.text = {instructionText.text}, scenarioData = {scenarioData}, scenarioData.ScenarioDescription = {scenarioData.ScenarioDescription}");
        instructionText.text = scenarioData.ScenarioDescription;
    }

    public async UniTask ShowUntilConfirm()
    {
        Show();
        await UniTask.Delay(1000);

        var tcs = new UniTaskCompletionSource();
        UnityAction onPressed = () => tcs.TrySetResult();

        confirmButton.VRButtonPressed.AddListener(onPressed);
        await tcs.Task;
        confirmButton.VRButtonPressed.RemoveListener(onPressed);

        Hide();
    }

    public void Show()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);
    }

    public void Hide()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }
}
