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
        await PlayBoxes(scenarioData);
    }

    private async UniTask PlayLegalScenario(ScenarioData scenarioData)
    {
        // TODO

    }

    private async UniTask PlayBoxesInstructions(ScenarioData scenarioData)
    {
        //TODO
    }
    private async UniTask PlayBoxes(ScenarioData scenarioData)
    {
        boxes.LoadScenarioAssets(scenarioData);
        await boxes.ShowBoxesAndWaitForAll();
    }
    private async UniTask PlayDesicion(ScenarioData scenarioData)
    {
        //TODO
    }
    private async UniTask PlayEndScenarioInstructions(ScenarioData scenarioData)
    {
        //TODO
    }

}
