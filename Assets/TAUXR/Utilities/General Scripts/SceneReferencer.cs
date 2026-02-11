using UnityEngine;

public class SceneReferencer : TXRSingleton<SceneReferencer>
{
    [Header("Global Settings")]
    [SerializeField] private bool shouldRandomizeName = true;
    public bool ShouldRandomizeName => shouldRandomizeName;

    [Header("References")]
    public Instructions generalInstructions;
    public Instructions endOfExperimentInstructions;
    public ScenarioManager scenarioManager;
    public ScenarioPlayer scenarioPlayer;

}
