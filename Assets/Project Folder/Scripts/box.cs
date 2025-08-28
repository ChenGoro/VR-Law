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
    [SerializeField, ShowIf("IsPhotoBox")]
    private SpriteRenderer spriteRenderer;

    private bool wasOpened = false;
    private System.Action onBoxViewed;
    private TXRDataManager dataManager;
    private int layoutOrder = -1;

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

    public void Init(System.Action onViewedCallback)
    {
        dataManager = TXRDataManager.Instance;
        onBoxViewed = onViewedCallback;
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
            Debug.Log($"{name}: Touched by {other.name}");
            OnBoxClicked();
        }
    }

    private void OnBoxClicked()
    {
        Debug.Log($"{name} was clicked.");
        ShowContent();
    }

    private void ShowContent()
    {
        Debug.Log($"{name}: Showing content...");

        content.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        if (!wasOpened)
        {
            wasOpened = true;
            dataManager.ReportBoxEvent(MainExperiment.Instance.ScenarioIndex, boxType.ToString(), layoutOrder, "opened");
            onBoxViewed?.Invoke();
        }

        if (IsStatementBox)
        {
            TtsCaller.I.SpeakNow(contentText.text);
        }
    }

    private void HideContent()
    {
        Debug.Log($"{name}: Hiding content...");
        content.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        dataManager.ReportBoxEvent(MainExperiment.Instance.ScenarioIndex, boxType.ToString(), layoutOrder, "closed");
    }

    public void LoadScenarioAssets(ScenarioData scenario)
    {
        switch (boxType)
        {
            case BoxType.AttorneysStatement:
                contentText.text = scenario.AttorneyStatement;
                break;
            case BoxType.ProsecutorsStatement:
                contentText.text = scenario.ProcecutorStatement;
                break;
            case BoxType.DefendantPhoto:
                spriteRenderer.sprite = scenario.DefendantPhoto.Sprite;
                break;
            case BoxType.VictimPhoto:
                spriteRenderer.sprite = scenario.VictimPhoto.Sprite;
                break;
            default:
                Debug.LogError($"{name}: Unknown box type {boxType}");
                break;
        }
    }

}

public enum BoxType
{
    AttorneysStatement,
    ProsecutorsStatement,
    DefendantPhoto,
    VictimPhoto
}
