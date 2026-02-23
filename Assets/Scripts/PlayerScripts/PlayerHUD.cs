using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Hunger")]
    [SerializeField] private Image hungerTopFill;
    [SerializeField] private Image hungerBottomFill;

    [Header("Thirst")]
    [SerializeField] private Image thirstTopFill;
    [SerializeField] private Image thirstBottomFill;

    // For hunger and thirst I was following the sketch Jacob did so there is a top and bottom half.
    // If we end up simpligfying the design the code will be very simmilar to how health is done.

    [Header("Health")]
    [SerializeField] private Image healthFill;

    public void SetHunger( float current, float max, float sprintThreshold )
    {
        float normalized = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        if ( normalized > sprintThreshold )
        {
            hungerBottomFill.fillAmount = 1f;
            hungerTopFill.fillAmount = (normalized - sprintThreshold) / (1f - sprintThreshold);
        }
        else
        {
            hungerBottomFill.fillAmount = 0f;
            hungerTopFill.fillAmount = (sprintThreshold <= 0f) ? 0f : (normalized / sprintThreshold);
        }
    }
    public void SetThirst( float current, float max, float sprintThreshold )
    {
        float normalized = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        if ( normalized > sprintThreshold )
        {
            thirstBottomFill.fillAmount = 1f;
            thirstTopFill.fillAmount = (normalized - sprintThreshold) / (1f - sprintThreshold);
        }
        else
        {
            thirstBottomFill.fillAmount = 0f;
            thirstTopFill.fillAmount = (sprintThreshold <= 0f) ? 0f : (normalized / sprintThreshold);
        }
    }


    public void SetHealth( float current, float max )
    {
        float n = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);

        healthFill.fillAmount = n;
    }
}