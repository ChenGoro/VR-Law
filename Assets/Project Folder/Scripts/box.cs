using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        wasOpened = false;
        boxMesh.SetActive(true);
        title.SetActive(true);
        content.SetActive(false);
        Debug.Log("inside box init before if");
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.VRButtonPressed.RemoveAllListeners();
            closeButton.VRButtonPressed.AddListener(HideContent);


            Debug.Log("inside box init after if");


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
            TXRDataManager.Instance.LogLineToFile($"player opened box. title: {title}, content: {content}");
            onBoxViewed?.Invoke();
        }
    }

    private void HideContent()
    {
        Debug.Log($"{name}: Hiding content...");
        content.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        TXRDataManager.Instance.LogLineToFile("player clicked close");
    }


    }
}
