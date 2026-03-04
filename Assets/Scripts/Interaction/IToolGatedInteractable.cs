using UnityEngine;

public interface IToolGatedInteractable
{
    /// <summary>
    /// Return true if the interactor is allowed to interact right now.
    /// If false, provide a short prompt explaining what tool is required.
    /// </summary>
    bool CanInteractWith(GameObject interactor, out string blockedPrompt);
}