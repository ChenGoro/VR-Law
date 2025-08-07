using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


[Serializable]
public class Scenario
    
{
    [Serialize]
    public string scenarioName;
    public GameObject legalScenarioPrefab;
    public GameObject boxesInstructionsPrefab;
    public GameObject boxesPrefab;
    public GameObject endSenarioInstructionsPrefab;
    public GameObject desicionPrefab;
    public Decision decisionType;


    public async UniTask PlayScenario()
    {
        await PlayLegalScenario();
        await PlayBoxesInstructions();
        await PlayBoxes();
        await PlayDesicion();
        await PlayEndScenarioInstructions();
        
    }

    private async UniTask PlayLegalScenario()
    {
        GameObject legalScenarioInstance = GameObject.Instantiate(legalScenarioPrefab, ScenarioLoader.Instance.transform);
        LegalScenario legalScenarioComponent = legalScenarioInstance.GetComponent<LegalScenario>();

        await legalScenarioComponent.ShowUntilConfirm();

        GameObject.Destroy(legalScenarioInstance);
    }

    private async UniTask PlayBoxes()
    {
        GameObject boxesInstance = GameObject.Instantiate(boxesPrefab, ScenarioLoader.Instance.transform);
        BoxesManager boxesManagerComponent = boxesInstance.GetComponent<BoxesManager>();

        await boxesManagerComponent.ShowBoxesAndWaitForAll();

        GameObject.Destroy(boxesInstance);
    }

    private async UniTask PlayDesicion()
    {
        GameObject desicionInstance = GameObject.Instantiate(desicionPrefab, ScenarioLoader.Instance.transform);
        
        switch (decisionType)
        {
            case (Decision.Bail):
                Bail bailComponent = desicionInstance.GetComponent<Bail>();
                bool bailChoice;
                bailChoice = await bailComponent.ShowUntilChoiceMade();
                TXRDataManager.Instance.LogLineToFile($"{scenarioName}: bail choice  =  {bailChoice}");
                break; 
            case (Decision.Sentencing):

                Debug.Log("No sentencing component available yet!");

                break;
        }

        GameObject.Destroy(desicionInstance);
    }

    private async UniTask PlayEndScenarioInstructions()
    {
        GameObject InstructionsInstance = GameObject.Instantiate(endSenarioInstructionsPrefab, ScenarioLoader.Instance.transform);
        Instructions instructionsComponent = InstructionsInstance.GetComponent<Instructions>();

        await instructionsComponent.ShowUntilConfirm();
        GameObject.Destroy(InstructionsInstance);
    }

    private async UniTask PlayBoxesInstructions()
    {
        GameObject InstructionsInstance = GameObject.Instantiate(boxesInstructionsPrefab, ScenarioLoader.Instance.transform);
        Instructions instructionsComponent = InstructionsInstance.GetComponent<Instructions>();

        await instructionsComponent.ShowUntilConfirm();
        GameObject.Destroy(InstructionsInstance);
    }

    public enum Decision { Bail, Sentencing }; 
}
