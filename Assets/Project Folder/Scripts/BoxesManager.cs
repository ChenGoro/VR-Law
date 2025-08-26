using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoxesManager : MonoBehaviour
{
    /// <summary>
    /// in charge of the four boxes game objects in each scenario. has a list of boxes, the contents (statements+photos), continue button.
    /// loads the assets to the boxes prefab, controls the visibility (showing and hiding) the boxes stage in the experiment.
    /// </summary>
    public List<Box> boxes;
    public VR_Button continueButton;

    public TextMeshPro ProsecutorStatement;
    public TextMeshPro AttorneyStatement;
    public SpriteRenderer DefandantPhoto;
    public SpriteRenderer VictimPhoto;
    public GameObject ContinueInstructions;
    public int ContinueInstructionsShowTime = 3;

    private int viewedCount = 0;
    private UniTaskCompletionSource allBoxesViewedTCS;
    private UniTaskCompletionSource ContinuePressedTCS;

    private void Start()
    {
        continueButton.gameObject.SetActive(false);
    }


    public void LoadScenarioAssets(ScenarioData scenario)
    {
        ProsecutorStatement.text = scenario.ProcecutorStatement;
        AttorneyStatement.text = scenario.AttorneyStatement;
        DefandantPhoto.sprite = scenario.DefendantPhoto;
        VictimPhoto.sprite = scenario.VictimPhoto;

        // change order of boxes based on layout order
        // order is set by the order of children in the hirarchy
        SetBoxesOrder(scenario.LayoutOrder);

    }

    private void SetBoxesOrder(int[] layoutOrder)
    {
        HorizontalObjectLayoutGroup layoutGroup = GetComponent<HorizontalObjectLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("BoxesManager: No HorizontalObjectLayoutGroup component found on the GameObject. cant update boxes order");
        }
        for (int i = 0; i < boxes.Count; i++)
        {
            int layoutIndex = layoutOrder[i];
            Box box = boxes[layoutIndex];
            box.transform.SetSiblingIndex(i);
        }
        layoutGroup.UpdateLayout();
    }

    public void Hide()
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

        ShowContinueInstructionsForSeconds(ContinueInstructionsShowTime).Forget();

        await allBoxesViewedTCS.Task;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            ContinuePressedTCS = new UniTaskCompletionSource();
            continueButton.VRButtonPressed.AddListener(OnContinuePressed);
            await ContinuePressedTCS.Task;
            continueButton.VRButtonPressed.RemoveAllListeners();
            Hide();
            viewedCount = 0;
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

    public async UniTask ShowContinueInstructionsForSeconds(int seconds)
    {
        ContinueInstructions.SetActive(true);
        await UniTask.Delay(seconds * 1000);
        ContinueInstructions.SetActive(false);
    }
}
