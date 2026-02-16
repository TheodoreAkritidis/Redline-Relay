using UnityEngine;

public interface IInteractable
{
    // return something like "Pick up", "Open", "Use"
    string GetPrompt();
    void Interact(GameObject interactor);
}
