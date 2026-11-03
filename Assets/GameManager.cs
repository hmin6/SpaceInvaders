using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Owns the game's flow. Other scripts report what happened (player hit, alien destroyed,
// fleet cleared, aliens landed) and GameManager decides the outcome. UI such as NierScreens
// and LivesDisplay only listens to its events, so screens, lives and levels stay in one place.
public class GameManager : MonoBehaviour
{
    // Add states like Paused or LevelComplete here as screens are added
    public enum GameState { Title, Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    // Set by Retry() so the reloaded scene skips the title screen. Static so it survives the reload.
    private static bool skipTitleOnLoad;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;

    [Header("Flow")]
    [Tooltip("Turn off to skip the start screen while testing")]
    [SerializeField] private bool showTitleScreen = true;

    public GameState State { get; private set; } = GameState.Title;
    public bool IsPlaying => State == GameState.Playing;
    public int Lives { get; private set; }
    public int TotalAliens { get; private set; }
    public int AliensDestroyed { get; private set; }

    // Raised whenever State changes, so screens, sounds, etc. can react
    public event Action<GameState> StateChanged;

    // Raised whenever Lives changes, e.g. for the lives display and hit flash
    public event Action<int> LivesChanged;

    // Enter Play Mode Options has domain reload disabled, so static fields keep their
    // values between play sessions unless they are reset here
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        skipTitleOnLoad = false;
    }

    void Awake()
    {
        // A GameManager from a scene that's being unloaded (on Retry) may still exist; only a
        // second one in the same scene is a real duplicate
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Debug.LogWarning("More than one GameManager in the scene; destroying the extra one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Lives = startingLives;
        bool skipTitle = skipTitleOnLoad || !showTitleScreen;
        skipTitleOnLoad = false;

        // The game is frozen on the title screen until the player starts it
        State = skipTitle ? GameState.Playing : GameState.Title;
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

    // Menu input runs in LateUpdate, after Spaceship.Update, so the Space press that
    // starts the game doesn't also fire a dart
    void LateUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool enter = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
        if (State == GameState.Title && (enter || keyboard.spaceKey.wasPressedThisFrame))
        {
            StartGame();
        }
        else if ((State == GameState.Won || State == GameState.Lost) && (enter || keyboard.rKey.wasPressedThisFrame))
        {
            Retry();
        }
    }

    public void StartGame()
    {
        if (State != GameState.Title)
        {
            return;
        }
        Time.timeScale = 1;
        SetState(GameState.Playing);
    }

    // Reloads the scene, which recreates every object. Statics are the only thing that carry over,
    // so skipTitleOnLoad is set here and Awake resets Time.timeScale, Lives and State.
    public void Retry()
    {
        skipTitleOnLoad = true;
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
        LivesChanged?.Invoke(Lives);

        if (Lives == 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        EndGame(GameState.Lost);
    }

    public void Win()
    {
        EndGame(GameState.Won);
    }

    void EndGame(GameState endState)
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
