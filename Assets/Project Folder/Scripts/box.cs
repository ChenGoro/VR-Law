using UnityEngine;

public class Box : MonoBehaviour
{
    public GameObject boxMesh;
    public GameObject title;
    public GameObject content;
    public VR_Button closeButton;

    private bool wasOpened = false;
    private System.Action onBoxViewed;

    private void Awake()
    {
        content.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
    }

    public void Init(System.Action onViewedCallback)
    {
        onBoxViewed = onViewedCallback;

        boxMesh.SetActive(true);
        title.SetActive(true);
        content.SetActive(false);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.VRButtonPressed.RemoveAllListeners();
            closeButton.VRButtonPressed.AddListener(HideContent);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Toucher") && boxMesh.activeSelf)
        {
            Debug.Log($"{name}: Touched by {other.name}");
            OnBoxClicked();
        }
    }

    public void OnBoxClicked()
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
            onBoxViewed?.Invoke();
        }
    }

    private void HideContent()
    {
        Debug.Log($"{name}: Hiding content...");
        content.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (boxMesh != null)
            boxMesh.SetActive(false);

        if (title != null)
            title.SetActive(false); // 
    }
}
