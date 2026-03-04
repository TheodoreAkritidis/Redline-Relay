using UnityEngine;

public sealed class WaterSource : MonoBehaviour, IInteractable
{
    [SerializeField] private float WaterValue;
    
    public string GetPrompt()
    {
        return "Drink Water";
    }

    public void Interact(GameObject interactor)
    {
        if (interactor != null)
        {
            var player = interactor.GetComponent<PlayerManager>();
            if (player == null)
            {
                return;
            }

            player.TryDrink(WaterValue);
        }
    }
}