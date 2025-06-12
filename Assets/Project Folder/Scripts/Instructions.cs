using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class Instructions : MonoBehaviour
{
    [SerializeField] private VR_Button confirmButton;

    private void Start() => Hide();

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
