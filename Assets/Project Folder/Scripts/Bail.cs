using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Bail : MonoBehaviour
/***
 * in charge of the bail game component, has the text prompt (textMeshPro) and the choices buttons (binaryChoiceManager)
 */
{
    [SerializeField] private TextMeshPro title;
    [SerializeField] private VR_Button RORButton;
    [SerializeField] private VR_Button ROBButton;
    [SerializeField] private VR_Button JailButton;
    [SerializeField] private BailAmountChooser bailAmountChooser;

    private BailOption[] bailOptions;
    private BailOptionType choice;
    private UniTaskCompletionSource tcs;

    private void Start()
    {
        bailOptions = GetComponentsInChildren<BailOption>();
    }


    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        string fullName = $"{scenarioData.DefendantFirstName} {scenarioData.DefendantLastName}";
        // change the options text so that the defandats name appears TODO
    }

    public async UniTask<(BailOptionType, float)> ShowUntilChoiceMade()
    {
        float bailAmount = -1f;
        Show();
        //bailAmountChooser.Hide(); // old flow: bail amount chooser only shows if ROB is chosen 
        await UniTask.Yield(); // ensure buttons are initialized before awaiting

        tcs = new UniTaskCompletionSource();
        bailAmountChooser.ShowAndWaitForBailAmount().Forget();
        ROBButton.VRButtonPressed.AddListener(ROBwasPresses);
        RORButton.VRButtonPressed.AddListener(RORwasPressed);
        JailButton.VRButtonPressed.AddListener(JailWasPressed);
        await tcs.Task;

        if (choice == BailOptionType.ROB)
        {
            bailAmount = bailAmountChooser.BailAmount;

        }

        await bailAmountChooser.CancelWait();

        //if (choice == BailOptionType.ROB) // old flow: bail amount chooser only shows if ROB is chosen
        //{
        //    SetOptionsVisibility(false);
        //    bailAmount = await bailAmountChooser.ShowAndWaitForBailAmount();
        //}

        Hide();
        return (choice, bailAmount);
    }

    private void ROBwasPresses()
    {
        choice = BailOptionType.ROB;
        tcs.TrySetResult();
    }

    private void RORwasPressed()
    {
        choice = BailOptionType.ROR;
        tcs.TrySetResult();
    }
    private void JailWasPressed()
    {
        choice = BailOptionType.Jail;
        tcs.TrySetResult();
    }


    public void Show()
    {
        title.gameObject.SetActive(true);
        SetOptionsVisibility(true);

    }

    public void Hide()
    {
        title.gameObject.SetActive(false);
        SetOptionsVisibility(false);
        bailAmountChooser.Hide();
    }

    private void SetOptionsVisibility(bool visible)
    {
        foreach (var option in bailOptions)
        {
            if (visible) option.Show();
            else option.Hide();
        }
    }

}
