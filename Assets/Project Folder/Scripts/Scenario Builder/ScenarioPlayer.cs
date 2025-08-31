using Cysharp.Threading.Tasks;
using UnityEngine;

public class ScenarioPlayer : MonoBehaviour
{
    public Instructions generalInstructions;
    public LegalScenario legalScenario;
    public Instructions boxesInstructions;
    public BoxesManager boxes;
    public Instructions endSenarioInstructions;
    public Bail bailDecision;
    public Sentence sentenceDecision;

    private void Start()
    {
        generalInstructions.Hide();
        legalScenario.Hide();
        boxesInstructions.Hide();
        boxes.Hide();
        endSenarioInstructions.Hide();
        bailDecision.Hide();
        sentenceDecision.Hide();
    }

    //is last scenario is used to skip the end scenario instructions
    public async UniTask PlayScenario(ScenarioData scenarioData, bool isLastScenario = false)
    {
        await PlayLegalScenario(scenarioData); // scenario description
        //await PlayBoxesInstructions(scenarioData);
        await PlayBoxes(scenarioData);
        await PlayDesicion(scenarioData);
        if (!isLastScenario) await PlayEndScenarioInstructions(scenarioData);
    }

    private async UniTask PlayLegalScenario(ScenarioData scenarioData)
    {
        legalScenario.LoadScenarioAssets(scenarioData);
        await legalScenario.ShowUntilConfirm();
    }

    private async UniTask PlayBoxesInstructions(ScenarioData scenarioData)
    {
        boxesInstructions.LoadScenarioAssets(scenarioData);
        await boxesInstructions.ShowUntilConfirm();
    }

    private async UniTask PlayBoxes(ScenarioData scenarioData)
    {
        boxes.LoadScenarioAssets(scenarioData);
        await boxes.ShowBoxesAndWaitForAll();
    }

    private async UniTask PlayDesicion(ScenarioData scenarioData)
    {
        switch (scenarioData.ScenarioType)
        {
            case ScenarioType.Bail:
                bailDecision.LoadScenarioAssets(scenarioData);
                BailOptionType bailChoice;
                float bailAmount;
                (bailChoice, bailAmount) = await bailDecision.ShowUntilChoiceMade();
                TXRDataManager.Instance.LogLineToFile($"{scenarioData.ScenarioDescription}: bail choice = {bailChoice}, bail amount = {bailAmount}");
                break;

            case ScenarioType.Sentencing:
                sentenceDecision.LoadScenarioAssets(scenarioData);
                SentenceOptionType sentenceChoice;
                float sentenceLength;
                float fineAmount;
                (sentenceChoice, sentenceLength, fineAmount) = await sentenceDecision.ShowUntilChoiceMade();
                TXRDataManager.Instance.LogLineToFile($"{scenarioData.ScenarioDescription}: sentence choice = {sentenceChoice}, sentence length = {sentenceLength}, fine amount = {fineAmount}");
                break;
        }
    }


    private async UniTask PlayEndScenarioInstructions(ScenarioData scenarioData)
    {
        await endSenarioInstructions.ShowUntilConfirm();
    }

}
