using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The Pod's in-play commentary: a small NieR-style box above the Pod meter (bottom-left) that
// types out a line and hides itself after a few seconds. Built at runtime under this Canvas.
//
// To avoid spam there's a global cooldown after each line. During the cooldown, only a line with a
// higher priority than the one shown can interrupt (e.g. "last life" over "damage sustained").
// Each event picks a random variant, never the same one twice in a row.
[RequireComponent(typeof(Canvas))]
public class PodChatter : MonoBehaviour
{
    [Serializable]
    public class ChatterLines
    {
        [Tooltip("Higher interrupts lower during the cooldown")]
        public int priority = 1;
        [TextArea(1, 3)]
        public string[] variants;

        [NonSerialized] public int lastIndex = -1;

        public ChatterLines()
        {
        }

        public ChatterLines(int priority, params string[] variants)
        {
            this.priority = priority;
            this.variants = variants;
        }
    }

    [Tooltip("Found automatically if left empty")]
    [SerializeField] private Pod pod;
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private AlienFleet fleet;
    [Tooltip("Optional. Uses the TextMesh Pro default font if empty.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Layout")]
    [Tooltip("Bottom-left corner of the box, in reference pixels from the screen's bottom-left (above the Pod meter)")]
    [SerializeField] private Vector2 position = new Vector2(40, 112);
    [SerializeField] private Vector2 size = new Vector2(640, 84);

    [Header("Timing")]
    [Tooltip("Seconds a line stays up")]
    [SerializeField] private float showDuration = 3.5f;
    [Tooltip("Seconds after a line before another of equal or lower priority can be shown")]
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float fadeOut = .3f;
    [SerializeField] private float typingSpeed = 55f;

    [Header("Triggers")]
    [Tooltip("Laser kills needed for the multi-kill line")]
    [SerializeField] private int laserMultiKillCount = 3;
    [Tooltip("Fleet speed multipliers that trigger the acceleration line (once each per level)")]
    [SerializeField] private float[] speedAlertMultipliers = { 2.5f, 4f };
    [Tooltip("Seconds between anomaly remarks on anomaly levels (random within this range)")]
    [SerializeField] private Vector2 anomalyInterval = new Vector2(12f, 20f);

    [Header("Lines")]
    [SerializeField] private ChatterLines levelStart = new ChatterLines(1,
        "Report: Combat simulation initiated.",
        "Report: Hostile targets detected. Engaging.");
    [SerializeField] private ChatterLines podProgramReady = new ChatterLines(1,
        "Proposal: Pod Program available.",
        "Report: Laser charge complete.");
    [SerializeField] private ChatterLines laserMultiKill = new ChatterLines(1,
        "Analysis: Multiple targets eliminated. Efficient.",
        "Report: Target cluster neutralized.");
    [SerializeField] private ChatterLines playerHit = new ChatterLines(1,
        "Alert: Damage sustained.",
        "Alert: Unit 11S hit. Proposal: Increase evasive maneuvers.");
    [SerializeField] private ChatterLines lastLife = new ChatterLines(3,
        "Alert: Unit integrity critical. Proposal: Avoid further damage.",
        "Alert: One more hit will terminate the simulation.");
    [SerializeField] private ChatterLines fleetAccelerating = new ChatterLines(1,
        "Analysis: Enemy movement speed increasing.",
        "Alert: Enemy formation accelerating.");
    [SerializeField] private ChatterLines threeTargetsLeft = new ChatterLines(1,
        "Report: Three targets remaining.");
    [SerializeField] private ChatterLines finalTarget = new ChatterLines(2,
        "Report: Final target.");
    [SerializeField] private ChatterLines firstDarkBullet = new ChatterLines(2,
        "Alert: Indestructible projectiles detected. Proposal: Evade.",
        "Analysis: Dark projectiles cannot be intercepted.");
    [SerializeField] private ChatterLines anomalyRemarks = new ChatterLines(0,
        "Query: Enemy data has no origin record.",
        "Report: Request to log anomaly... denied.",
        "Analysis: This simulation was not authored by the Bunker.",
        "Alert: Unidentified signal in simulation layer.");

    private GameManager gameManager;
    private CanvasGroup group;
    private TextMeshProUGUI label;
    private TypewriterText text;

    private bool showing;
    private float hideTime;
    private float cooldownEnd;
    private int activePriority;
    private float nextAnomalyTime;
    private int nextSpeedAlert;
    private bool darkBulletSeen;

    void Awake()
    {
        RectTransform root = NierStyle.CreateRect("PodChatter", transform);
        NierStyle.Stretch(root);
        // First child, so the start/end screens (built later by NierScreens) draw over it
        root.SetAsFirstSibling();

        Image box = NierStyle.CreateImage("Box", root, NierStyle.WithAlpha(NierStyle.Background, .94f));
        NierStyle.Place(box.rectTransform, Vector2.zero, position, size);
        NierStyle.CreateFrame(box.transform, NierStyle.InkDark, 2);
        group = box.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;

        label = NierStyle.CreateText("Speaker", box.transform, "POD 217", 18, NierStyle.Accent,
            TextAlignmentOptions.TopLeft, font, 8);
        NierStyle.Place(label.rectTransform, new Vector2(0, 1), new Vector2(16, -8), new Vector2(size.x - 32, 24));

        TextMeshProUGUI line = NierStyle.CreateText("Text", box.transform, "", 22, NierStyle.InkDark,
            TextAlignmentOptions.TopLeft, font, 2, false);
        line.textWrappingMode = TextWrappingModes.Normal;
        NierStyle.Place(line.rectTransform, new Vector2(0, 1), new Vector2(16, -34), new Vector2(size.x - 32, size.y - 40));
        text = line.gameObject.AddComponent<TypewriterText>();
    }

