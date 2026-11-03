using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns the game's flow and level progression. Other scripts report what happened (player hit,
// alien destroyed, fleet cleared, aliens landed) and GameManager decides the outcome. UI such as
// NierScreens and LivesDisplay listens to its events and calls its methods for menu choices.
//
// Each level is played in a freshly loaded scene. Static "pending" fields carry the level index,
// lives and starting state across SceneManager.LoadScene.
public class GameManager : MonoBehaviour
{
    // Title -> Briefing -> Playing -> Debrief -> (next level's) Briefing ... -> Won -> Epilogue -> Title
    // Playing -> Lost -> (retry) Playing
    public enum GameState { Title, Briefing, Playing, Debrief, Lost, Won, Epilogue }

    public static GameManager Instance { get; private set; }

    // A certification rank: reached when the run's totals are within both limits.
    [Serializable]
    public class CertificationRank
    {
        public string rank = "S";
        [Tooltip("Most lives the player can lose across the whole run and still get this rank")]
        public int maxLivesLost;
        [Tooltip("Most retries the player can use across the whole run and still get this rank")]
        public int maxRetries;
        [TextArea(3, 6)]
        public string operatorMessage;
    }

    // How the next scene load should start. Statics survive SceneManager.LoadScene, and
    // ResetStatics clears them when entering Play mode (domain reload is disabled).
    private static bool hasPendingStart;
    private static GameState pendingState;
    private static int pendingLevelIndex;

    // Run-wide stats across every simulation. Static so they survive the scene reload between
    // levels; reset when entering Play mode and whenever a new run starts (see Awake).
    private static int runLivesLost;
    private static int runRetries;
    private static int runTargetsDestroyed;

    [Header("Levels")]
    [Tooltip("Played in order. If empty, a single level is played using AlienFleet's own settings.")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();

    [Header("Lives")]
    [Tooltip("Lives at the start of every simulation (the unit is repaired between sims)")]
    [SerializeField] private int startingLives = 3;

    [Header("Certification Ranks")]
    [Tooltip("Best first. The run gets the first rank whose limits it's within; the last entry is the fallback.")]
    [SerializeField] private List<CertificationRank> certificationRanks = new List<CertificationRank>
    {
        new CertificationRank
        {
            rank = "S", maxLivesLost = 1, maxRetries = 0,
            operatorMessage =
                "Certification complete.\n" +
                "...I checked the data twice, 11S.\n" +
                "I've never filed a log like this one.\n" +
                "You're cleared for deployment to Earth.\n" +
                "I'll be your Operator down there, too.\n" +
                "...If that's all right.\n" +
                "Glory to Mankind."
        },
        new CertificationRank
        {
            rank = "A", maxLivesLost = 3, maxRetries = 0,
            operatorMessage =
                "Certification complete. That was excellent work, 11S.\n" +
                "A few hits, but nothing the engineers will lose sleep over.\n" +
                "You're cleared for deployment to Earth.\n" +
                "I'll be your Operator down there, too.\n" +
                "...If that's all right.\n" +
                "Glory to Mankind."
        },
        new CertificationRank
        {
            rank = "B", maxLivesLost = 8, maxRetries = 2,
            operatorMessage =
                "Certification complete. You took damage, but you adapted.\n" +
                "Down there, that matters more than a clean record.\n" +
                "You're cleared for deployment to Earth.\n" +
                "I'll be your Operator down there, too.\n" +
                "...If that's all right.\n" +
                "Glory to Mankind."
        },
        new CertificationRank
        {
            rank = "C", maxLivesLost = int.MaxValue, maxRetries = int.MaxValue,
            operatorMessage =
                "Certification complete.\n" +
                "...You made it. I wasn't sure for a while.\n" +
                "Please don't put that in your log.\n" +
                "You're cleared for deployment to Earth.\n" +
                "I'll be your Operator down there, too.\n" +
                "...If that's all right.\n" +
                "Glory to Mankind."
        },
    };

    [Header("Testing")]
    [Tooltip("Turn off to skip the title screen and briefing")]
    [SerializeField] private bool showTitleScreen = true;
    [Tooltip("Level to start on from the title screen, e.g. 2 to test SIM 03")]
    [SerializeField] private int firstLevelIndex = 0;

    public GameState State { get; private set; } = GameState.Title;
    public bool IsPlaying => State == GameState.Playing;
    public int Lives { get; private set; }
    public int TotalAliens { get; private set; }
    public int AliensDestroyed { get; private set; }

    // Run-wide totals, across every simulation and retry since the run started
    public int RunLivesLost => runLivesLost;
    public int RunRetries => runRetries;
    public int RunTargetsDestroyed => runTargetsDestroyed;

    public int LevelIndex { get; private set; }
    public int LevelCount => levels.Count;
    public LevelData CurrentLevel => levels.Count > 0 ? levels[LevelIndex] : null;
    public bool IsFinalLevel => LevelIndex >= levels.Count - 1;

    // Raised whenever State changes, so screens, sounds, etc. can react
    public event Action<GameState> StateChanged;

    // Raised whenever Lives changes, e.g. for the lives display and hit flash
    public event Action<int> LivesChanged;

    // Raised each time an alien is destroyed, e.g. to charge the Pod
    public event Action TargetDestroyed;

    // Enter Play Mode Options has domain reload disabled, so static fields keep their
    // values between play sessions unless they are reset here
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        hasPendingStart = false;
        pendingState = GameState.Title;
        pendingLevelIndex = 0;
        ResetRunStats();
    }

