using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Builds every NieR-styled screen at runtime as children of this Canvas: title, briefing,
// debrief / failure / certification results, the epilogue, the HUD level label and the hit flash.
// Shows them based on GameManager's state and handles their key presses.
// Level-specific text lives in LevelData assets; everything else is in the Inspector fields below.
[RequireComponent(typeof(Canvas))]
public class NierScreens : MonoBehaviour
{
    [Tooltip("Optional. Uses the TextMesh Pro default font if empty.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Start Screen")]
    [SerializeField] private string headerText = "YoRHa // Bunker Combat Simulation System";
    [SerializeField] private string titleText = "Space Invaders";
    [SerializeField] private string subtitleText = "YoRHa No. 11 Type S // Final Combat Certification";
    [SerializeField] private string startLabel = "[ Start ]";
    [SerializeField] private string startHint = "Press Space or Enter";
    [SerializeField] private string controlsText = "Left / Right : Move      Space : Fire      E / Shift : Pod Program";

    [Header("Story")]
    [SerializeField] private string operatorName = "Operator 17O";
    [Tooltip("Typewriter speed in characters per second")]
    [SerializeField] private float typingSpeed = 45f;
    [Tooltip("Seconds after a screen appears before key presses count, so a held fire key can't skip it")]
    [SerializeField] private float inputDelay = .4f;
    [SerializeField] private string skipHint = "Press any key to skip";
    [SerializeField] private string confirmHint = "Press Space or Enter";

    [Header("Briefing")]
    [SerializeField] private string briefingTag = "Briefing";
    [SerializeField] private string beginLabel = "[ Begin ]";

    [Header("Debrief")]
    [SerializeField] private string debriefTitle = "Simulation Complete";
    [SerializeField] private string continueLabel = "[ Continue ]";
    [SerializeField] private string killsLabel = "Targets Destroyed";
    [SerializeField] private string livesLabel = "Lives Remaining";
    [Tooltip("Shown on the debrief, since every simulation starts with full lives")]
    [SerializeField] private string repairedText = "Unit repaired.";

    [Header("Failure (defaults; each LevelData can override)")]
    [SerializeField] private string failedTitle = "Simulation Failed";
    [SerializeField] private string failedSystemLine = "Unit will be recalibrated.";
    [TextArea(2, 4)]
    [SerializeField] private string failedOperatorMessage = "Don't worry. Nobody passes on the first try.\n...Almost nobody.";
    [SerializeField] private string retryLabel = "[ Retry ]";
    [SerializeField] private string retryHint = "Press R or Enter";

    [Header("Certification (after the final level)")]
    [SerializeField] private string certifiedTitle = "Certification Complete";
    [SerializeField] private string totalKillsLabel = "Total Targets Destroyed";
    [SerializeField] private string livesLostLabel = "Lives Lost";
    [SerializeField] private string retriesLabel = "Retries";

    [Header("Epilogue")]
    [TextArea(2, 4)]
    [SerializeField] private string epilogueText = "Combat data uploaded to Bunker server.\nMemory backup complete.";
    [SerializeField] private float epilogueBlackFade = 1f;
    [SerializeField] private float epiloguePause = 2f;
    [SerializeField] private float epilogueTextFade = 2.5f;

    [Header("Anomaly Glitch (failure title on anomaly levels)")]
    [SerializeField] private float glitchDuration = 1f;
    [Tooltip("Seconds between corrupted frames")]
    [SerializeField] private float glitchInterval = .05f;
    [SerializeField] private string glitchCharacters = "#%&@$*/\\|<>=+?!01_";

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = .25f;
    [Range(0, 1)]
    [SerializeField] private float flashAlpha = .35f;

    private static readonly Vector2 Center = new Vector2(.5f, .5f);

    // Result panel title and its underline (ShowResult moves them sideways when there's a rank badge)
    private const float ResultTitleY = 310;
    private const float ResultTitleBarY = 262;

    private GameManager gameManager;
    private float stateEnteredTime;

    // Title
    private GameObject startScreen;
    private Image startMarker;

    // HUD
    private TextMeshProUGUI levelLabel;

    // Briefing
    private GameObject briefingScreen;
    private TextMeshProUGUI briefingHeader;
    private Image briefingHeaderSquare;
    private TextMeshProUGUI briefingSystem;
    private TypewriterText briefingBody;
    private GameObject briefingMenu;
    private Image briefingMarker;
    private TextMeshProUGUI briefingHint;

