using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class BoxesManager : MonoBehaviour
{
    public List<Box> boxes;
    public VR_Button continueButton;

    private int viewedCount = 0;
    private UniTaskCompletionSource allBoxesViewedTCS;
    private UniTaskCompletionSource ContinuePressedTCS;
    private void Start()
    {
        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        foreach (var box in boxes)
            box.gameObject.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    public async UniTask ShowBoxesAndWaitForAll()
    {
        gameObject.SetActive(true);
        viewedCount = 0;
        allBoxesViewedTCS = new UniTaskCompletionSource();

        Debug.Log("inside BoxesManagers ShowBoxesAndWaitForAll before for each");

        foreach (var box in boxes)
        {
            box.gameObject.SetActive(true);
            box.Init(OnBoxViewed);

            Debug.Log("inside BoxesManagers ShowBoxesAndWaitForAll after for each");
        }

        await allBoxesViewedTCS.Task;

        if (continueButton != null)
        { 
            continueButton.gameObject.SetActive(true);
            ContinuePressedTCS = new UniTaskCompletionSource();
            continueButton.VRButtonPressed.AddListener(OnContinuePressed);
            await ContinuePressedTCS.Task;
            continueButton.VRButtonPressed.RemoveAllListeners();
            Hide();



        }
    }
private void OnContinuePressed()
    {
        ContinuePressedTCS.TrySetResult();
    }
    private void OnBoxViewed()
    {
        viewedCount++;
        if (viewedCount >= boxes.Count)
            allBoxesViewedTCS.TrySetResult();
    }
}
