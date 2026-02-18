using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class CrosshairUITK : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    [Header("Text")]
    [SerializeField] private string defaultGlyph = ".";   // idle crosshair
    [SerializeField] private int crosshairFontSize = 28;
    [SerializeField] private int promptFontSize = 18;

    private VisualElement root;
    private Label centerLabel;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null) return;

        root.Clear();
        root.style.position = Position.Relative;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.pickingMode = PickingMode.Ignore;

        centerLabel = new Label(defaultGlyph);
        centerLabel.pickingMode = PickingMode.Ignore;

        // Center the *middle* of the label on screen.
        centerLabel.style.position = Position.Absolute;
        centerLabel.style.left = Length.Percent(50);
        centerLabel.style.top = Length.Percent(50);
        centerLabel.style.translate = new Translate(-50, -50, 0);

        centerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        centerLabel.style.whiteSpace = WhiteSpace.Normal;

        centerLabel.style.color = Color.white;
        centerLabel.style.fontSize = crosshairFontSize;

        // IMPORTANT: no background box
        centerLabel.style.backgroundColor = Color.clear;

        root.Add(centerLabel);

        SetDefault();
    }

    public void SetVisible(bool visible)
    {
        if (uiDocument != null)
            uiDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetDefault()
    {
        if (centerLabel == null) return;
        centerLabel.text = defaultGlyph;
        centerLabel.style.fontSize = crosshairFontSize;
    }

    // Example: "E to Pick up"
    public void SetPrompt(string action)
    {
        if (centerLabel == null) return;

        string a = string.IsNullOrWhiteSpace(action) ? "Interact" : action.Trim();
        centerLabel.text = $"E to {a}";
        centerLabel.style.fontSize = promptFontSize;
    }
}