    // Results: debrief, failure and certification share one panel
    private GameObject resultScreen;
    private TextMeshProUGUI resultHeader;
    private Image resultHeaderSquare;
    private TextMeshProUGUI resultTitle;
    private Image resultBar;
    private TextMeshProUGUI resultSystem;
    private const int StatRowCount = 3;
    private readonly TextMeshProUGUI[] statLabels = new TextMeshProUGUI[StatRowCount];
    private readonly TextMeshProUGUI[] statValues = new TextMeshProUGUI[StatRowCount];
    private GameObject rankBadge;
    private TextMeshProUGUI rankLetter;
    private TextMeshProUGUI resultSpeaker;
    private TypewriterText resultMessage;
    private GameObject resultMenu;
    private TextMeshProUGUI resultMenuLabel;
    private Image resultMarker;
    private TextMeshProUGUI resultHint;
    private string resultConfirmHint;

    // Epilogue
    private GameObject epilogueScreen;
    private Image epilogueBlack;
    private TextMeshProUGUI epilogueLabel;
    private Image epilogueMarker;

    // Hit flash
    private Image flash;
    private float flashTimer;

    // Glitch on the result title
    private string glitchText;
    private float glitchEndTime;
    private float nextGlitchTime;
    private Vector2 resultTitleRestPosition;

    void Awake()
    {
        // Added last, so it draws over the HUD. Children are drawn in creation order.
        RectTransform root = NierStyle.CreateRect("NierScreens", transform);
        NierStyle.Stretch(root);

        BuildLevelLabel(root);

        flash = NierStyle.CreateImage("HitFlash", root, NierStyle.WithAlpha(NierStyle.Accent, 0));
        NierStyle.Stretch(flash.rectTransform);
        flash.enabled = false;

        BuildStartScreen(root);
        BuildBriefingScreen(root);
        BuildResultScreen(root);
        BuildEpilogueScreen(root);
        ShowOnly(null);
    }

    // Start (not Awake) so GameManager.Awake has already set Instance, State and the level
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

    // ---------- State ----------

    void OnStateChanged(GameManager.GameState state)
    {
        stateEnteredTime = Time.unscaledTime;
        LevelData level = gameManager.CurrentLevel;
        StopGlitch();

        levelLabel.gameObject.SetActive(state == GameManager.GameState.Playing && level != null);
        if (level != null)
        {
            levelLabel.text = level.LevelName;
        }

        switch (state)
        {
            case GameManager.GameState.Title:
                ShowOnly(startScreen);
                break;

            case GameManager.GameState.Briefing:
                ShowOnly(briefingScreen);
                ShowBriefing(level);
                break;

            case GameManager.GameState.Debrief:
                ShowOnly(resultScreen);
                ShowResult(level, debriefTitle, level != null ? level.DebriefSystemLine : "",
                    level != null ? level.Debrief : "", continueLabel, confirmHint, NierStyle.Ink);
                SetStatRow(0, killsLabel, gameManager.AliensDestroyed + " / " + gameManager.TotalAliens);
                SetStatRow(1, livesLabel, gameManager.Lives.ToString());
                SetStatRow(2, repairedText, "");
                break;

            case GameManager.GameState.Won:
                // Run-wide stats and a rank; the Operator's message depends on the rank
                GameManager.CertificationRank rank = gameManager.GetCertificationRank();
                string certificationMessage = rank != null && !string.IsNullOrEmpty(rank.operatorMessage)
                    ? rank.operatorMessage
                    : (level != null ? level.Debrief : "");
                ShowOnly(resultScreen);
                ShowResult(level, certifiedTitle, level != null ? level.DebriefSystemLine : "",
                    certificationMessage, continueLabel, confirmHint, NierStyle.Ink, rank != null ? rank.rank : null);
                SetStatRow(0, totalKillsLabel, gameManager.RunTargetsDestroyed.ToString());
                SetStatRow(1, livesLostLabel, gameManager.RunLivesLost.ToString());
                SetStatRow(2, retriesLabel, gameManager.RunRetries.ToString());
                break;

            case GameManager.GameState.Lost:
                // Each level can override the failure text; empty fields fall back to the defaults here
                string title = Pick(level != null ? level.FailureTitle : null, failedTitle);
                ShowOnly(resultScreen);
                ShowResult(level, title,
                    Pick(level != null ? level.FailureSystemLine : null, failedSystemLine),
                    Pick(level != null ? level.FailureMessage : null, failedOperatorMessage),
                    Pick(level != null ? level.FailureMenuLabel : null, retryLabel),
                    retryHint, NierStyle.Accent);
                SetStatRow(0, killsLabel, gameManager.AliensDestroyed + " / " + gameManager.TotalAliens);
                SetStatRow(1, livesLabel, gameManager.Lives.ToString());
                SetStatRow(2, null, null);
                if (level != null && level.IsAnomaly)
                {
                    StartGlitch(title);
                }
                break;

            case GameManager.GameState.Epilogue:
                // Keep the certification panel underneath so the black fades in over it
                ShowOnly(epilogueScreen);
                resultScreen.SetActive(true);
                epilogueLabel.text = epilogueText;
                UpdateEpilogue();
                break;

            default:
                ShowOnly(null);
                break;
        }
    }

