using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Restyles an existing TextMeshPro text (e.g. the HUD) to the NieR look when the game starts:
// palette color, uppercase, letter spacing, and an optional thin underline.
[RequireComponent(typeof(TMP_Text))]
public class NierTextStyle : MonoBehaviour
{
    [SerializeField] private Color color = NierStyle.InkDark;
    [SerializeField] private float letterSpacing = NierStyle.LetterSpacing;
    [Tooltip("0 keeps the size set on the text")]
    [SerializeField] private float fontSize = 28f;
    [SerializeField] private bool underline = true;

    void Awake()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        NierStyle.ApplyText(text, color, letterSpacing);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (fontSize > 0)
        {
            text.fontSize = fontSize;
        }

        if (underline)
        {
            // Full width of the text's box, just below it
            Image line = NierStyle.CreateImage("Underline", transform, NierStyle.Ink);
            RectTransform rt = line.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(.5f, 1);
            rt.anchoredPosition = new Vector2(0, -4);
            rt.sizeDelta = new Vector2(0, 2);
        }
    }
}
