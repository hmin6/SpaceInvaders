using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Builds every NieR-styled screen at runtime as children of this Canvas: title, briefing,
// debrief / failure / certification results, the epilogue, the HUD level label and the hit flash.
// Shows them based on GameManager's state and handles their key presses.
// Level-specific conversations live in LevelData assets (certification in GameManager);
// everything else is in the Inspector fields below.
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
    [Tooltip("Used only if there's no Pod in the scene; otherwise the Pod's own Designation is shown")]
    [SerializeField] private string podName = "POD 217";
    [Tooltip("Typewriter speed in characters per second")]
    [SerializeField] private float typingSpeed = 45f;
    [Tooltip("Seconds after a screen appears before key presses count, so a held fire key can't skip it")]
    [SerializeField] private float inputDelay = .4f;
    [SerializeField] private string skipHint = "Press any key to skip";
    [SerializeField] private string confirmHint = "Press Space or Enter";
    [Tooltip("Shown when a conversation continues on another page")]
    [SerializeField] private string nextHint = "Press Space or Enter : Next";

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
    [SerializeField] private List<DialogueLine> failedDialogue = new List<DialogueLine>
    {
        DialogueLine.Operator("Don't worry. Nobody passes on the first try."),
        DialogueLine.Operator("...Almost nobody."),
        DialogueLine.Pod("Proposal: Retry."),
    };
    [SerializeField] private string retryLabel = "[ Retry ]";
    [SerializeField] private string retryHint = "Press R or Enter";

    [Header("Certification (after the final level)")]
    [SerializeField] private string certifiedTitle = "Certification Complete";
    [SerializeField] private string totalKillsLabel = "Total Targets Destroyed";
    [SerializeField] private string livesLostLabel = "Lives Lost";
    [SerializeField] private string retriesLabel = "Retries";

    [Header("Epilogue")]
    [SerializeField] private string[] epilogueSystemLines =
    {
        "Combat data uploaded to Bunker server.",
        "Memory backup complete.",
    };
    [Tooltip("Shown after the system lines, each prefixed with the Pod's designation in orange")]
    [SerializeField] private string[] epiloguePodLines =
    {
        "Backup confirmed.",
        "...See you again, 11S.",
    };
    [SerializeField] private float epilogueBlackFade = 1f;
    [Tooltip("Seconds of black before the first line")]
    [SerializeField] private float epiloguePause = 2f;
    [Tooltip("Seconds each line takes to fade in")]
    [SerializeField] private float epilogueTextFade = 2.5f;
    [Tooltip("Seconds between system lines")]
    [SerializeField] private float epilogueLineGap = 1.5f;
    [Tooltip("Seconds between the last system line and the first Pod line")]
    [SerializeField] private float epiloguePodPause = 3.5f;
    [Tooltip("Seconds between Pod lines")]
    [SerializeField] private float epiloguePodGap = 2.5f;

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
    private const float ResultTitleY = 370;
    private const float ResultTitleBarY = 322;

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
    private DialogueView briefingDialogue;
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
    private DialogueView resultDialogue;
    private GameObject resultMenu;
    private TextMeshProUGUI resultMenuLabel;
    private Image resultMarker;
    private TextMeshProUGUI resultHint;
    private string resultConfirmHint;

    // Epilogue
    private GameObject epilogueScreen;
    private Image epilogueBlack;
    private TextMeshProUGUI[] epilogueLines;
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
        Pod pod = FindAnyObjectByType<Pod>();
        if (pod != null)
        {
            podName = pod.Designation;
        }
        foreach (DialogueView view in new[] { briefingDialogue, resultDialogue })
        {
            view.SetNames(operatorName, podName);
            view.TypingSpeed = typingSpeed;
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
                    level != null ? level.DebriefDialogue : null, continueLabel, confirmHint, NierStyle.Ink);
                SetStatRow(0, killsLabel, gameManager.AliensDestroyed + " / " + gameManager.TotalAliens);
                SetStatRow(1, livesLabel, gameManager.Lives.ToString());
                SetStatRow(2, repairedText, "");
                break;

            case GameManager.GameState.Won:
                // Run-wide stats and a rank; the conversation depends on the rank
                GameManager.CertificationRank rank = gameManager.GetCertificationRank();
                ShowOnly(resultScreen);
                ShowResult(level, certifiedTitle, level != null ? level.DebriefSystemLine : "",
                    PickDialogue(gameManager.GetCertificationDialogue(rank), level != null ? level.DebriefDialogue : null),
                    continueLabel, confirmHint, NierStyle.Ink, rank != null ? rank.rank : null);
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
                    PickDialogue(level != null ? level.FailureDialogue : null, failedDialogue),
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

        briefingDialogue.Show(level != null ? level.BriefingDialogue : null);
    }

    // Stat rows are filled in separately with SetStatRow. rank = null hides the rank badge.
    void ShowResult(LevelData level, string title, string systemLine, IReadOnlyList<DialogueLine> dialogue,
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

        resultDialogue.Show(dialogue);

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

    static IReadOnlyList<DialogueLine> PickDialogue(IReadOnlyList<DialogueLine> preferred, IReadOnlyList<DialogueLine> fallback)
    {
        return preferred != null && preferred.Count > 0 ? preferred : fallback;
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

        UpdateDialogueControls(briefingDialogue, briefingMenu, briefingHint, confirmHint);
        UpdateDialogueControls(resultDialogue, resultMenu, resultHint, resultConfirmHint);

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

    // The menu item appears once the whole conversation has been shown; until then the hint says
    // whether a key skips the typing or turns the page
    void UpdateDialogueControls(DialogueView view, GameObject menu, TextMeshProUGUI hint, string finishedHint)
    {
        menu.SetActive(view.IsFinished);
        hint.text = !view.IsPageComplete ? skipHint : view.HasNextPage ? nextHint : finishedHint;
    }

    // Black fades in, then after a pause the system lines fade in one by one, then after a longer
    // pause the Pod's lines (with its designation in orange)
    void UpdateEpilogue()
    {
        float t = Time.unscaledTime - stateEnteredTime;
        epilogueBlack.color = NierStyle.WithAlpha(Color.black, Mathf.Clamp01(t / epilogueBlackFade));

        string accent = ColorUtility.ToHtmlStringRGB(NierStyle.Accent);
        for (int i = 0; i < epilogueLines.Length; i++)
        {
            float alpha = Mathf.Clamp01((t - EpilogueLineStart(i)) / epilogueTextFade);
            epilogueLines[i].color = NierStyle.WithAlpha(NierStyle.BackgroundDark, alpha * .8f);
            if (i >= epilogueSystemLines.Length)
            {
                // The label's color tag needs its own alpha to fade with the line
                string labelAlpha = Mathf.RoundToInt(alpha * 255).ToString("X2");
                epilogueLines[i].text = "<color=#" + accent + labelAlpha + ">" + podName + ":</color> "
                    + epiloguePodLines[i - epilogueSystemLines.Length];
            }
        }
        epilogueMarker.enabled = EpilogueReady() && Time.unscaledTime % 1.4f < .7f;
    }

    // Seconds after the epilogue starts that line i (system lines first, then Pod lines) begins fading in
    float EpilogueLineStart(int i)
    {
        float firstLine = epilogueBlackFade + epiloguePause;
        if (i < epilogueSystemLines.Length)
        {
            return firstLine + i * epilogueLineGap;
        }
        float lastSystemLine = firstLine + Mathf.Max(0, epilogueSystemLines.Length - 1) * epilogueLineGap;
        return lastSystemLine + epiloguePodPause + (i - epilogueSystemLines.Length) * epiloguePodGap;
    }

    bool EpilogueReady()
    {
        float end = epilogueLines.Length > 0
            ? EpilogueLineStart(epilogueLines.Length - 1) + epilogueTextFade
            : epilogueBlackFade;
        return Time.unscaledTime - stateEnteredTime >= end;
    }

    // Any key finishes the page's typing; Space/Enter then turns the page, or returns true
    // (move on) on the last one
    static bool AdvanceDialogue(DialogueView view, bool anyKey, bool confirm)
    {
        if (!view.IsPageComplete)
        {
            if (anyKey)
            {
                view.CompletePage();
            }
            return false;
        }
        if (!confirm)
        {
            return false;
        }
        if (view.HasNextPage)
        {
            view.NextPage();
            return false;
        }
        return true;
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
                if (AdvanceDialogue(briefingDialogue, anyKey, confirm))
                {
                    gameManager.BeginLevel();
                }
                break;

            case GameManager.GameState.Debrief:
            case GameManager.GameState.Won:
            case GameManager.GameState.Lost:
                bool proceed = confirm || (gameManager.State == GameManager.GameState.Lost && keyboard.rKey.wasPressedThisFrame);
                if (AdvanceDialogue(resultDialogue, anyKey, proceed))
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

        // Panel is 760 tall: y runs from +380 (top) to -380 (bottom)
        Image panel = CreatePanel(dim.transform, new Vector2(1100, 760));
        CreateHeader(panel.transform, out briefingHeader, out briefingHeaderSquare, briefingTag);

        briefingSystem = NierStyle.CreateText("SystemLine", panel.transform, "", 24, NierStyle.Ink,
            TextAlignmentOptions.Left, font, 6);
        NierStyle.Place(briefingSystem.rectTransform, Center, new Vector2(0, 288), new Vector2(1000, 40));

        CreateLine(panel.transform, new Vector2(0, 256), 960);

        // About six one-line entries per page; longer conversations page with Space
        briefingDialogue = DialogueView.Create(panel.transform, new Vector2(0, 21), new Vector2(1000, 430), 24, font);

        CreateLine(panel.transform, new Vector2(0, -214), 960);

        RectTransform menu = NierStyle.CreateRect("Menu", panel.transform);
        NierStyle.Stretch(menu);
        briefingMenu = menu.gameObject;
        briefingMarker = NierStyle.CreateMenuItem(menu, beginLabel, new Vector2(0, -274), font);

        briefingHint = NierStyle.CreateText("Hint", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(briefingHint.rectTransform, Center, new Vector2(0, -340), new Vector2(900, 36));
    }

    void BuildResultScreen(Transform root)
    {
        Image dim = NierStyle.CreateImage("ResultScreen", root, NierStyle.WithAlpha(NierStyle.Background, .6f));
        NierStyle.Stretch(dim.rectTransform);
        resultScreen = dim.gameObject;

        // Panel is 960 tall: y runs from +480 (top) to -480 (bottom)
        Image panel = CreatePanel(dim.transform, new Vector2(1000, 960));
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
        NierStyle.Place(badge.rectTransform, Center, new Vector2(400, 360), new Vector2(100, 100));
        rankLetter = NierStyle.CreateText("Rank", badge.transform, "", 72, NierStyle.Background,
            TextAlignmentOptions.Center, font, 0);
        NierStyle.Stretch(rankLetter.rectTransform);
        rankBadge = badge.gameObject;
        rankBadge.SetActive(false);

        resultSystem = NierStyle.CreateText("SystemLine", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font, 6);
        NierStyle.Place(resultSystem.rectTransform, Center, new Vector2(0, 282), new Vector2(940, 36));

        CreateLine(panel.transform, new Vector2(0, 252), 840);
        for (int i = 0; i < StatRowCount; i++)
        {
            CreateStatRow(panel.transform, 217 - i * 42, out statLabels[i], out statValues[i]);
        }
        CreateLine(panel.transform, new Vector2(0, 92), 840);

        // About six one-line entries per page; longer conversations page with Space
        resultDialogue = DialogueView.Create(panel.transform, new Vector2(0, -139), new Vector2(840, 422), 22, font);

        RectTransform menu = NierStyle.CreateRect("Menu", panel.transform);
        NierStyle.Stretch(menu);
        resultMenu = menu.gameObject;
        resultMarker = NierStyle.CreateMenuItem(menu, continueLabel, new Vector2(0, -395), font, out resultMenuLabel);

        resultHint = NierStyle.CreateText("Hint", panel.transform, "", 22, NierStyle.Ink,
            TextAlignmentOptions.Center, font);
        NierStyle.Place(resultHint.rectTransform, Center, new Vector2(0, -450), new Vector2(800, 36));
    }

    void BuildEpilogueScreen(Transform root)
    {
        epilogueBlack = NierStyle.CreateImage("EpilogueScreen", root, Color.black);
        NierStyle.Stretch(epilogueBlack.rectTransform);
        epilogueScreen = epilogueBlack.gameObject;

        // System lines, a gap, then the Pod's lines, centred as one block
        const float lineHeight = 44f;
        const float groupGap = 40f;
        int count = epilogueSystemLines.Length + epiloguePodLines.Length;
        float blockHeight = count * lineHeight + (epiloguePodLines.Length > 0 ? groupGap : 0);
        float y = blockHeight / 2 - lineHeight / 2;
        epilogueLines = new TextMeshProUGUI[count];
        for (int i = 0; i < count; i++)
        {
            if (i == epilogueSystemLines.Length && i > 0)
            {
                y -= groupGap;
            }
            string content = i < epilogueSystemLines.Length ? epilogueSystemLines[i] : "";
            epilogueLines[i] = NierStyle.CreateText("Line", epilogueBlack.transform, content, 24, NierStyle.BackgroundDark,
                TextAlignmentOptions.Center, font, 6, false);
            NierStyle.Place(epilogueLines[i].rectTransform, Center, new Vector2(0, y), new Vector2(1400, lineHeight));
            y -= lineHeight;
        }

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
