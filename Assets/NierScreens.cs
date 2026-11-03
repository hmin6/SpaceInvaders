using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Builds the NieR-styled start screen, end screen and hit flash at runtime as children of this
// Canvas, and shows them based on GameManager's state. Text and timings are Inspector fields;
// colors and layout helpers come from NierStyle.
[RequireComponent(typeof(Canvas))]
public class NierScreens : MonoBehaviour
{
    [Tooltip("Optional. Uses the TextMesh Pro default font if empty.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Start Screen")]
    [SerializeField] private string titleText = "Space Invaders";
    [SerializeField] private string subtitleText = "YoRHa Unit: Combat Simulation";
    [SerializeField] private string startLabel = "[ Start ]";
    [SerializeField] private string startHint = "Press Space or Enter";
    [SerializeField] private string controlsText = "Left / Right Arrows : Move        Space : Fire";

    [Header("End Screen")]
    [SerializeField] private string winText = "You Win";
    [SerializeField] private string loseText = "Game Over";
    [SerializeField] private string retryLabel = "[ Retry ]";
    [SerializeField] private string retryHint = "Press R or Enter";

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = .25f;
    [Range(0, 1)]
    [SerializeField] private float flashAlpha = .35f;

    private GameObject startScreen;
    private GameObject endScreen;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI killsText;
    private TextMeshProUGUI livesText;
    private Image resultBar;
    private Image startMarker;
    private Image retryMarker;
    private Image flash;
    private float flashTimer;
    private GameManager gameManager;

    void Awake()
    {
        // Added last, so it draws over the HUD
        RectTransform root = NierStyle.CreateRect("NierScreens", transform);
        NierStyle.Stretch(root);

        flash = NierStyle.CreateImage("HitFlash", root, NierStyle.WithAlpha(NierStyle.Accent, 0));
        NierStyle.Stretch(flash.rectTransform);
        flash.enabled = false;

        BuildStartScreen(root);
        BuildEndScreen(root);
        startScreen.SetActive(false);
        endScreen.SetActive(false);
    }

