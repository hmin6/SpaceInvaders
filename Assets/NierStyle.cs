using UnityEngine;
using UnityEngine.UI;
using TMPro;

// NieR: Automata menu palette, plus small helpers for building UI from code.
// The colors are static readonly and never reassigned, so they're safe with domain reload disabled.
public static class NierStyle
{
    public static readonly Color Background = new Color32(0xDA, 0xD4, 0xBB, 0xFF); // parchment
    public static readonly Color BackgroundDark = new Color32(0xCD, 0xC8, 0xB0, 0xFF);
    public static readonly Color Ink = new Color32(0x4E, 0x4B, 0x42, 0xFF);        // text and lines
    public static readonly Color InkDark = new Color32(0x3A, 0x38, 0x30, 0xFF);
    public static readonly Color Accent = new Color32(0xE0, 0x6A, 0x3A, 0xFF);     // enemy bullets and hits
    public static readonly Color Paper = new Color32(0xF4, 0xF1, 0xE6, 0xFF);      // light flash

    public const float LetterSpacing = 12f;

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // Uppercase, letter-spaced, palette-colored text
    public static void ApplyText(TMP_Text text, Color color, float letterSpacing = LetterSpacing, bool uppercase = true)
    {
        text.color = color;
        text.characterSpacing = letterSpacing;
        if (uppercase)
        {
            text.fontStyle |= FontStyles.UpperCase;
        }
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Fill the parent, optionally inset from every edge
    public static void Stretch(RectTransform rt, float inset = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    // Anchor (and pivot) at a point of the parent, e.g. (0.5, 0.5) = center, (1, 1) = top-right
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, Color color,
        TextAlignmentOptions alignment, TMP_FontAsset font, float letterSpacing = LetterSpacing, bool uppercase = true)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        ApplyText(text, color, letterSpacing, uppercase);
        return text;
    }

    // Thin rectangular outline around the parent, made of four Images
    public static void CreateFrame(Transform parent, Color color, float thickness)
    {
        CreateEdge(parent, color, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, thickness)); // top
        CreateEdge(parent, color, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, thickness)); // bottom
        CreateEdge(parent, color, new Vector2(0, 0), new Vector2(0, 1), new Vector2(thickness, 0)); // left
        CreateEdge(parent, color, new Vector2(1, 0), new Vector2(1, 1), new Vector2(thickness, 0)); // right
    }

    static void CreateEdge(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        RectTransform rt = CreateImage("Edge", parent, color).rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = (anchorMin + anchorMax) / 2;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    // NieR-style highlighted menu item: a dark bar with light text and a small square marker to its left.
    // Returns the marker so the caller can blink it.
    public static Image CreateMenuItem(Transform parent, string label, Vector2 position, TMP_FontAsset font)
    {
        return CreateMenuItem(parent, label, position, font, out _);
    }

    // Same, also returning the label so its text can be changed later
    public static Image CreateMenuItem(Transform parent, string label, Vector2 position, TMP_FontAsset font,
        out TextMeshProUGUI text)
    {
        Vector2 barSize = new Vector2(520, 72);
        Vector2 center = new Vector2(.5f, .5f);

        Image bar = CreateImage("MenuItem", parent, InkDark);
        Place(bar.rectTransform, center, position, barSize);

        text = CreateText("Label", bar.transform, label, 38, Background,
            TextAlignmentOptions.Center, font, 16);
        Stretch(text.rectTransform);

        Image marker = CreateImage("Marker", parent, InkDark);
        Place(marker.rectTransform, center, position + new Vector2(-barSize.x / 2 - 34, 0), new Vector2(18, 18));
        return marker;
    }
}