    void ShowOnly(GameObject screen)
    {
        startScreen.SetActive(screen == startScreen);
        briefingScreen.SetActive(screen == briefingScreen);
        resultScreen.SetActive(screen == resultScreen);
        epilogueScreen.SetActive(screen == epilogueScreen);
    }

    void ShowBriefing(LevelData level)
    {
        bool anomaly = level != null && level.IsAnomaly;
        briefingHeader.text = level != null ? level.LevelName : "";
        briefingSystem.text = level != null ? level.BriefingSystemLine : "";
        // Anomaly levels get an orange system line and header square: a small sign something is off
        briefingSystem.color = anomaly ? NierStyle.Accent : NierStyle.Ink;
        briefingHeaderSquare.color = anomaly ? NierStyle.Accent : NierStyle.Background;

        briefingBody.CharactersPerSecond = typingSpeed;
        briefingBody.Play(level != null ? level.Briefing : "");
    }

    // Stat rows are filled in separately with SetStatRow. rank = null hides the rank badge.
    void ShowResult(LevelData level, string title, string systemLine, string message,
        string menuLabel, string hint, Color barColor, string rank = null)
    {
        resultHeader.text = level != null ? level.LevelName : "";
        resultHeaderSquare.color = barColor == NierStyle.Accent ? NierStyle.Accent : NierStyle.Background;
        resultTitle.text = title;
        resultBar.color = barColor;
        resultSystem.text = systemLine;
        resultSystem.color = barColor == NierStyle.Accent ? NierStyle.Accent : NierStyle.Ink;

        // With a rank badge on the right, the title and its bar move left to make room
        bool hasRank = !string.IsNullOrEmpty(rank);
        rankBadge.SetActive(hasRank);
        rankLetter.text = hasRank ? rank : "";
        float titleX = hasRank ? -100 : 0;
        resultTitleRestPosition = new Vector2(titleX, ResultTitleY);
        resultTitle.rectTransform.anchoredPosition = resultTitleRestPosition;
        resultTitle.rectTransform.sizeDelta = new Vector2(hasRank ? 720 : 940, 84);
        resultBar.rectTransform.anchoredPosition = new Vector2(titleX, ResultTitleBarY);

        bool hasMessage = !string.IsNullOrEmpty(message);
        resultSpeaker.gameObject.SetActive(hasMessage);
        resultMessage.CharactersPerSecond = typingSpeed;
        resultMessage.Play(hasMessage ? message : "");

        resultMenuLabel.text = menuLabel;
        resultConfirmHint = hint;
    }

    // label = null hides the row
    void SetStatRow(int row, string label, string value)
    {
        bool visible = label != null;
        statLabels[row].gameObject.SetActive(visible);
        statValues[row].gameObject.SetActive(visible);
        statLabels[row].text = label ?? "";
        statValues[row].text = value ?? "";
    }

    static string Pick(string preferred, string fallback)
    {
        return string.IsNullOrEmpty(preferred) ? fallback : preferred;
    }

    // ---------- Glitch ----------

    void StartGlitch(string text)
    {
        glitchText = text;
        glitchEndTime = Time.unscaledTime + glitchDuration;
        nextGlitchTime = 0;
    }