    static void ResetRunStats()
    {
        runLivesLost = 0;
        runRetries = 0;
        runTargetsDestroyed = 0;
    }

    void Awake()
    {
        // A GameManager from a scene that's being unloaded may still exist; only a
        // second one in the same scene is a real duplicate
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Debug.LogWarning("More than one GameManager in the scene; destroying the extra one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Every simulation starts with full lives
        Lives = startingLives;

        if (hasPendingStart)
        {
            // Moving to the next level or retrying: same run, so the run stats carry on
            hasPendingStart = false;
            LevelIndex = ClampLevelIndex(pendingLevelIndex);
            State = pendingState;
        }
        else
        {
            // Entering Play mode or returning to the title: a new run
            ResetRunStats();
            LevelIndex = ClampLevelIndex(firstLevelIndex);
            State = showTitleScreen ? GameState.Title : GameState.Playing;
        }

        // Everything is frozen except while playing
        Time.timeScale = IsPlaying ? 1 : 0;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1;
        }
    }

    int ClampLevelIndex(int index)
    {
        return levels.Count == 0 ? 0 : Mathf.Clamp(index, 0, levels.Count - 1);
    }

    // Title screen -> first briefing (or straight into play if there are no levels)
    public void StartFromTitle()
    {
        if (State != GameState.Title)
        {
            return;
        }
        if (CurrentLevel != null)
        {
            SetState(GameState.Briefing);
        }
        else
        {
            BeginPlaying();
        }
    }

    // Briefing -> play
    public void BeginLevel()
    {
        if (State == GameState.Briefing)
        {
            BeginPlaying();
        }
    }

    void BeginPlaying()
    {
        Time.timeScale = 1;
        SetState(GameState.Playing);
    }

    // Debrief -> next level's briefing (with full lives; the unit is repaired)
    public void ContinueToNextLevel()
    {
        if (State == GameState.Debrief)
        {
            LoadScene(GameState.Briefing, LevelIndex + 1);
        }
    }

    // Lost -> replay the same level with full lives
    public void Retry()
    {
        if (State == GameState.Lost)
        {
            runRetries++;
            LoadScene(GameState.Playing, LevelIndex);
        }
    }

    // Certification -> final black screen
    public void ShowEpilogue()
    {
        if (State == GameState.Won)
        {
            SetState(GameState.Epilogue);
        }
    }

    // Epilogue -> title screen, starting over from the first level
    public void ReturnToTitle()
    {
        hasPendingStart = false;
        ReloadScene();
    }

    void LoadScene(GameState state, int levelIndex)
    {
        hasPendingStart = true;
        pendingState = state;
        pendingLevelIndex = levelIndex;
        ReloadScene();
    }

    // The first rank whose limits the run is within; the last rank is the fallback
    public CertificationRank GetCertificationRank()
    {
        foreach (CertificationRank tier in certificationRanks)
        {
            if (runLivesLost <= tier.maxLivesLost && runRetries <= tier.maxRetries)
            {
                return tier;
            }
        }
        return certificationRanks.Count > 0 ? certificationRanks[certificationRanks.Count - 1] : null;
    }

    void ReloadScene()
    {
        Time.timeScale = 1;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(scene.name);
        }
    }

    // Called by AlienFleet after spawning
    public void RegisterAliens(int total)
    {
        TotalAliens = total;
        AliensDestroyed = 0;
    }

    // Called by AlienFleet each time an alien is destroyed
    public void AlienDestroyed()
    {
        AliensDestroyed++;
        runTargetsDestroyed++;
        TargetDestroyed?.Invoke();
    }

    // Called by the Spaceship when an alien bullet hits it (and it isn't invulnerable).
    // Costs a life; losing the last one ends the game.
    public void PlayerHit()
    {
        if (!IsPlaying)
        {
            return;
        }

        Lives = Mathf.Max(Lives - 1, 0);
        runLivesLost++;
        LivesChanged?.Invoke(Lives);

        if (Lives == 0)
        {
            GameOver();
        }
    }

    // Called when the player runs out of lives or the aliens reach the lose line
    public void GameOver()
    {
        EndPlaying(GameState.Lost);
    }

    // Called by AlienFleet when every alien in the level is destroyed
    public void LevelCleared()
    {
        EndPlaying(IsFinalLevel ? GameState.Won : GameState.Debrief);
    }

    void EndPlaying(GameState endState)
    {
        // Ignore repeat calls, e.g. the last alien dying on the same step the fleet reaches the lose line
        if (!IsPlaying)
        {
            return;
        }

        Time.timeScale = 0;
        SetState(endState);
    }

    void SetState(GameState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }
}
