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

    private void Start()
    {
        generalInstructions.Hide();
        legalScenario.Hide();
        boxesInstructions.Hide();
        boxes.Hide();
        endSenarioInstructions.Hide();
        bailDecision.Hide();
    }

    public async UniTask PlayScenario(ScenarioData scenarioData)
    {
        await PlayLegalScenario(scenarioData); // scenario description
        //await PlayBoxesInstructions(scenarioData);
        await PlayBoxes(scenarioData);
        await PlayDesicion(scenarioData);
        await PlayEndScenarioInstructions(scenarioData);
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
                Debug.Log("No sentencing component implemented yet");
                break;
        }
    }

    private async UniTask PlayEndScenarioInstructions(ScenarioData scenarioData)
    {

        await endSenarioInstructions.ShowUntilConfirm();
    }
}
