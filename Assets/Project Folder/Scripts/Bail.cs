using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Bail : MonoBehaviour
{
    [SerializeField] private BinaryChoiceManager ChoiceManager;
    [SerializeField] private TextMeshPro text;
    [SerializeField] private TextMeshPro title;

    private bool choice;
    private UniTaskCompletionSource tcs;

    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        string fullName = $"{scenarioData.DefendantFirstName} {scenarioData.DefendantLastName}";
        title.text = $"Bail decision for {fullName}";
    }

    public async UniTask<bool> ShowUntilChoiceMade()
    {
        Show();
        await UniTask.Delay(1000);

        tcs = new UniTaskCompletionSource();
        ChoiceManager.ChoiceMade.AddListener(OnPressed);
        await tcs.Task;
        ChoiceManager.ChoiceMade.RemoveListener(OnPressed);

        Hide();
        return choice;
    }

    public void OnPressed(bool choice)
    {
        tcs.TrySetResult();
        this.choice = choice;
    }

    public void Show()
    {
        text.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        ChoiceManager.Show();
    }

    public void Hide()
    {
        text.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        ChoiceManager.hide();
    }
}
