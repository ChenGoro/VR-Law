using UnityEngine;

public class SceneReferencer : TXRSingleton<SceneReferencer>
{
    [Header("Global Settings")]
    [SerializeField] private bool shouldRandomizeName = true;
    public bool ShouldRandomizeName => shouldRandomizeName;

    [Header("Debug")]
    [Tooltip("When true, log dynamic text at each stage (loaded, JSON/timings, final TMP) for Legal Scenario and statement boxes. Use [DebugDynamicTexts] to filter in Console.")]
    [SerializeField] private bool debugLogDynamicTexts;
    public bool DebugLogDynamicTexts => debugLogDynamicTexts;

    [Header("References")]
    public Instructions generalInstructions;
    public Instructions endOfExperimentInstructions;
    public ScenarioManager scenarioManager;
    public ScenarioPlayer scenarioPlayer;

}
