using UnityEngine;
using TMPro;

// Shows the player's lives as text. It only sets the string, so font, colour, size and
// position can all be restyled in the Inspector. An icon-based display could replace it
// by listening to the same GameManager.LivesChanged event.
[RequireComponent(typeof(TextMeshProUGUI))]
public class LivesDisplay : MonoBehaviour
{
    [Tooltip("{0} is replaced with the number of lives")]
    [SerializeField] private string format = "LIVES: {0}";

    private TextMeshProUGUI uiText;
    private GameManager gameManager;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
    }

    // Start (not Awake/OnEnable) so GameManager.Awake has already set Instance and Lives
    void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("LivesDisplay: no GameManager in the scene.", this);
            return;
        }
        gameManager.LivesChanged += Show;
        Show(gameManager.Lives);
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.LivesChanged -= Show;
        }
    }

    void Show(int lives)
    {
        uiText.text = string.Format(format, lives);
    }
}