    // Start (not Awake) so GameManager.Awake has already set Instance and State
    void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("NierScreens: no GameManager in the scene.", this);
            return;
        }
        gameManager.StateChanged += OnStateChanged;
        gameManager.LivesChanged += OnLivesChanged;
        OnStateChanged(gameManager.State);
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= OnStateChanged;
            gameManager.LivesChanged -= OnLivesChanged;
        }
    }

    // Unscaled time, because the game is frozen (timeScale 0) on the start and end screens
    void Update()
    {
        bool markerOn = Time.unscaledTime % 1f < .6f;
        startMarker.enabled = markerOn;
        retryMarker.enabled = markerOn;

        if (flashTimer > 0)
        {
            flashTimer -= Time.unscaledDeltaTime;
            flash.color = NierStyle.WithAlpha(NierStyle.Accent, flashAlpha * Mathf.Clamp01(flashTimer / flashDuration));
            flash.enabled = flashTimer > 0;
        }
    }

    void OnStateChanged(GameManager.GameState state)
    {
        startScreen.SetActive(state == GameManager.GameState.Title);

        bool ended = state == GameManager.GameState.Won || state == GameManager.GameState.Lost;
        endScreen.SetActive(ended);
        if (ended)
        {
            bool won = state == GameManager.GameState.Won;
            resultText.text = won ? winText : loseText;
            resultBar.color = won ? NierStyle.Ink : NierStyle.Accent;
            killsText.text = gameManager.AliensDestroyed + " / " + gameManager.TotalAliens;
            livesText.text = gameManager.Lives.ToString();
        }
    }

    // Lives only change when the player is hit
    void OnLivesChanged(int lives)
    {
        flashTimer = flashDuration;
        flash.enabled = true;
    }

    void BuildStartScreen(Transform root)
    {
        Vector2 center = new Vector2(.5f, .5f);

        Image screen = NierStyle.CreateImage("StartScreen", root, NierStyle.WithAlpha(NierStyle.Background, .9f));
        NierStyle.Stretch(screen.rectTransform);
        startScreen = screen.gameObject;

        RectTransform frame = NierStyle.CreateRect("Frame", screen.transform);
        NierStyle.Stretch(frame, 60);
        NierStyle.CreateFrame(frame, NierStyle.Ink, 2);

        TextMeshProUGUI header = NierStyle.CreateText("Header", frame, "YoRHa // Combat Simulation System", 22,
            NierStyle.Ink, TextAlignmentOptions.TopLeft, font);
        NierStyle.Place(header.rectTransform, new Vector2(0, 1), new Vector2(32, -26), new Vector2(1000, 40));

        // Title block between two thin lines, with small squares on the line ends
        CreateLine(screen.transform, new Vector2(0, 175), 1100);
        CreateLine(screen.transform, new Vector2(0, -60), 1100);

        TextMeshProUGUI title = NierStyle.CreateText("Title", screen.transform, titleText, 110, NierStyle.InkDark,
            TextAlignmentOptions.Center, font, 24);
        NierStyle.Place(title.rectTransform, center, new Vector2(0, 85), new Vector2(1700, 140));

        TextMeshProUGUI subtitle = NierStyle.CreateText("Subtitle", screen.transform, subtitleText, 34, NierStyle.Ink,
            TextAlignmentOptions.Center, font, 10, false);
        NierStyle.Place(subtitle.rectTransform, center, new Vector2(0, -8), new Vector2(1400, 50));

        startMarker = NierStyle.CreateMenuItem(screen.transform, startLabel, new Vector2(0, -175), font);

        TextMeshProUGUI hint = NierStyle.CreateText("Hint", screen.transform, startHint, 24, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(hint.rectTransform, center, new Vector2(0, -245), new Vector2(1000, 40));

        TextMeshProUGUI controls = NierStyle.CreateText("Controls", frame, controlsText, 24, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(controls.rectTransform, new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(1600, 40));
    }

    void BuildEndScreen(Transform root)
    {
        Vector2 center = new Vector2(.5f, .5f);

        Image dim = NierStyle.CreateImage("EndScreen", root, NierStyle.WithAlpha(NierStyle.Background, .6f));
        NierStyle.Stretch(dim.rectTransform);
        endScreen = dim.gameObject;

        Image panel = NierStyle.CreateImage("Panel", dim.transform, NierStyle.Background);
        NierStyle.Place(panel.rectTransform, center, Vector2.zero, new Vector2(960, 600));
        NierStyle.CreateFrame(panel.transform, NierStyle.InkDark, 2);

        // Dark header bar along the top of the panel
        Image headerBar = NierStyle.CreateImage("Header", panel.transform, NierStyle.InkDark);
        RectTransform headerRect = headerBar.rectTransform;
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(.5f, 1);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0, 64);

        TextMeshProUGUI headerText = NierStyle.CreateText("Label", headerBar.transform, "Result", 28,
            NierStyle.Background, TextAlignmentOptions.Left, font);
        NierStyle.Stretch(headerText.rectTransform);
        headerText.rectTransform.offsetMin = new Vector2(30, 0);

        Image headerSquare = NierStyle.CreateImage("Square", headerBar.transform, NierStyle.Background);
        NierStyle.Place(headerSquare.rectTransform, new Vector2(1, .5f), new Vector2(-30, 0), new Vector2(14, 14));

        resultText = NierStyle.CreateText("Result", panel.transform, "", 76, NierStyle.InkDark,
            TextAlignmentOptions.Center, font, 20);
        NierStyle.Place(resultText.rectTransform, center, new Vector2(0, 150), new Vector2(900, 100));

        resultBar = NierStyle.CreateImage("ResultBar", panel.transform, NierStyle.Accent);
        NierStyle.Place(resultBar.rectTransform, center, new Vector2(0, 88), new Vector2(420, 4));

        CreateLine(panel.transform, new Vector2(0, 40), 820);
        killsText = CreateStatRow(panel.transform, "Aliens Destroyed", -5);
        livesText = CreateStatRow(panel.transform, "Lives Remaining", -60);
        CreateLine(panel.transform, new Vector2(0, -105), 820);

        retryMarker = NierStyle.CreateMenuItem(panel.transform, retryLabel, new Vector2(0, -180), font);

        TextMeshProUGUI hint = NierStyle.CreateText("Hint", panel.transform, retryHint, 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(hint.rectTransform, center, new Vector2(0, -250), new Vector2(800, 36));
    }

    // Label on the left, value on the right. Returns the value text.
    TextMeshProUGUI CreateStatRow(Transform parent, string label, float y)
    {
        Vector2 center = new Vector2(.5f, .5f);

        TextMeshProUGUI labelText = NierStyle.CreateText("StatLabel", parent, label, 30, NierStyle.Ink,
            TextAlignmentOptions.Left, font);
        NierStyle.Place(labelText.rectTransform, center, new Vector2(-200, y), new Vector2(400, 50));

        TextMeshProUGUI valueText = NierStyle.CreateText("StatValue", parent, "", 30, NierStyle.InkDark,
            TextAlignmentOptions.Right, font);
        NierStyle.Place(valueText.rectTransform, center, new Vector2(200, y), new Vector2(400, 50));
        return valueText;
    }

    // Thin horizontal line with a small square at each end
    void CreateLine(Transform parent, Vector2 position, float width)
    {
        Vector2 center = new Vector2(.5f, .5f);

        Image line = NierStyle.CreateImage("Line", parent, NierStyle.Ink);
        NierStyle.Place(line.rectTransform, center, position, new Vector2(width, 2));

        Image left = NierStyle.CreateImage("LineEnd", parent, NierStyle.Ink);
        NierStyle.Place(left.rectTransform, center, position + new Vector2(-width / 2 - 12, 0), new Vector2(10, 10));
        Image right = NierStyle.CreateImage("LineEnd", parent, NierStyle.Ink);
        NierStyle.Place(right.rectTransform, center, position + new Vector2(width / 2 + 12, 0), new Vector2(10, 10));
    }
}
