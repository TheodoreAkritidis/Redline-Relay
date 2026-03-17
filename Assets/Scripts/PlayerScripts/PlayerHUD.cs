using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Statuses")]
    [SerializeField] private StatusManager status;
    [SerializeField] private GameObject poisonIcon;
    [SerializeField] private GameObject healingIcon;

    private void Awake( )
    {
        poisonIcon.SetActive(false);
        healingIcon.SetActive(false);

        if ( status == null )
        {
            status = FindFirstObjectByType<StatusManager>();
        }
    }

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

    public void SetActivePoisonIcon( )
    {
        status.SetNewStatusIcon(poisonIcon);
    }

    public void SetInactivePoisonIcon( )
    {
        status.RemoveStatusIcon(poisonIcon);

    }

    public void SetActiveHealingIcon( )
    {
        status.SetNewStatusIcon(healingIcon);
    }

    public void SetInactiveHealingIcon( )
    {
        status.RemoveStatusIcon(healingIcon);
    }
}