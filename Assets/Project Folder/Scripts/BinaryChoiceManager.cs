using UnityEngine;
using UnityEngine.Events;

public class BinaryChoiceManager : MonoBehaviour
/***
 * in charge of the 2 choice buttons game object
 */
{

    public VR_Button YesButton;
    public VR_Button NoButton;
    private bool Choice;
    public UnityEvent<bool> ChoiceMade;


    private void YesWasPressed()
    {
        Choice = true;
        ChoiceMade.Invoke(Choice);
    }


    private void NoWasPressed()
    {
        Choice = false;
        ChoiceMade.Invoke(Choice);
    }
    private void Start()
    {
        YesButton.VRButtonPressed.AddListener(YesWasPressed);
        NoButton.VRButtonPressed.AddListener(NoWasPressed);
    }

    public void Show()
    {
        YesButton.gameObject.SetActive(true);
        NoButton.gameObject.SetActive(true);
    }


    public void hide()
    {
        YesButton.gameObject.SetActive(false);
        NoButton.gameObject.SetActive(false);
    }

}
