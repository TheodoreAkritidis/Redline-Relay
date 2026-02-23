using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private SimpleFpsController controller;
    [SerializeField] private PlayerHUD hud;

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
    public float healthDrainRateTemp = 0.01f;
    public float healthDrainRateHunger = 0.2f;
    public float healthDrainRateThirst = 0.2f;
    public float emptyGracePeriod = 5f; // Time in seconds before health starts draining from hunger/thirst being empty
    private float emptyTimer;

    [Header("Temperature")]
    public float internalTemp = 98f;
    public float targetTemp = 98f;
    public float envTemp = 70f; // placeholder, would be what the world temperature is atm

    public float envPull = 0.08f; // How strongly is the players internal temp pulled towards the env temp (higher = faster)
    public float bodyPull = 0.02f; // How strongly does the body pull back toward target temp (higher = faster)

    public float tooHot = 108f;
    public float tooCold = 88f;

    [Range(0f, 1f)]
    public float tempResistance = 0.0f; // 0 = no resistance, 1 = complete resistance
    // This is a placeholder as of now but will be good for like clothes and stuff later on

    public bool isSprinting = false; // TO:DO connect this to the actual sprinting control
    public float sprintMult = 2f;

    private float sprintThreshold = 0.25f; // Below this hunger/thirst level, sprinting is not allowed
    public bool CanSprint =>
       hunger > maxHunger * sprintThreshold
       && thirst > maxThirst * sprintThreshold;

    void Awake( )
    {
        if ( controller == null ) controller = GetComponent<SimpleFpsController>();
        if ( hud == null ) hud = FindFirstObjectByType<PlayerHUD>();
    }

    void TemperatureDamageCheck( )
    {
        if ( internalTemp < tooCold || internalTemp > tooHot )
        {
            health -= healthDrainRateTemp * Time.deltaTime;
            health = Mathf.Max(health, 0f);
        }
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
        else
        {
            emptyTimer = 0f;
            return;
        }

        if ( emptyTimer < emptyGracePeriod )
            return;

        if ( !starving && !dehydrated ) return;

        float damage = 0f;
        if ( starving ) damage += healthDrainRateHunger;
        if ( dehydrated ) damage += healthDrainRateThirst;

        health -= damage * Time.deltaTime;
        health = Mathf.Max(health, 0f);
    }


    void TempChange( )
    {
        float dt = Time.deltaTime;

        float actualEnvPull = envPull * (1f - tempResistance);

        float envTerm = actualEnvPull * (envTemp - internalTemp);
        float bodyTerm = bodyPull * (targetTemp - internalTemp);

        internalTemp += (envTerm + bodyTerm) * dt;
    }

    void Update( )
    {
        isSprinting = controller != null && controller.IsSprinting;
        float drainMult = isSprinting ? sprintMult : 1f;

        TempChange();
        TemperatureDamageCheck();
        ThirstDrain(drainMult);
        HungerDrain(drainMult);

        HealthDrain();


        Debug.Log("Hunger: " + hunger);
        if ( hud != null )
        {
            hud.SetHunger(hunger, maxHunger, sprintThreshold);
            hud.SetThirst(thirst, maxThirst, sprintThreshold);
            hud.SetHealth(health, maxHealth);
        }

        if ( controller != null )
            controller.SetSprintAllowed(CanSprint);
    }
}
