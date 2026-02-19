using UnityEngine;

public class PlayerManager : MonoBehaviour
{
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

    void TemperatureDamageCheck()
    {
        if(internalTemp < tooCold || internalTemp > tooHot)
        {
            health -= healthDrainRateTemp * Time.deltaTime;
            health = Mathf.Max(health, 0f);
        }
    }

    void ThirstDrain()
    {
        if (isSprinting)
        {
            thirst -= thirstDrainRate * 2f * Time.deltaTime;

        }
        else
        {
            thirst -= thirstDrainRate * Time.deltaTime;
        }
    }

    void HungerDrain()
    {
        if (isSprinting)
        {
            hunger -= hungerDrainRate * 2f * Time.deltaTime;

        }
        else
        {
            hunger -= hungerDrainRate * Time.deltaTime;
        }
    }

    void Update( )
    {
        float dt = Time.deltaTime;

        float actualEnvPull = envPull * (1f - tempResistance);

        float envTerm = actualEnvPull * (envTemp - internalTemp);
        float bodyTerm = bodyPull * (targetTemp - internalTemp);

        internalTemp += (envTerm + bodyTerm) * dt;

        TemperatureDamageCheck( );
        ThirstDrain( );
        HungerDrain( );
    }
}
