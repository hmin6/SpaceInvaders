using System.Collections.Generic;
using UnityEngine;

// One combat simulation: fleet and firing settings plus the briefing, debrief and failure conversations.
// GameManager plays a list of these in order. Values are read-only at runtime, because changes
// made to a ScriptableObject during Play mode are saved into the asset.
// Create via Assets > Create > YoRHa > Level Data, or Tools > YoRHa > Create Default Levels.
[CreateAssetMenu(fileName = "LevelData", menuName = "YoRHa/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Simulation")]
    [SerializeField] private string levelName = "SIM 01: BASIC COMBAT";
    [Tooltip("Hook for later phases (glitch effects, new enemy types). Currently only tints the briefing.")]
    [SerializeField] private bool isAnomaly = false;
    [Tooltip("Optional. Spawn this enemy prefab instead of the fleet's default alien.")]
    [SerializeField] private GameObject alienPrefabOverride;

    [Header("Fleet")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 3;
    [Tooltip("Sideways speed in units/sec with the full fleet. Also the speed the fleet steps down at.")]
    [SerializeField] private float baseSpeed = 5f;
    [Tooltip("Speed multiplier when only one alien is left")]
    [SerializeField] private float maxSpeedMultiplier = 5f;
    [Tooltip("1 = same speed-up per kill. Higher values save more of the speed-up for the last few aliens.")]
    [SerializeField] private float speedUpExponent = 1.5f;

    [Header("Firing")]
    [Tooltip("Shots per second from the whole fleet with the full fleet")]
    [SerializeField] private float baseFireRate = 1.5f;
    [Tooltip("Shots per second from the whole fleet when only one alien is left")]
    [SerializeField] private float maxFireRate = 3.5f;
    [Range(0, 1)]
    [SerializeField] private float aimedShotChance = .35f;
    [Tooltip("Alien bullet speed in units/sec")]
    [SerializeField] private float alienBulletSpeed = 9f;
    [Tooltip("Share of shots that are dark (indestructible: must be dodged) instead of orange (darts destroy them)")]
    [Range(0, 1)]
    [SerializeField] private float darkBulletChance = 0f;

    [Header("Briefing")]
    [Tooltip("Short formal system line shown above the conversation")]
    [SerializeField] private string briefingSystemLine;
    [SerializeField] private List<DialogueLine> briefingDialogue = new List<DialogueLine>();

    [Header("Debrief")]
    [SerializeField] private string debriefSystemLine;
    [Tooltip("Not shown on the final level, where GameManager's certification conversation is used instead")]
    [SerializeField] private List<DialogueLine> debriefDialogue = new List<DialogueLine>();

    [Header("Failure (leave empty to use the NierScreens defaults)")]
    [SerializeField] private string failureTitle;
    [SerializeField] private string failureSystemLine;
    [SerializeField] private List<DialogueLine> failureDialogue = new List<DialogueLine>();
    [SerializeField] private string failureMenuLabel;

    public string LevelName => levelName;
    public bool IsAnomaly => isAnomaly;
    public GameObject AlienPrefabOverride => alienPrefabOverride;
    public int Columns => columns;
    public int Rows => rows;
    public float BaseSpeed => baseSpeed;
    public float MaxSpeedMultiplier => maxSpeedMultiplier;
    public float SpeedUpExponent => speedUpExponent;
    public float BaseFireRate => baseFireRate;
    public float MaxFireRate => maxFireRate;
    public float AimedShotChance => aimedShotChance;
    public float AlienBulletSpeed => alienBulletSpeed;
    public float DarkBulletChance => darkBulletChance;
    public string BriefingSystemLine => briefingSystemLine;
    public IReadOnlyList<DialogueLine> BriefingDialogue => briefingDialogue;
    public string DebriefSystemLine => debriefSystemLine;
    public IReadOnlyList<DialogueLine> DebriefDialogue => debriefDialogue;
    public string FailureTitle => failureTitle;
    public string FailureSystemLine => failureSystemLine;
    public IReadOnlyList<DialogueLine> FailureDialogue => failureDialogue;
    public string FailureMenuLabel => failureMenuLabel;

#if UNITY_EDITOR
    // Used only by the Tools > YoRHa level menus to fill in an asset
    public void EditorInitialize(string levelName, bool isAnomaly, int columns, int rows,
        float baseSpeed, float maxSpeedMultiplier, float speedUpExponent,
        float baseFireRate, float maxFireRate, float aimedShotChance, float alienBulletSpeed,
        float darkBulletChance, string briefingSystemLine, DialogueLine[] briefing,
        string debriefSystemLine, DialogueLine[] debrief, DialogueLine[] failureDialogue,
        string failureTitle = "", string failureSystemLine = "", string failureMenuLabel = "")
    {
        this.levelName = levelName;
        this.isAnomaly = isAnomaly;
        this.columns = columns;
        this.rows = rows;
        this.baseSpeed = baseSpeed;
        this.maxSpeedMultiplier = maxSpeedMultiplier;
        this.speedUpExponent = speedUpExponent;
        this.baseFireRate = baseFireRate;
        this.maxFireRate = maxFireRate;
        this.aimedShotChance = aimedShotChance;
        this.alienBulletSpeed = alienBulletSpeed;
        this.darkBulletChance = darkBulletChance;
        this.briefingSystemLine = briefingSystemLine;
        this.briefingDialogue = new List<DialogueLine>(briefing);
        this.debriefSystemLine = debriefSystemLine;
        this.debriefDialogue = new List<DialogueLine>(debrief);
        this.failureTitle = failureTitle;
        this.failureSystemLine = failureSystemLine;
        this.failureDialogue = new List<DialogueLine>(failureDialogue);
        this.failureMenuLabel = failureMenuLabel;
    }
#endif
}
