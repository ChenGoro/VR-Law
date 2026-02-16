using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

public class AmountChooser : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshPro Tiltle;
    private float amount = -1;
    public float Amount { get { return amount; } }

    public event Action sliderWasTouched;



    private void Awake()
    {
        slider.sliderWasTouched += () => sliderWasTouched?.Invoke();
    }

    public async UniTask<float> ShowAndWaitForAmount()
    {
        amount = -1; // reset previous value
        slider.Reset();
        Show();
        amount = await slider.WaitForConfirm();
        Hide();
        return amount;
    }

    public async UniTask CancelWait()
    {
        await slider.CancelWait();
        amount = -1; // reset previous value
    }



    public void Show()
    {
        gameObject.SetActive(true);
        slider.gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        slider.gameObject.SetActive(false);
    }

    internal void SetDefendantNameAndIncomeOnBail(string defendantFirstName, string defendantLastName, float annualIncome)
    {
        string formattedIncome = $"${annualIncome:N0}.";

        string firstLine = "Doe's Annual income is";
        string secondLine = "\\r\\nDoe must pay:";

        string newText = firstLine.Replace("Doe", defendantLastName) + " " + formattedIncome + secondLine.Replace("Doe", defendantLastName);
        Tiltle.text = newText;

    }

    internal void SetDefendantNameAndIncomeOnFine(string defendantFirstName, string defendantLastName, float annualIncome)
    {
        string formattedIncome = $"${annualIncome:N0}.";
        string firstLine = "Doe's Annual income is";
        string secondLine = "\\r\\nThe Fine amount should be:";
        string newText = firstLine.Replace("Doe", defendantLastName) + " " + formattedIncome + secondLine;
        Tiltle.text = newText;
    }

    internal void SetDefendantName(string firstName, string LastName)
    {
        string newText = Tiltle.text.Replace("Doe", LastName);
        Tiltle.text = newText;
    }
}
