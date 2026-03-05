using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeatSource : MonoBehaviour
{
    [SerializeField] private float heatStrength = 1.5f; // This is basically how much per second the players temp will increase

    private void Reset( )
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay( Collider other )
    {
        if ( other.CompareTag("Player") )
        {
            PlayerManager player = other.GetComponent<PlayerManager>();

            if ( player != null )
            {
                player.AddHeat(heatStrength);
            }
        }
    }
}