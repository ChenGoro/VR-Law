using TMPro;
using UnityEngine;
public class BailOption : MonoBehaviour
{
    public TextMeshPro text;
    public TextMeshPro title;
    public VR_Button button;
    public BailOptionType bailOptionType;


    public void Show()
    {
        text.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        button.gameObject.SetActive(true);

    }

    public void Hide()
    {
        text.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
    }
}

public enum BailOptionType
{
    ROR,
    ROB,
    Jail
}
