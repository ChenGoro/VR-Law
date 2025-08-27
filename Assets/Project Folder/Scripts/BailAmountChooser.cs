using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

public class BailAmountChooser : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshPro Tiltle;
    private float bailAmount = -1;
    public float BailAmount { get { return bailAmount; } }

    public event Action sliderWasTouched;
    private void Awake()
    {
        slider.sliderWasTouched += () => sliderWasTouched?.Invoke();
    }

    public async UniTask<float> ShowAndWaitForBailAmount()
    {
        bailAmount = -1; // reset previous value
        Show();
        bailAmount = await slider.WaitForConfirm();
        Hide();
        return bailAmount;
    }

    public async UniTask CancelWait()
    {
        await slider.CancelWait();
        bailAmount = -1; // reset previous value
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



}
