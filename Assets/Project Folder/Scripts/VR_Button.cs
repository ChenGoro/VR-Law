using UnityEngine;
using UnityEngine.Events;

public class VR_Button : MonoBehaviour
{
    public UnityEvent VRButtonPressed = new UnityEvent();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Toucher"))
        {
            Debug.Log("VRButton: inside OnTriggerEnter, Tag is Toucher");
            VRButtonPressed.Invoke();
        }
    }
}