    void StopGlitch()
    {
        if (glitchEndTime <= 0)
        {
            return;
        }
        glitchEndTime = 0;
        resultTitle.text = glitchText;
        resultTitle.alpha = 1;
        resultTitle.rectTransform.anchoredPosition = resultTitleRestPosition;
    }

    // For the first second: corrupt random characters, jitter sideways and flicker,
    // easing off as the second runs out
    void UpdateGlitch()
    {
        if (glitchEndTime <= 0)
        {
            return;
        }
        if (Time.unscaledTime >= glitchEndTime)
        {
            StopGlitch();
            return;
        }
        if (Time.unscaledTime < nextGlitchTime)
        {
            return;
        }
        nextGlitchTime = Time.unscaledTime + glitchInterval;

        float intensity = (glitchEndTime - Time.unscaledTime) / glitchDuration; // 1 -> 0
        char[] chars = glitchText.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] != ' ' && Random.value < .1f + .5f * intensity)
            {
                chars[i] = glitchCharacters[Random.Range(0, glitchCharacters.Length)];
            }
        }
        resultTitle.text = new string(chars);
        resultTitle.alpha = Random.value < .25f * intensity ? .3f : 1f;
        resultTitle.rectTransform.anchoredPosition =
            resultTitleRestPosition + new Vector2(Random.Range(-12f, 12f) * intensity, 0);
    }

    void OnLivesChanged(int lives)
    {
        // Lives only change when the player is hit
        flashTimer = flashDuration;
        flash.enabled = true;
    }

    // ---------- Per frame ----------

    // Unscaled time throughout, because every screen is shown while the game is frozen (timeScale 0)
    void Update()
    {
        bool markerOn = Time.unscaledTime % 1f < .6f;
        startMarker.enabled = markerOn;
        briefingMarker.enabled = markerOn;
        resultMarker.enabled = markerOn;

        // Menu items appear once the typewriter has finished
        bool briefingDone = briefingBody.IsComplete;
        briefingMenu.SetActive(briefingDone);
        briefingHint.text = briefingDone ? confirmHint : skipHint;

        bool resultDone = resultMessage.IsComplete;
        resultMenu.SetActive(resultDone);
        resultHint.text = resultDone ? resultConfirmHint : skipHint;

        if (epilogueScreen.activeSelf)
        {
            UpdateEpilogue();
        }

        UpdateGlitch();

        if (flashTimer > 0)
        {
            flashTimer -= Time.unscaledDeltaTime;
            flash.color = NierStyle.WithAlpha(NierStyle.Accent, flashAlpha * Mathf.Clamp01(flashTimer / flashDuration));
            flash.enabled = flashTimer > 0;
        }
    }

    // Black fades in, then a pause, then the quiet text fades in
    void UpdateEpilogue()
    {
        float t = Time.unscaledTime - stateEnteredTime;
        epilogueBlack.color = NierStyle.WithAlpha(Color.black, Mathf.Clamp01(t / epilogueBlackFade));

        float textAlpha = Mathf.Clamp01((t - epilogueBlackFade - epiloguePause) / epilogueTextFade);
        epilogueLabel.color = NierStyle.WithAlpha(NierStyle.BackgroundDark, textAlpha * .8f);
        epilogueMarker.enabled = EpilogueReady() && Time.unscaledTime % 1.4f < .7f;
    }

    bool EpilogueReady()
    {
        return Time.unscaledTime - stateEnteredTime >= epilogueBlackFade + epiloguePause + epilogueTextFade;
    }

    // LateUpdate, after Spaceship.Update, so the key that starts a level doesn't also fire a dart
    void LateUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (gameManager == null || keyboard == null || Time.unscaledTime - stateEnteredTime < inputDelay)
        {
            return;
        }

        bool confirm = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;
        bool anyKey = keyboard.anyKey.wasPressedThisFrame;

        switch (gameManager.State)
        {
            case GameManager.GameState.Title:
                if (confirm)
                {
                    gameManager.StartFromTitle();
                }
                break;

            case GameManager.GameState.Briefing:
                if (!briefingBody.IsComplete)
                {
                    if (anyKey)
                    {
                        briefingBody.Complete();
                    }
                }
                else if (confirm)
                {
                    gameManager.BeginLevel();
                }
                break;

            case GameManager.GameState.Debrief:
            case GameManager.GameState.Won:
            case GameManager.GameState.Lost:
                if (!resultMessage.IsComplete)
                {
                    if (anyKey)
                    {
                        resultMessage.Complete();
                    }
                }
                else if (confirm || (gameManager.State == GameManager.GameState.Lost && keyboard.rKey.wasPressedThisFrame))
                {
                    if (gameManager.State == GameManager.GameState.Debrief)
                    {
                        gameManager.ContinueToNextLevel();
                    }
                    else if (gameManager.State == GameManager.GameState.Won)
                    {
                        gameManager.ShowEpilogue();
                    }
                    else
                    {
                        gameManager.Retry();
                    }
                }
                break;

            case GameManager.GameState.Epilogue:
                if (anyKey && EpilogueReady())
                {
                    gameManager.ReturnToTitle();
                }
                break;
        }
    }

    // ---------- Building ----------

    void BuildLevelLabel(Transform root)
    {
        levelLabel = NierStyle.CreateText("LevelLabel", root, "", 24, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(levelLabel.rectTransform, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(900, 40));
        levelLabel.gameObject.SetActive(false);
    }

    void BuildStartScreen(Transform root)
    {
        Image screen = NierStyle.CreateImage("StartScreen", root, NierStyle.WithAlpha(NierStyle.Background, .9f));
        NierStyle.Stretch(screen.rectTransform);
        startScreen = screen.gameObject;

        RectTransform frame = NierStyle.CreateRect("Frame", screen.transform);
        NierStyle.Stretch(frame, 60);
        NierStyle.CreateFrame(frame, NierStyle.Ink, 2);

        TextMeshProUGUI header = NierStyle.CreateText("Header", frame, headerText, 22,
            NierStyle.Ink, TextAlignmentOptions.TopLeft, font);
        NierStyle.Place(header.rectTransform, new Vector2(0, 1), new Vector2(32, -26), new Vector2(1000, 40));

        // Title block between two thin lines, with small squares on the line ends
        CreateLine(screen.transform, new Vector2(0, 175), 1100);
        CreateLine(screen.transform, new Vector2(0, -60), 1100);

        TextMeshProUGUI title = NierStyle.CreateText("Title", screen.transform, titleText, 110, NierStyle.InkDark,
            TextAlignmentOptions.Center, font, 24);
        NierStyle.Place(title.rectTransform, Center, new Vector2(0, 85), new Vector2(1700, 140));

        TextMeshProUGUI subtitle = NierStyle.CreateText("Subtitle", screen.transform, subtitleText, 34, NierStyle.Ink,
            TextAlignmentOptions.Center, font, 10, false);
        NierStyle.Place(subtitle.rectTransform, Center, new Vector2(0, -8), new Vector2(1600, 50));

        startMarker = NierStyle.CreateMenuItem(screen.transform, startLabel, new Vector2(0, -175), font);

        TextMeshProUGUI hint = NierStyle.CreateText("Hint", screen.transform, startHint, 24, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(hint.rectTransform, Center, new Vector2(0, -245), new Vector2(1000, 40));

        TextMeshProUGUI controls = NierStyle.CreateText("Controls", frame, controlsText, 24, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(controls.rectTransform, new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(1600, 40));
    }

    void BuildBriefingScreen(Transform root)
    {
        Image dim = NierStyle.CreateImage("BriefingScreen", root, NierStyle.WithAlpha(NierStyle.Background, .75f));
        NierStyle.Stretch(dim.rectTransform);
        briefingScreen = dim.gameObject;

        // Panel is 660 tall: y runs from +330 (top) to -330 (bottom)
        Image panel = CreatePanel(dim.transform, new Vector2(1100, 660));
        CreateHeader(panel.transform, out briefingHeader, out briefingHeaderSquare, briefingTag);

        briefingSystem = NierStyle.CreateText("SystemLine", panel.transform, "", 24, NierStyle.Ink,
            TextAlignmentOptions.Left, font, 6);
        NierStyle.Place(briefingSystem.rectTransform, Center, new Vector2(0, 238), new Vector2(1000, 40));

        CreateLine(panel.transform, new Vector2(0, 206), 960);

        TextMeshProUGUI speaker = NierStyle.CreateText("Speaker", panel.transform, operatorName, 22, NierStyle.Ink,
            TextAlignmentOptions.Left, font, 8);
        NierStyle.Place(speaker.rectTransform, Center, new Vector2(0, 176), new Vector2(1000, 34));

        // Room for about seven lines (each "..." pause starts a new line)
        briefingBody = CreateMessage(panel.transform, new Vector2(0, 8), new Vector2(1000, 300), 28);

        CreateLine(panel.transform, new Vector2(0, -158), 960);

        RectTransform menu = NierStyle.CreateRect("Menu", panel.transform);
        NierStyle.Stretch(menu);
        briefingMenu = menu.gameObject;
        briefingMarker = NierStyle.CreateMenuItem(menu, beginLabel, new Vector2(0, -218), font);

        briefingHint = NierStyle.CreateText("Hint", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(briefingHint.rectTransform, Center, new Vector2(0, -285), new Vector2(900, 36));
    }

    void BuildResultScreen(Transform root)
    {
        Image dim = NierStyle.CreateImage("ResultScreen", root, NierStyle.WithAlpha(NierStyle.Background, .6f));
        NierStyle.Stretch(dim.rectTransform);
        resultScreen = dim.gameObject;

        // Panel is 840 tall: y runs from +420 (top) to -420 (bottom)
        Image panel = CreatePanel(dim.transform, new Vector2(1000, 840));
        CreateHeader(panel.transform, out resultHeader, out resultHeaderSquare, "Result");

        resultTitle = NierStyle.CreateText("Title", panel.transform, "", 60, NierStyle.InkDark,
            TextAlignmentOptions.Center, font, 18);
        // Shrinks long titles like "SIMULATION INTEGRITY COMPROMISED" to fit the panel
        resultTitle.enableAutoSizing = true;
        resultTitle.fontSizeMin = 28;
        resultTitle.fontSizeMax = 60;
        NierStyle.Place(resultTitle.rectTransform, Center, new Vector2(0, ResultTitleY), new Vector2(940, 84));
        resultTitleRestPosition = resultTitle.rectTransform.anchoredPosition;

        resultBar = NierStyle.CreateImage("TitleBar", panel.transform, NierStyle.Ink);
        NierStyle.Place(resultBar.rectTransform, Center, new Vector2(0, ResultTitleBarY), new Vector2(420, 4));

        // Certification rank: a dark square with the letter, to the right of the title
        Image badge = NierStyle.CreateImage("RankBadge", panel.transform, NierStyle.InkDark);
        NierStyle.Place(badge.rectTransform, Center, new Vector2(400, 300), new Vector2(100, 100));
        rankLetter = NierStyle.CreateText("Rank", badge.transform, "", 72, NierStyle.Background,
            TextAlignmentOptions.Center, font, 0);
        NierStyle.Stretch(rankLetter.rectTransform);
        rankBadge = badge.gameObject;
        rankBadge.SetActive(false);

        resultSystem = NierStyle.CreateText("SystemLine", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font, 6);
        NierStyle.Place(resultSystem.rectTransform, Center, new Vector2(0, 222), new Vector2(940, 36));

        CreateLine(panel.transform, new Vector2(0, 192), 840);
        for (int i = 0; i < StatRowCount; i++)
        {
            CreateStatRow(panel.transform, 157 - i * 42, out statLabels[i], out statValues[i]);
        }
        CreateLine(panel.transform, new Vector2(0, 32), 840);

        resultSpeaker = NierStyle.CreateText("Speaker", panel.transform, operatorName, 20, NierStyle.Ink,
            TextAlignmentOptions.Left, font, 8);
        NierStyle.Place(resultSpeaker.rectTransform, Center, new Vector2(0, 6), new Vector2(840, 30));

        // Room for about eight lines (the rank messages are seven) between the speaker label and the menu item
        resultMessage = CreateMessage(panel.transform, new Vector2(0, -145), new Vector2(840, 250), 22);

        RectTransform menu = NierStyle.CreateRect("Menu", panel.transform);
        NierStyle.Stretch(menu);
        resultMenu = menu.gameObject;
        resultMarker = NierStyle.CreateMenuItem(menu, continueLabel, new Vector2(0, -315), font, out resultMenuLabel);

        resultHint = NierStyle.CreateText("Hint", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(resultHint.rectTransform, Center, new Vector2(0, -385), new Vector2(800, 36));
    }

    void BuildEpilogueScreen(Transform root)
    {
        epilogueBlack = NierStyle.CreateImage("EpilogueScreen", root, Color.black);
        NierStyle.Stretch(epilogueBlack.rectTransform);
        epilogueScreen = epilogueBlack.gameObject;

        epilogueLabel = NierStyle.CreateText("Text", epilogueBlack.transform, "", 24, NierStyle.BackgroundDark,
            TextAlignmentOptions.Center, font, 6, false);
        epilogueLabel.lineSpacing = 30;
        NierStyle.Place(epilogueLabel.rectTransform, Center, Vector2.zero, new Vector2(1200, 120));

        epilogueMarker = NierStyle.CreateImage("Marker", epilogueBlack.transform,
            NierStyle.WithAlpha(NierStyle.BackgroundDark, .5f));
        NierStyle.Place(epilogueMarker.rectTransform, new Vector2(1, 0), new Vector2(-80, 80), new Vector2(10, 10));
    }

    // ---------- Building helpers ----------

    Image CreatePanel(Transform parent, Vector2 size)
    {
        Image panel = NierStyle.CreateImage("Panel", parent, NierStyle.Background);
        NierStyle.Place(panel.rectTransform, Center, Vector2.zero, size);
        NierStyle.CreateFrame(panel.transform, NierStyle.InkDark, 2);
        return panel;
    }

    // Dark bar along the top of a panel: title on the left, a small tag and square on the right
    void CreateHeader(Transform panel, out TextMeshProUGUI title, out Image square, string tag)
    {
        Image bar = NierStyle.CreateImage("Header", panel, NierStyle.InkDark);
        RectTransform rect = bar.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 64);

        title = NierStyle.CreateText("Title", bar.transform, "", 28, NierStyle.Background,
            TextAlignmentOptions.Left, font);
        NierStyle.Stretch(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(30, 0);

        TextMeshProUGUI tagText = NierStyle.CreateText("Tag", bar.transform, tag, 20, NierStyle.BackgroundDark,
            TextAlignmentOptions.Right, font, 8);
        NierStyle.Place(tagText.rectTransform, new Vector2(1, .5f), new Vector2(-60, 0), new Vector2(300, 40));

        square = NierStyle.CreateImage("Square", bar.transform, NierStyle.Background);
        NierStyle.Place(square.rectTransform, new Vector2(1, .5f), new Vector2(-30, 0), new Vector2(14, 14));
    }

    // Wrapped, sentence-case dialogue text with a typewriter
    TypewriterText CreateMessage(Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI text = NierStyle.CreateText("Message", parent, "", fontSize, NierStyle.InkDark,
            TextAlignmentOptions.TopLeft, font, 2, false);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 12;
        NierStyle.Place(text.rectTransform, Center, position, size);
        return text.gameObject.AddComponent<TypewriterText>();
    }

    // Label on the left, value on the right; both filled in later by SetStatRow
    void CreateStatRow(Transform parent, float y, out TextMeshProUGUI labelText, out TextMeshProUGUI valueText)
    {
        labelText = NierStyle.CreateText("StatLabel", parent, "", 26, NierStyle.Ink,
            TextAlignmentOptions.Left, font);
        NierStyle.Place(labelText.rectTransform, Center, new Vector2(-210, y), new Vector2(420, 40));

        valueText = NierStyle.CreateText("StatValue", parent, "", 26, NierStyle.InkDark,
            TextAlignmentOptions.Right, font);
        NierStyle.Place(valueText.rectTransform, Center, new Vector2(210, y), new Vector2(420, 40));
    }

    // Thin horizontal line with a small square at each end
    void CreateLine(Transform parent, Vector2 position, float width)
    {
        Image line = NierStyle.CreateImage("Line", parent, NierStyle.Ink);
        NierStyle.Place(line.rectTransform, Center, position, new Vector2(width, 2));

        Image left = NierStyle.CreateImage("LineEnd", parent, NierStyle.Ink);
        NierStyle.Place(left.rectTransform, Center, position + new Vector2(-width / 2 - 12, 0), new Vector2(10, 10));
        Image right = NierStyle.CreateImage("LineEnd", parent, NierStyle.Ink);
        NierStyle.Place(right.rectTransform, Center, position + new Vector2(width / 2 + 12, 0), new Vector2(10, 10));
    }
}
