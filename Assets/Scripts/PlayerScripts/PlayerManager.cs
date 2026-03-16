using UnityEngine;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private SimpleFpsController controller;
    [SerializeField] private PlayerHUD hud;

    // TODO hookup to day/night cycle
    bool isNight = false;

    [Header("Hunger")]
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerDrainRate = 0.1f;

    [Header("Thirst")]
    public float maxThirst = 100f;
    public float thirst = 100f;
    public float thirstDrainRate = 0.1f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float healingRate = 0f;
    public float healthDrainRatePoison = 1f;
    public float healthDrainRateTemp = 0.01f;
    public float healthDrainRateHunger = 0.2f;
    public float healthDrainRateThirst = 0.2f;
    public float emptyGracePeriod = 5f; // Time in seconds before health starts draining from hunger/thirst being empty
    private float emptyTimer;
    private bool isHealing;
    private bool isPoisoned;

    [Header("Temperature")]
    public float maxTemp = 100f;
    public float temp = 100f;

    public float coldThreshold = 25f;
    public float tempMin = 0f;

    [SerializeField] private float tempDrainRate = 0.0f;  // how fast you lose temp in neutral conditions
    [SerializeField] private float nightColdMultiplier = 1.5f;
    // Can add more multipliers for maybe weather or biome

    // 0 = no protection, 1 = full protection
    [Range(0f, 1f)]
    public float coldResistance = 0.0f;

    public bool isSprinting = false; // TO:DO connect this to the actual sprinting control
    public float sprintMult = 2f;

    private float sprintThreshold = 0.25f; // Below this hunger/thirst level, sprinting is not allowed
    public bool CanSprint =>
       hunger > maxHunger * sprintThreshold
       && thirst > maxThirst * sprintThreshold;

    // For status effects
    public bool isHealing = false;
    public bool isPoisoned = false;
    public float healTimer = 0f;
    public float poisonTimer = 0f;

    void Awake( )
    {
        if ( controller == null ) controller = GetComponent<SimpleFpsController>();
        if ( hud == null ) hud = FindFirstObjectByType<PlayerHUD>();
    }

    void ColdDamageCheck( )
    {
        if ( temp > coldThreshold ) return;

        float dt = Time.deltaTime;

        float severity = Mathf.InverseLerp(coldThreshold, tempMin, temp);
        // severity = 0 at 25, 1 at 0

        float damagePerSecond = healthDrainRateTemp * (1f + severity);
        health -= damagePerSecond * dt;
        health = Mathf.Max(health, 0f);
    }

    // Heat sources are very simple rn, just adds heat per second.
    public void AddHeat( float heatPerSecond )
    {
        temp += heatPerSecond * Time.deltaTime;
        temp = Mathf.Clamp(temp, tempMin, maxTemp);
    }

    void ThirstDrain( float sprintMult )
    {
        if ( thirst <= 0f ) return;
        thirst -= thirstDrainRate * sprintMult * Time.deltaTime;
        thirst = Mathf.Max(thirst, 0f);
    }

    void HungerDrain( float sprintMult )
    {
        if ( hunger <= 0f ) return;
        hunger -= hungerDrainRate * sprintMult * Time.deltaTime;
        hunger = Mathf.Max(hunger, 0f);
    }

    void HealthDrain( )
    {
        if ( health <= 0f ) return;
        bool starving = hunger <= 0f;
        bool dehydrated = thirst <= 0f;

        if ( starving || dehydrated )
        {
            emptyTimer += Time.deltaTime;
        }
        else {
            emptyTimer = 0f;
        }

        if ( (emptyTimer < emptyGracePeriod) && !isPoisoned )
            return;

        if ( !starving && !dehydrated && !isPoisoned ) return;

        float damage = 0f;
        if ( emptyTimer >= emptyGracePeriod )
        {
            if ( starving ) damage += healthDrainRateHunger;
            if ( dehydrated ) damage += healthDrainRateThirst;
        }
        if ( isPoisoned ) damage += healthDrainRatePoison;

        health -= damage * Time.deltaTime;
        health = Mathf.Max(health, 0f);
    }

    void HealthRestore( )
    {
        if ( hunger >= maxHunger * 0.75)
        {
            health += healingRate * Time.deltaTime;
        }
    }


    void TempDrain( )
    {
        float dt = Time.deltaTime;

        float nightMult = isNight ? nightColdMultiplier : 1f;

        // Clothes reduce how fast you get cold.
        float resistanceMult = 1f - coldResistance; // 0 resistance => 1.0 drain, 1 resistance => 0 drain

        float drainPerSecond = tempDrainRate * nightMult * resistanceMult;

        temp -= drainPerSecond * dt;
        temp = Mathf.Clamp(temp, tempMin, maxTemp);
    }

    void Update( )
    {
        isSprinting = controller != null && controller.IsSprinting;
        float drainMult = isSprinting ? sprintMult : 1f;

        TempDrain();
        ColdDamageCheck();
        ThirstDrain(drainMult);
        HungerDrain(drainMult);

        HealthDrain();
        HealthRestore();

        // Debug.Log("Hunger: " + hunger);
        if ( hud != null )
        {
            hud.SetHunger(hunger, maxHunger);
            hud.SetThirst(thirst, maxThirst);
            hud.SetHealth(health, maxHealth);
            hud.SetTemperature(temp, maxTemp);
        }

        if ( controller != null )
            controller.SetSprintAllowed(CanSprint);
    }

    // Returns true if successfully drank (useful for consumables)
    public bool TryDrink( float waterValue )
    {
        // Check if we're currently at max thirst
        if ( thirst == maxThirst )
        {
            return false;
        }

        float tempThirst = thirst + waterValue;

        // Prevent exceeding max thirst
        if ( tempThirst >= maxThirst )
        {
            thirst = maxThirst;
        }
        else
        {
            thirst = tempThirst;
        }

        hud.SetThirst(thirst, maxThirst);
        return true;
    }

    // Returns true if successfully ate (useful for consumables)
    public bool TryEat( float foodValue )
    {
        // Check if we're currently at max hunger
        if ( hunger == maxHunger )
        {
            return false;
        }

        float tempHunger = hunger + foodValue;

        // Prevent exceeding max hunger
        if ( tempHunger >= maxHunger )
        {
            hunger = maxHunger;
        }
        else
        {
            hunger += tempHunger;
        }

        hud.SetHunger(hunger, maxHunger);
        return true;
    }

    public void TryApplyStatus( string status )
    {
        if ( string.Equals(status, "Healing") )
        {
            // Refresh healing duration if already healing
            if ( isHealing )
            {
                Debug.Log("Refreshed Healing");
                healTimer = 0f;
            }
            else
            {
                Debug.Log("Healing");
                isHealing = true;
                
                StartCoroutine(HealingBuffTimer(10));
            }
        }

        if ( string.Equals(status, "Poison") )
        {    
            // Refresh poison duration if already poisoned
            if ( isPoisoned )
            {
                Debug.Log("Refreshed Poison");
                poisonTimer = 0f;
            }
            else
            {
                Debug.Log("Poisoned");
                isPoisoned = true;
                hud.SetActivePoisonIcon();
                StartCoroutine(PoisonDebuffTimer(8));
            }
        }
    }

    IEnumerator HealingBuffTimer( float seconds )
    {
        while ( isHealing && (healTimer <= seconds) )
        {
            healTimer += 1;

            yield return new WaitForSeconds(1);
        }

        isHealing = false;
        healTimer = 0f;
        
        Debug.Log("No Longer Healing");
    }

    IEnumerator PoisonDebuffTimer( float seconds )
    {
        while ( isPoisoned && (poisonTimer <= seconds) )
        {
            poisonTimer += 1;

            yield return new WaitForSeconds(1);
        }

        isPoisoned = false;
        poisonTimer = 0f;
        hud.SetInactivePoisonIcon();
        Debug.Log("No Longer Poisoned");
    }
}
