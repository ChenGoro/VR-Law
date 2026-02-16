using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LegalScenario : MonoBehaviour
{
    [SerializeField] private VR_Button confirmButton;
    [SerializeField] private TextMeshPro scenarioText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private KaraokeTextHighlighter karaokeHighlighter;
    [SerializeField] private Transform backfaceTransform;
    [SerializeField] private VR_Button replayAudioButton;
    [SerializeField] private TMPBackplateResizer tMPBackplateResizer;
    [SerializeField] private float panelButtonOffset;

    public void LoadScenarioAssets(ScenarioData scenarioData)
    {
        NarrativeBlock block = scenarioData.DescriptionBlock;
        if (ShouldLogDynamicTexts())
            Debug.Log($"[DebugDynamicTexts] LegalScenario LOADED ({block.Text?.Length ?? 0} chars). Has \\n: {block.Text?.Contains("\n") ?? false}, Has \\t: {block.Text?.Contains("\t") ?? false}\n{Repr(block.Text)}");

        scenarioText.text = block.Text;

        if (audioSource != null)
            audioSource.clip = block.AudioClip;

        if (karaokeHighlighter != null && !string.IsNullOrEmpty(block.WordTimesJson))
            karaokeHighlighter.LoadFromJson(block.WordTimesJson, "LegalScenario");

        if (ShouldLogDynamicTexts() && scenarioText != null)
            Debug.Log($"[DebugDynamicTexts] LegalScenario FINAL TMP ({scenarioText.text?.Length ?? 0} chars). Has \\n: {scenarioText.text?.Contains("\n") ?? false}, Has \\t: {scenarioText.text?.Contains("\t") ?? false}\n{Repr(scenarioText.text)}");
    }

    private static bool ShouldLogDynamicTexts() =>
        SceneReferencer.Instance != null && SceneReferencer.Instance.DebugLogDynamicTexts;

    private static string Repr(string s)
    {
        if (s == null) return "(null)";
        return s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    public async UniTask ShowUntilConfirm()
    {
        gameObject.SetActive(true);
        if (tMPBackplateResizer != null)
            tMPBackplateResizer.UpdateBackplate();
        AlignPanelToButtonOffset();

        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Shown");

        if (karaokeHighlighter != null && audioSource != null && audioSource.clip != null)
        {
            karaokeHighlighter.StartPlayback();
        }

        await UniTask.Delay(500);

        var tcs = new UniTaskCompletionSource();
        UnityAction onPressed = () => tcs.TrySetResult();

        confirmButton.VRButtonPressed.AddListener(onPressed);
        await tcs.Task;
        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Confirmed");
        confirmButton.VRButtonPressed.RemoveListener(onPressed);

        if (karaokeHighlighter != null)
            karaokeHighlighter.StopPlayback();

        gameObject.SetActive(false);
        TXRDataManager.Instance.ReportPanelOrConfirmationEvent(MainExperiment.Instance.ScenarioIndex, name, "Hidden");
    }

    public void Hide() => gameObject.SetActive(false);

    /// <summary>
    /// Replays the scenario audio from the start. Hook this to the replay button's VRButtonPressed in the inspector.
    /// If already playing, stops and restarts from the beginning.
    /// </summary>
    public void ReplayAudio()
    {
        if (karaokeHighlighter != null)
            karaokeHighlighter.StopPlayback();
        if (audioSource != null)
            audioSource.time = 0f;
        if (karaokeHighlighter != null && audioSource != null && audioSource.clip != null)
            karaokeHighlighter.StartPlayback();
    }

    /// <summary>
    /// Moves scenarioText and backface so the distance from button center to the bottom of the resizer's bounds equals panelButtonOffset.
    /// Call after UpdateBackplate() when the panel is shown.
    /// </summary>
    private void AlignPanelToButtonOffset()
    {
        if (tMPBackplateResizer == null || scenarioText == null || backfaceTransform == null || confirmButton == null)
            return;

        // Use the resizer's text bounds (TMP local space); bottom center in world
        Bounds b = tMPBackplateResizer.LastBounds;
        Vector3 localBottomCenter = new Vector3(b.center.x, b.min.y, b.center.z);
        float currentBottomWorldY = scenarioText.transform.TransformPoint(localBottomCenter).y;
        float targetBottomY = confirmButton.transform.position.y + panelButtonOffset;
        float deltaY = targetBottomY - currentBottomWorldY;

        scenarioText.transform.position += new Vector3(0f, deltaY, 0f);
        backfaceTransform.position += new Vector3(0f, deltaY, 0f);
    }
}
