using System;
using UnityEngine;
using TMPro;

// Owns the game's flow. Other scripts report what happened (player hit, fleet cleared,
// aliens landed) and GameManager decides the outcome, so lives, levels and start/end
// screens can be added here later without changing the gameplay scripts.
public class GameManager : MonoBehaviour
{
    // Add states like Title, Paused or LevelComplete here as screens are added
    public enum GameState { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    [Header("Inscribed")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string winMessage = "YOU WIN";
    [SerializeField] private string gameOverMessage = "GAME OVER";

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    public int Lives { get; private set; }

    // Raised whenever State changes, so end screens, sounds, etc. can react
    public event Action<GameState> StateChanged;

    // Raised whenever Lives changes, e.g. for the lives display
    public event Action<int> LivesChanged;

    // Enter Play Mode Options has domain reload disabled, so static fields keep their
    // values between play sessions unless they are reset here
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one GameManager in the scene; destroying the extra one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Lives = startingLives;
        Time.timeScale = 1;
        if (messageText != null)
        {
            messageText.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1;
        }
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
        EndGame(GameState.Lost, gameOverMessage);
    }

    public void Win()
    {
        EndGame(GameState.Won, winMessage);
    }

    void EndGame(GameState endState, string message)
    {
        // Ignore repeat calls, e.g. the last alien dying on the same step the fleet reaches the lose line
        if (!IsPlaying)
        {
            return;
        }

        Time.timeScale = 0;
        ShowMessage(message);
        SetState(endState);
    }

    void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning("GameManager has no Message Text assigned; can't show \"" + message + "\".", this);
            return;
        }
        messageText.text = message;
        messageText.enabled = true;
    }

    void SetState(GameState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }
}
