using UnityEngine;
using UnityEngine.Events;

public class VR_Button : MonoBehaviour
{
    public UnityEvent VRButtonPressed = new UnityEvent();
    public Material disabledMaterial;
    public Renderer buttonGraphics;
    private Material originalMaterial;

    private bool disabled = false;

    private void Awake()
    {
        originalMaterial = buttonGraphics.material;
    }

    public void SetButtonEnabled(bool enabled)
    {
        if (enabled)
        {
            buttonGraphics.material = originalMaterial;
            disabled = false;
        }
        else
        {
            buttonGraphics.material = disabledMaterial;
            disabled = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (disabled) return;
        if (other.gameObject.CompareTag("Toucher"))
        {
            Debug.Log("VRButton: inside OnTriggerEnter, Tag is Toucher");
            VRButtonPressed.Invoke();
        }
    }
}
