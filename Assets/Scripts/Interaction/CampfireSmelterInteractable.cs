using UnityEngine;

public sealed class CampfireSmelterInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private SimpleFpsController playerController;

    [Header("Prompt")]
    [SerializeField] private string promptText = "Open Smelter";

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<SimpleFpsController>();
    }

    public string GetPrompt()
    {
        return promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (playerController == null)
        {
            Debug.LogWarning("CampfireSmelterInteractable: No SimpleFpsController found in scene.");
            return;
        }

        playerController.OpenSmelterMenu();
    }
}
