using UnityEngine;
using TMPro;

public class AlienCounter : MonoBehaviour
{
    [Tooltip("{0} is replaced with the number of aliens left")]
    [SerializeField] private string format = "Targets Remaining: {0}";

    private TextMeshProUGUI uiText;

    // Awake (not Start) so uiText is ready before AlienFleet.Start calls SetRemaining
    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
    }

    // Called by AlienFleet only when the alien count changes, instead of rebuilding the string every frame
    public void SetRemaining(int remaining)
    {
        // Fallback in case this runs before Awake (e.g. if this GameObject starts inactive)
        if (uiText == null)
        {
            uiText = GetComponent<TextMeshProUGUI>();
        }
        uiText.text = string.Format(format, remaining);
    }
}
