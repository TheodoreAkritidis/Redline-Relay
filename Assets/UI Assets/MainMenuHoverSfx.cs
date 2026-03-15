using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuHoverSfx : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private string buttonClass = "menu-button";

    private readonly HashSet<VisualElement> hovered = new();
    private readonly List<VisualElement> targets = new();

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        var root = uiDocument.rootVisualElement;

        targets.Clear();
        targets.AddRange(root.Query<VisualElement>(className: buttonClass).ToList());

        foreach (var t in targets)
        {
            t.RegisterCallback<PointerEnterEvent>(OnEnter);
            t.RegisterCallback<PointerLeaveEvent>(OnLeave);
        }
    }

    private void OnDisable()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;
            t.UnregisterCallback<PointerEnterEvent>(OnEnter);
            t.UnregisterCallback<PointerLeaveEvent>(OnLeave);
        }

        targets.Clear();
        hovered.Clear();
    }

    private void OnEnter(PointerEnterEvent evt)
    {
        if (hoverClip == null || audioSource == null) return;
        var ve = evt.currentTarget as VisualElement;
        if (ve == null) return;

        // play once per hover session
        if (hoverClip != null && audioSource != null && hovered.Add(ve))
            audioSource.PlayOneShot(hoverClip);
    }

    private void OnLeave(PointerLeaveEvent evt)
    {
        var ve = evt.currentTarget as VisualElement;
        if (ve == null) return;

        hovered.Remove(ve);
    }
}