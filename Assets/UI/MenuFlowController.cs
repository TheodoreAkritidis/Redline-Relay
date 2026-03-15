using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public sealed class MenuFlowController : MonoBehaviour
{
    private enum MenuState
    {
        MainMenu,
        Playing,
        Paused,
        Victory
    }

    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private SimpleFpsController playerController;
    [SerializeField] private InventoryUITKView inventoryUI;
    [SerializeField] private Interactor interactor;

    [Header("Startup")]
    [SerializeField] private bool startAtMainMenu = true;

    [Header("Button Names")]
    [SerializeField] private string playButtonName = "PlayButton";
    [SerializeField] private string quitButtonName = "QuitButton";
    [SerializeField] private string resumeButtonName = "ResumeButton";
    [SerializeField] private string pauseQuitButtonName = "PauseQuitButton";
    [SerializeField] private string victoryRestartButtonName = "RestartButton";
    [SerializeField] private string victoryQuitButtonName = "VictoryQuitButton";

    [Header("Victory UI")]
    [SerializeField] private string victoryTimeLabelName = "contactTimeLabel";

    [Header("Audio")]
    [SerializeField] private BackgroundMusic backgroundMusic;

    private VisualElement root;
    private VisualElement mainMenuRoot;
    private VisualElement pauseMenuRoot;
    private VisualElement victoryMenuRoot;

    private Button playButton;
    private Button quitButton;
    private Button resumeButton;
    private Button pauseQuitButton;
    private Button victoryRestartButton;
    private Button victoryQuitButton;

    private Label victoryTimeLabel;


    private static bool forceStartPlayingOnNextLoad;
    private MenuState currentState;

    private float runTimerSeconds;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (playerController == null) playerController = FindFirstObjectByType<SimpleFpsController>();
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUITKView>();
        if (interactor == null) interactor = FindFirstObjectByType<Interactor>();
        if (backgroundMusic == null) backgroundMusic = FindFirstObjectByType<BackgroundMusic>();
    }

    private void OnEnable()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        mainMenuRoot = root.Q<VisualElement>("MainMenuRoot");
        pauseMenuRoot = root.Q<VisualElement>("PauseMenuRoot");
        victoryMenuRoot = root.Q<VisualElement>("ContactMenuRoot");

        playButton = mainMenuRoot != null ? mainMenuRoot.Q<Button>(playButtonName) : null;
        quitButton = mainMenuRoot != null ? mainMenuRoot.Q<Button>(quitButtonName) : null;

        resumeButton = pauseMenuRoot != null ? pauseMenuRoot.Q<Button>(resumeButtonName) : null;
        pauseQuitButton = pauseMenuRoot != null ? pauseMenuRoot.Q<Button>(pauseQuitButtonName) : null;

        victoryRestartButton = victoryMenuRoot != null ? victoryMenuRoot.Q<Button>(victoryRestartButtonName) : null;
        victoryQuitButton = victoryMenuRoot != null ? victoryMenuRoot.Q<Button>(victoryQuitButtonName) : null;
        victoryTimeLabel = victoryMenuRoot != null ? victoryMenuRoot.Q<Label>(victoryTimeLabelName) : null;

        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;
        if (resumeButton != null) resumeButton.clicked += OnResumeClicked;
        if (pauseQuitButton != null) pauseQuitButton.clicked += OnQuitToMainMenuClicked;
        if (victoryRestartButton != null) victoryRestartButton.clicked += OnRestartClicked;
        if (victoryQuitButton != null) victoryQuitButton.clicked += OnQuitToMainMenuClicked;

        bool shouldStartPlaying = forceStartPlayingOnNextLoad || !startAtMainMenu;
        forceStartPlayingOnNextLoad = false;

        SetState(shouldStartPlaying ? MenuState.Playing : MenuState.MainMenu);
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= OnPlayClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
        if (resumeButton != null) resumeButton.clicked -= OnResumeClicked;
        if (pauseQuitButton != null) pauseQuitButton.clicked -= OnQuitToMainMenuClicked;
        if (victoryRestartButton != null) victoryRestartButton.clicked -= OnRestartClicked;
        if (victoryQuitButton != null) victoryQuitButton.clicked -= OnQuitToMainMenuClicked;
    }

    private void Update()
    {
        if (currentState == MenuState.Playing)
        {
            runTimerSeconds += Time.unscaledDeltaTime;
        }

        if (currentState == MenuState.MainMenu || currentState == MenuState.Victory)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentState == MenuState.Playing)
                SetState(MenuState.Paused);
            else if (currentState == MenuState.Paused)
                SetState(MenuState.Playing);
        }
    }

    public void ShowVictory()
    {
        Debug.Log("Victory triggered.");
        UpdateVictoryTimeText();
        SetState(MenuState.Victory);
    }

    private void OnPlayClicked()
    {
        SetState(MenuState.Playing);
    }

    private void OnResumeClicked()
    {
        SetState(MenuState.Playing);
    }

    private void OnQuitToMainMenuClicked()
    {
        SetState(MenuState.MainMenu);
    }

    private void OnRestartClicked()
    {
        RestartCurrentScene();
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestartCurrentScene()
    {
        forceStartPlayingOnNextLoad = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        backgroundMusic?.StopAllMusic();

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void SetState(MenuState newState)
    {
        MenuState previousState = currentState;
        currentState = newState;

        bool showMain = newState == MenuState.MainMenu;
        bool showPause = newState == MenuState.Paused;
        bool showVictory = newState == MenuState.Victory;
        bool isPlaying = newState == MenuState.Playing;

        SetVisible(mainMenuRoot, showMain);
        SetVisible(pauseMenuRoot, showPause);
        SetVisible(victoryMenuRoot, showVictory);

        if (!isPlaying)
            CloseGameplayMenus();

        if (playerController != null)
            playerController.enabled = isPlaying;

        if (interactor != null)
            interactor.enabled = isPlaying;

        UnityEngine.Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !isPlaying;

        Time.timeScale = isPlaying ? 1f : 0f;

        if (newState == MenuState.MainMenu)
        {
            AudioListener.pause = false;
            backgroundMusic?.PlayMenuMusic();
        }
        else if (newState == MenuState.Playing)
        {
            AudioListener.pause = false;

            if (previousState == MenuState.Paused)
            {
                backgroundMusic?.ResumeGameplayMusic();
            }
            else
            {
                runTimerSeconds = 0f;
                backgroundMusic?.StartGameplayMusicFresh();
            }
        }
        else if (newState == MenuState.Paused)
        {
            backgroundMusic?.PauseGameplayMusic();
            AudioListener.pause = true;
        }
        else if (newState == MenuState.Victory)
        {
            AudioListener.pause = false;
            backgroundMusic?.PlayVictoryMusic();
            AudioListener.pause = true;
        }
    }

    private void UpdateVictoryTimeText()
    {
        if (victoryTimeLabel == null) return;
        victoryTimeLabel.text = $"Time till contact: {FormatTimer(runTimerSeconds)}";
    }

    private static string FormatTimer(float totalSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void CloseGameplayMenus()
    {
        if (inventoryUI == null) return;

        inventoryUI.SetBackpackOpen(false);
        inventoryUI.SetCraftingOpen(false);
        inventoryUI.SetSmelterOpen(false);
    }

    private static void SetVisible(VisualElement ve, bool visible)
    {
        if (ve == null) return;
        ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}