    void Start()
    {
        if (pod == null)
        {
            pod = FindAnyObjectByType<Pod>();
        }
        if (fleet == null)
        {
            fleet = FindAnyObjectByType<AlienFleet>();
        }
        if (pod != null)
        {
            label.text = pod.Designation;
            pod.Ready += OnPodReady;
            pod.LaserFinished += OnLaserFinished;
        }
        if (fleet != null)
        {
            fleet.AliensRemainingChanged += OnAliensRemainingChanged;
            fleet.BulletFired += OnBulletFired;
        }

        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.StateChanged += OnStateChanged;
            gameManager.LivesChanged += OnLivesChanged;
            // A retry starts straight in Playing, so announce it here too
            if (gameManager.IsPlaying)
            {
                OnStateChanged(gameManager.State);
            }
        }
    }

    void OnDestroy()
    {
        if (pod != null)
        {
            pod.Ready -= OnPodReady;
            pod.LaserFinished -= OnLaserFinished;
        }
        if (fleet != null)
        {
            fleet.AliensRemainingChanged -= OnAliensRemainingChanged;
            fleet.BulletFired -= OnBulletFired;
        }
        if (gameManager != null)
        {
            gameManager.StateChanged -= OnStateChanged;
            gameManager.LivesChanged -= OnLivesChanged;
        }
    }

    bool Playing => gameManager == null || gameManager.IsPlaying;

    bool IsAnomalyLevel => gameManager != null && gameManager.CurrentLevel != null && gameManager.CurrentLevel.IsAnomaly;

    // ---------- Events ----------

    void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            nextAnomalyTime = Time.time + UnityEngine.Random.Range(anomalyInterval.x, anomalyInterval.y);
            Say(levelStart);
        }
        else
        {
            // Screens cover the HUD; don't leave a line hanging behind them
            showing = false;
            group.alpha = 0;
        }
    }

    void OnPodReady()
    {
        Say(podProgramReady);
    }

    void OnLaserFinished(int kills)
    {
        if (kills >= laserMultiKillCount)
        {
            Say(laserMultiKill);
        }
    }

    void OnLivesChanged(int lives)
    {
        if (lives == 1)
        {
            Say(lastLife);
        }
        else if (lives > 1)
        {
            Say(playerHit);
        }
    }

    void OnAliensRemainingChanged(int remaining)
    {
        if (remaining == 3)
        {
            Say(threeTargetsLeft);
        }
        else if (remaining == 1)
        {
            Say(finalTarget);
        }

        if (fleet != null && nextSpeedAlert < speedAlertMultipliers.Length
            && fleet.SpeedMultiplier >= speedAlertMultipliers[nextSpeedAlert])
        {
            // Skip any thresholds passed at the same time, so this only fires once
            while (nextSpeedAlert < speedAlertMultipliers.Length && fleet.SpeedMultiplier >= speedAlertMultipliers[nextSpeedAlert])
            {
                nextSpeedAlert++;
            }
            Say(fleetAccelerating);
        }
    }

    void OnBulletFired(bool dark)
    {
        if (dark && !darkBulletSeen)
        {
            darkBulletSeen = true;
            Say(firstDarkBullet);
        }
    }

    // ---------- Showing lines ----------

    void Say(ChatterLines lines)
    {
        if (!Playing || lines == null || lines.variants == null || lines.variants.Length == 0)
        {
            return;
        }
        if (Time.time < cooldownEnd && lines.priority <= activePriority)
        {
            return;
        }

        activePriority = lines.priority;
        cooldownEnd = Time.time + cooldown;
        hideTime = Time.time + showDuration;
        showing = true;
        group.alpha = 1;
        text.CharactersPerSecond = typingSpeed;
        text.Play(Pick(lines));
    }

    static string Pick(ChatterLines lines)
    {
        int index = UnityEngine.Random.Range(0, lines.variants.Length);
        if (lines.variants.Length > 1 && index == lines.lastIndex)
        {
            index = (index + 1 + UnityEngine.Random.Range(0, lines.variants.Length - 1)) % lines.variants.Length;
        }
        lines.lastIndex = index;
        return lines.variants[index];
    }

    // Game time, so lines pause with the game
    void Update()
    {
        if (Playing && IsAnomalyLevel && Time.time >= nextAnomalyTime)
        {
            nextAnomalyTime = Time.time + UnityEngine.Random.Range(anomalyInterval.x, anomalyInterval.y);
            Say(anomalyRemarks);
        }

        if (showing && Time.time >= hideTime)
        {
            group.alpha = Mathf.Clamp01(1f - (Time.time - hideTime) / fadeOut);
            if (group.alpha <= 0)
            {
                showing = false;
            }
        }
    }
}
