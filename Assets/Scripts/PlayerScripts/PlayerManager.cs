using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public float internalTemp = 98f;
    public float targetTemp = 98f;

    public float envTemp = 70f; // placeholder, would be what the world temperature is atm

    public float envPull = 0.08f; // How strongly is the players internal temp pulled towards the env temp (higher = faster)

    public float bodyPull = 0.02f; // How strongly does the body pull back toward target temp (higher = faster)

    [Range(0f, 1f)]
    public float tempResistance = 0.0f; // 0 = no resistance, 1 = complete resistance
    // This is a placeholder as of now but will be good for like clothes and stuff later on

    void Update( )
    {
        float dt = Time.deltaTime;

        float actualEnvPull = envPull * (1f - tempResistance);

        float envTerm = actualEnvPull * (envTemp - internalTemp);
        float bodyTerm = bodyPull * (targetTemp - internalTemp);

        internalTemp += (envTerm + bodyTerm) * dt;
    }
}
