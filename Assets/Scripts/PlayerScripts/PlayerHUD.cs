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
    [SerializeField] private GameObject poisonIcon;
    [SerializeField] private GameObject healingIcon;

    private void Awake( )
    {
        poisonIcon.SetActive(false);
        healingIcon.SetActive(false);
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

    public void SetActivePoisonIcon()
    {
        RectTransform poisonRT = poisonIcon.GetComponent<RectTransform>();
        RectTransform healRT = healingIcon.GetComponent<RectTransform>();

        if (healingIcon.activeSelf)
        {
            poisonRT.anchoredPosition = new Vector2(70, poisonRT.anchoredPosition.y);
        }
        else
        {
            poisonRT.anchoredPosition = new Vector2(0, poisonRT.anchoredPosition.y);
        }

        poisonIcon.SetActive(true);
    }

    public void SetInactivePoisonIcon()
    {
        RectTransform healRT = healingIcon.GetComponent<RectTransform>();

        if (healingIcon.activeSelf)
        {
            healRT.anchoredPosition = new Vector2(0, healRT.anchoredPosition.y);
        }

        poisonIcon.SetActive(false);
    }

    public void SetActiveHealingIcon()
    {
        RectTransform healRT = healingIcon.GetComponent<RectTransform>();
        RectTransform poisonRT = poisonIcon.GetComponent<RectTransform>();

        if (poisonIcon.activeSelf)
        {
            healRT.anchoredPosition = new Vector2(70, healRT.anchoredPosition.y);
        }
        else
        {
            healRT.anchoredPosition = new Vector2(0, healRT.anchoredPosition.y);
        }

        healingIcon.SetActive(true);
    }

    public void SetInactiveHealingIcon()
    {
        RectTransform poisonRT = poisonIcon.GetComponent<RectTransform>();

        if (poisonIcon.activeSelf)
        {
            poisonRT.anchoredPosition = new Vector2(0, poisonRT.anchoredPosition.y);
        }

        healingIcon.SetActive(false);
    }
}