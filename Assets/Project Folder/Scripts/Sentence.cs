using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Sentence : MonoBehaviour
{

    [SerializeField] private TextMeshPro title;
    [SerializeField] private VR_Button sentenceButton;
    [SerializeField] private VR_Button fineButton;

    [SerializeField] private AmountChooser sentenceLengthChooser;
    [SerializeField] private AmountChooser fineAmountChooser;


    private SentenceOption[] sentenceOptions;
    private SentenceOptionType choice;
    private UniTaskCompletionSource tcs;

    private float timeShowed;
    private float timeChosen;

    private void Awake()
    {
        sentenceOptions = GetComponentsInChildren<SentenceOption>();
        Debug.Log($"Sentence: found {sentenceOptions.Length} sentence options");
    }

    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        foreach (SentenceOption option in sentenceOptions)
        {
            option.SetDefendantName(scenarioData.DefendantFirstName, scenarioData.DefendantLastName);
        }
        sentenceLengthChooser.SetDefendantName(scenarioData.DefendantFirstName, scenarioData.DefendantLastName);
        fineAmountChooser.SetDefendantNameAndIncomeOnFine(scenarioData.DefendantFirstName, scenarioData.DefendantLastName, scenarioData.AnnualIncome);
    }

    public async UniTask<(SentenceOptionType, float, float)> ShowUntilChoiceMade()
    {
        float sentenceLength = -1f;
        float fineAmount = -1f;

        Show();
        timeShowed = Time.time;

        // initially disable buttons until amount is touched
        sentenceButton.SetButtonEnabled(false);
        fineButton.SetButtonEnabled(false);
        sentenceLengthChooser.sliderWasTouched += EnableSentenceButton;
        fineAmountChooser.sliderWasTouched += EnableFineButton;

        sentenceLengthChooser.ShowAndWaitForAmount().Forget();
        fineAmountChooser.ShowAndWaitForAmount().Forget();
        // wait for one of the buttons to be pressed
        tcs = new UniTaskCompletionSource();
        sentenceButton.VRButtonPressed.AddListener(SentenceWasPressed);
        fineButton.VRButtonPressed.AddListener(FineWasPressed);
        await tcs.Task;
        timeChosen = Time.time;

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Confirmed");

        switch (choice)
        {
            case SentenceOptionType.Sentence:
                sentenceLength = sentenceLengthChooser.Amount;
                fineAmountChooser.CancelWait().Forget();
                break;
            case SentenceOptionType.Fine:
                fineAmount = fineAmountChooser.Amount;
                sentenceLengthChooser.CancelWait().Forget();
                break;
        }

        TXRDataManager.Instance.ReportDecision(MainExperiment.Instance.ScenarioIndex, "Sentence", choice.ToString(), -1, sentenceLength, fineAmount, timeChosen - timeShowed);

        Hide();
        Debug.Log($"Sentence: choice made: {choice}, sentence length={sentenceLength}, fine amount={fineAmount} after {timeChosen - timeShowed} seconds");
        return (choice, sentenceLength, fineAmount);
    }
    public void EnableSentenceButton()
    {
        sentenceButton.SetButtonEnabled(true);
    }
    public void EnableFineButton()
    {
        fineButton.SetButtonEnabled(true);
    }

    private void FineWasPressed()
    {
        choice = SentenceOptionType.Fine;
        tcs.TrySetResult();

    }

    private void SentenceWasPressed()
    {
        choice = SentenceOptionType.Sentence;
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
        fineAmountChooser.Hide();
        sentenceLengthChooser.Hide();

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Hidden");

    }

    private void SetOptionsVisibility(bool visible)
    {
        foreach (SentenceOption option in sentenceOptions)
        {
            if (visible) option.Show();
            else option.Hide();
        }
    }

}
