using UnityEngine;
using TMPro;

// Reveals a TextMeshPro text a few characters at a time. Uses unscaled time, because the
// briefing and debrief screens are shown while the game is frozen (timeScale 0).
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 45f;

    private TMP_Text text;
    private float visibleCount;
    private int totalCount;

    public bool IsComplete => text == null || text.maxVisibleCharacters >= totalCount;

    public float CharactersPerSecond
    {
        get => charactersPerSecond;
        set => charactersPerSecond = value;
    }

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public void Play(string content)
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }
        text.text = content;
        // Parse now so characterCount is right (and excludes rich-text tags), even if the object is inactive
        text.ForceMeshUpdate(true, true);
        totalCount = text.textInfo.characterCount;
        visibleCount = 0;
        text.maxVisibleCharacters = 0;
    }

    public void Complete()
    {
        visibleCount = totalCount;
        text.maxVisibleCharacters = totalCount;
    }

    void Update()
    {
        if (IsComplete)
        {
            return;
        }
        visibleCount += charactersPerSecond * Time.unscaledDeltaTime;
        text.maxVisibleCharacters = Mathf.Min(totalCount, (int)visibleCount);
    }
}
