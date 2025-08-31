using TMPro;
using UnityEngine;

public class SentenceOption : MonoBehaviour
{
    public TextMeshPro text;
    public TextMeshPro title;
    public VR_Button button;
    public SentenceOptionType sentenceOptionType;
    public GameObject Backface;

    public void Show()
    {
        text.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        button.gameObject.SetActive(true);
        Backface.SetActive(true);

    }

    public void Hide()
    {
        text.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
        Backface.SetActive(false);
    }

    internal void SetDefendantName(string firstName, string LastName)
    {
        string newText = text.text.Replace("Doe", LastName);
        text.text = newText;
    }
}

public enum SentenceOptionType

{
    Sentence,
    Fine
}