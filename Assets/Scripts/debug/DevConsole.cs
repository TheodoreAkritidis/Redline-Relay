using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DevConsole : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryComponent playerInventory;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Toggle")]
    [SerializeField] private Key toggleKey = Key.Backquote;

    private const int MaxLogLines = 2;
    public bool IsOpen => open;
    private bool open;
    private string inputLine = "";
    private readonly List<string> logLines = new();

    private void Awake()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventoryComponent>();
        if (itemDatabase == null) itemDatabase = FindFirstObjectByType<ItemDatabase>();

        Log("DevConsole ready. Type 'help'.");
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle (works even without a PlayerInput action)
        if (kb[toggleKey].wasPressedThisFrame)
            SetOpen(!open);

        if (!open) return;

        // Submit on Enter (reliable)
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            RunCommand(inputLine);
            inputLine = "";
            // keep focus on the text field so typing feels normal
            GUI.FocusControl("DevConsoleInput");
        }

        // Close on Escape
        if (kb.escapeKey.wasPressedThisFrame)
            SetOpen(false);
    }

    private void SetOpen(bool value)
    {
        open = value;

        if (open)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            GUI.FocusControl("DevConsoleInput");
        }
    }



    private void OnGUI()
    {
        if (!open) return;

        float w = 560;
        float h = 280;
        Rect rect = new Rect(10, Screen.height - h - 10, w, h);

        GUI.Box(rect, "Dev Console (` to toggle)");

        GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 25, rect.width - 20, rect.height - 35));

        int maxLines = 10;
        int start = Mathf.Max(0, logLines.Count - maxLines);
        for (int i = start; i < logLines.Count; i++)
            GUILayout.Label(logLines[i]);

        GUILayout.Space(6);

        GUI.SetNextControlName("DevConsoleInput");
        inputLine = GUILayout.TextField(inputLine);

        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            RunCommand(inputLine);
            inputLine = "";
            GUI.FocusControl("DevConsoleInput");
            e.Use();
        }

        GUILayout.EndArea();
    }

    private void RunCommand(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        Log($"> {line}");

        string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0].ToLowerInvariant();

        if (cmd == "help")
        {
            Log("give <itemId> <amount> | list | clear");
            return;
        }

        if (cmd == "clear")
        {
            logLines.Clear();
            Log("Cleared.");
            return;
        }

        if (cmd == "list")
        {
            if (itemDatabase == null) { Log("No ItemDatabase found."); return; }
            Log("Items: " + string.Join(", ", itemDatabase.GetAllIds()));
            return;
        }

        if (cmd == "give")
        {
            if (playerInventory == null) { Log("No PlayerInventoryComponent found."); return; }
            if (itemDatabase == null) { Log("No ItemDatabase found."); return; }

            if (parts.Length < 2)
            {
                Log("Usage: give <itemId> <amount>");
                return;
            }

            string id = parts[1];
            int amount = 1;
            if (parts.Length >= 3 && !int.TryParse(parts[2], out amount)) amount = 1;
            amount = Mathf.Max(1, amount);

            if (!itemDatabase.TryGet(id, out var def))
            {
                Log($"Unknown item id '{id}'. Try: list");
                return;
            }

            
            int remainder = InventoryRules.TryAutoAdd(def, amount, playerInventory.Model.Hotbar, playerInventory.Model.Backpack);

            playerInventory.NotifyInventoryChanged();

            if (remainder == 0) Log($"Gave {def.ItemId} x{amount}.");
            else Log($"Gave {def.ItemId} x{amount - remainder}. Inventory full; remainder {remainder}.");

            return;
        }

        Log($"Unknown command '{cmd}'. Type 'help'.");
    }

    private void Log(string msg)
    {
        logLines.Add(msg);

        while (logLines.Count > MaxLogLines)
            logLines.RemoveAt(0);
    }
}
