using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class BoxesManager : MonoBehaviour
{
    public List<Box> boxes;
    public VR_Button continueButton;

    private int viewedCount = 0;
    private UniTaskCompletionSource allBoxesViewedTCS;

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

        foreach (var box in boxes)
        {
            box.gameObject.SetActive(true);
            box.Init(OnBoxViewed);
        }

        await allBoxesViewedTCS.Task;

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private void OnBoxViewed()
    {
        viewedCount++;
        if (viewedCount >= boxes.Count)
            allBoxesViewedTCS.TrySetResult();
    }
}
