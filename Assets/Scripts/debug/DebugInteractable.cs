using UnityEngine;

public sealed class DebugInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Interact";

    public string GetPrompt() => prompt;


    public void Interact(GameObject interactor)
    {
        Debug.Log($"Interacted with: {name} by {interactor.name}");
    }
}
