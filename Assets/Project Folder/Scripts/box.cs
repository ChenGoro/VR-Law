using NaughtyAttributes;
using TMPro;
using UnityEngine;


public class Box : MonoBehaviour
{
    public GameObject boxMesh;
    public GameObject title;
    public GameObject content;
    public VR_Button closeButton;

    public BoxType boxType;
 

    private bool IsStatementBox =>
    boxType == BoxType.AttorneysStatement || boxType == BoxType.ProsecutorsStatement;
    private bool IsPhotoBox =>
        boxType == BoxType.DefendantPhoto || boxType == BoxType.VictimPhoto;

    [SerializeField, ShowIf("IsStatementBox")]
    private TextMeshPro contentText;
    [SerializeField, ShowIf("IsStatementBox")]
    private AudioSource audioSource;
    [SerializeField, ShowIf("IsStatementBox")]
    private KaraokeTextHighlighter karaokeHighlighter;
    [SerializeField, ShowIf("IsPhotoBox")]
    private SpriteRenderer spriteRenderer;

    private NarrativeBlock _statementBlock;
    private BoxesManager _boxesManager;
    private bool wasOpened = false;
    private System.Action onBoxViewed;
    private TXRDataManager dataManager;
    private int layoutOrder = -1;

    [SerializeField] private TMPBackplateResizer tMPBackplateResizer;
    [SerializeField]    private float boxButtonOffset = 0.1f;

    private void Awake()
    {
        CheckInspectorReferences();

        content.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private void CheckInspectorReferences()
    {
        if (boxMesh == null)
            Debug.LogError($"{name}: boxMesh reference is missing in the inspector.");
        if (title == null)
            Debug.LogError($"{name}: title reference is missing in the inspector.");
        if (content == null)
            Debug.LogError($"{name}: content reference is missing in the inspector.");
        if (IsStatementBox && contentText == null)
            Debug.LogError($"{name}: contentText reference is missing in the inspector for a statement box.");
        if (IsPhotoBox && spriteRenderer == null)
            Debug.LogError($"{name}: sprite reference is missing in the inspector for a photo box.");
    }

    public void Init(System.Action onViewedCallback, BoxesManager boxesManager = null)
    {
        dataManager = TXRDataManager.Instance;
        onBoxViewed = onViewedCallback;
        _boxesManager = boxesManager;
        wasOpened = false;
        boxMesh.SetActive(true);
        title.SetActive(true);
        content.SetActive(false);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.VRButtonPressed.RemoveAllListeners();
            closeButton.VRButtonPressed.AddListener(HideContent);
        }
        // layout order is the index of the box among its siblings
        layoutOrder = transform.GetSiblingIndex();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Toucher") && boxMesh.activeSelf)
        {
            OnBoxClicked();
        }
    }

    private void OnBoxClicked()
    {
        ShowContent();
    }

    private void ShowContent()
    {
        content.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        if (!wasOpened)
        {
            wasOpened = true;
            dataManager.ReportBoxEvent(MainExperiment.Instance.ScenarioIndex, boxType.ToString(), layoutOrder, "opened");
            onBoxViewed?.Invoke();
        }

        if (IsStatementBox && _statementBlock != null)
        {
            _boxesManager?.NotifyStatementBoxShowing(this);
            if (karaokeHighlighter != null)
                karaokeHighlighter.StopPlayback();
            if (audioSource != null)
                audioSource.clip = _statementBlock.AudioClip;
            if (karaokeHighlighter != null && !string.IsNullOrEmpty(_statementBlock.WordTimesJson))
                karaokeHighlighter.LoadFromJson(_statementBlock.WordTimesJson, boxType.ToString());
            if (tMPBackplateResizer != null)
                tMPBackplateResizer.UpdateBackplate();
            AlignContentToButtonOffset();
            if (karaokeHighlighter != null && audioSource != null && audioSource.clip != null)
                karaokeHighlighter.StartPlayback();
        }
    }

    private void HideContent()
    {
        if (IsStatementBox && karaokeHighlighter != null)
        {
            karaokeHighlighter.StopPlayback();
            _boxesManager?.NotifyStatementBoxHidden(this);
        }
        content.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        dataManager.ReportBoxEvent(MainExperiment.Instance.ScenarioIndex, boxType.ToString(), layoutOrder, "closed");
    }

    public void StopPlaybackIfStatement()
    {
        if (IsStatementBox && karaokeHighlighter != null)
            karaokeHighlighter.StopPlayback();
    }

    public void LoadScenarioAssets(ScenarioData scenario)
    {
        switch (boxType)
        {
            case BoxType.AttorneysStatement:
                if (ShouldLogDynamicTexts())
                    Debug.Log($"[DebugDynamicTexts] AttorneyStatement LOADED ({scenario.AttorneyStatement?.Length ?? 0} chars). Has \\n: {scenario.AttorneyStatement?.Contains("\n") ?? false}, Has \\t: {scenario.AttorneyStatement?.Contains("\t") ?? false}\n{Repr(scenario.AttorneyStatement)}");
                contentText.text = scenario.AttorneyStatement;
                _statementBlock = scenario.AttorneyBlock;
                break;
            case BoxType.ProsecutorsStatement:
                if (ShouldLogDynamicTexts())
                    Debug.Log($"[DebugDynamicTexts] ProsecutorStatement LOADED ({scenario.ProcecutorStatement?.Length ?? 0} chars). Has \\n: {scenario.ProcecutorStatement?.Contains("\n") ?? false}, Has \\t: {scenario.ProcecutorStatement?.Contains("\t") ?? false}\n{Repr(scenario.ProcecutorStatement)}");
                contentText.text = scenario.ProcecutorStatement;
                _statementBlock = scenario.ProsecutorBlock;
                break;
            case BoxType.DefendantPhoto:
                spriteRenderer.sprite = scenario.DefendantPhoto.Sprite;
                _statementBlock = null;
                break;
            case BoxType.VictimPhoto:
                spriteRenderer.sprite = scenario.VictimPhoto.Sprite;
                _statementBlock = null;
                break;
            default:
                Debug.LogError($"{name}: Unknown box type {boxType}");
                break;
        }
    }

    private static bool ShouldLogDynamicTexts() =>
        SceneReferencer.Instance != null && SceneReferencer.Instance.DebugLogDynamicTexts;

    private static string Repr(string s)
    {
        if (s == null) return "(null)";
        return s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    /// <summary>
    /// Moves content (text + backface as child) so the distance from closeButton center to the bottom of the resizer's bounds equals boxButtonOffset.
    /// Call after UpdateBackplate() when showing statement content.
    /// </summary>
    private void AlignContentToButtonOffset()
    {
        if (!IsStatementBox || tMPBackplateResizer == null || contentText == null || content == null || closeButton == null)
            return;

        Bounds b = tMPBackplateResizer.LastBounds;
        Vector3 localBottomCenter = new Vector3(b.center.x, b.min.y, b.center.z);
        float currentBottomWorldY = contentText.transform.TransformPoint(localBottomCenter).y;
        float targetBottomY = closeButton.transform.position.y + boxButtonOffset;
        float deltaY = targetBottomY - currentBottomWorldY;

        content.transform.position += new Vector3(0f, deltaY, 0f);
    }
}

public enum BoxType
{
    AttorneysStatement,
    ProsecutorsStatement,
    DefendantPhoto,
    VictimPhoto
}
