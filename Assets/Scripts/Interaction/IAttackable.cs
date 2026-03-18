using UnityEngine;

public interface IAttackable
{
    // Return a raw crosshair message such as "Left click or F to Attack" (no automatic "E to" prefix)
    string GetAttackPrompt();
}
