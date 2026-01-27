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
    [SerializeField] private AmountChooser bailAmountChooser;

    private BailOption[] bailOptions;
    private BailOptionType choice;
    private UniTaskCompletionSource tcs;

    private float timeShowed;
    private float timeChosen;


    private void Awake()
    {
        bailOptions = GetComponentsInChildren<BailOption>();
        Debug.Log($"Bail: found {bailOptions.Length} bail options");
    }


    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        foreach (BailOption option in bailOptions)
        {
            option.SetDefendantName(scenarioData.DefendantFirstName, scenarioData.DefendantLastName);
        }
        // replace the name in the title
        string newTitle = title.text.Replace("Doe", scenarioData.DefendantLastName);
        title.text = newTitle;

        bailAmountChooser.SetDefendantNameAndIncomeOnBail(scenarioData.DefendantFirstName, scenarioData.DefendantLastName, scenarioData.AnnualIncome);
    }

    public async UniTask<(BailOptionType, float)> ShowUntilChoiceMade()
    {
        float bailAmount = -1f;
        Show();
        timeShowed = Time.time;
        //bailAmountChooser.Hide(); // old flow: bail amount chooser only shows if ROB is chosen. new flow: always show it, but disable ROB button until amount is touched. for that the confirm button for the slider and for the desicion on bail desition are THE SAME BUTTON 
        await UniTask.Yield(); // ensure buttons are initialized before awaiting

        // initially disable ROB button until bail amount is touched
        ROBButton.SetButtonEnabled(false);
        bailAmountChooser.sliderWasTouched += EnableROBbutton;
        bailAmountChooser.ShowAndWaitForAmount().Forget();

        // wait for one of the buttons to be pressed
        tcs = new UniTaskCompletionSource();
        ROBButton.VRButtonPressed.AddListener(ROBwasPresses);
        RORButton.VRButtonPressed.AddListener(RORwasPressed);
        JailButton.VRButtonPressed.AddListener(JailWasPressed);
        await tcs.Task;
        timeChosen = Time.time;

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Confirmed");

        if (choice == BailOptionType.ROB)
        {
            bailAmount = bailAmountChooser.Amount;
        }

        TXRDataManager.Instance.ReportDecision(MainExperiment.Instance.ScenarioIndex, "Bail", choice.ToString(), bailAmount, -1, -1, timeChosen - timeShowed);

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

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Shown");
    }

    public void Hide()
    {
        title.gameObject.SetActive(false);
        SetOptionsVisibility(false);
        bailAmountChooser.Hide();

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Hidden");
    }

    private void SetOptionsVisibility(bool visible)
    {
        foreach (BailOption option in bailOptions)
        {
            if (visible) option.Show();
            else option.Hide();
        }
    }

    private void EnableROBbutton()
    {
        ROBButton.SetButtonEnabled(true);
    }

}
