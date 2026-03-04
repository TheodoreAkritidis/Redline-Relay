using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Hunger")]
    [SerializeField] private Image hungerFill;

    [Header("Thirst")]
    [SerializeField] private Image thirstFill;

    // For hunger and thirst I was following the sketch Jacob did so there is a top and bottom half.
    // If we end up simpligfying the design the code will be very simmilar to how health is done.

    [Header("Health")]
    [SerializeField] private Image healthFill;

    [Header("Temperature")]
    [SerializeField] private Image tempFill;

    public void SetHunger( float current, float max )
    {
        float normalized = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        hungerFill.fillAmount = normalized;
    }

    public void SetThirst( float current, float max )
    {
        float normalized = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        thirstFill.fillAmount = normalized;
    }


    public void SetHealth( float current, float max )
    {
        float n = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        healthFill.fillAmount = n;
    }

    public void SetTemperature( float current, float max )
    {
        float n = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);


        tempFill.fillAmount = n;
    }
}