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


    UniTaskCompletionSource tcs;
    private void Start()
    {
        Hide();
    }
    public async UniTask<bool> ShowUntilChoiceMade()
    {
        Debug.Log("Bail: Show until choice made");
        Show();
        Debug.Log("Bail: Show until choice made - after show");
        await UniTask.Delay(1000);

        tcs = new UniTaskCompletionSource();
    

        ChoiceManager.ChoiceMade.AddListener(OnPressed);
        Debug.Log("Bail: Show until choice made - after add listener");
        await tcs.Task;
        Debug.Log("Bail: Show until choice made - after await");
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
