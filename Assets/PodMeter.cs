using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD meter for the Pod, built at runtime under this Canvas (bottom-left): a "POD PROGRAM: LASER"
// label, a segmented charge bar that pulses orange when ready, and a short system message.
[RequireComponent(typeof(Canvas))]
public class PodMeter : MonoBehaviour
{
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private Pod pod;
    [Tooltip("Optional. Uses the TextMesh Pro default font if empty.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Text")]
    [SerializeField] private string label = "Pod Program: Laser";
    [SerializeField] private string keyHint = "[ E ]";
    [SerializeField] private string readyMessage = "Proposal: Pod Program available.";

    [Header("Layout")]
    [Tooltip("Bottom-left corner of the meter, in reference pixels (1920 x 1080) from the screen's bottom-left")]
    [SerializeField] private Vector2 position = new Vector2(40, 24);
    [SerializeField] private float barWidth = 420f;
    [SerializeField] private float segmentGap = 6f;

    [Header("Message Timing")]
    [SerializeField] private float messageFadeIn = .2f;
    [SerializeField] private float messageHold = 2.5f;
    [SerializeField] private float messageFadeOut = .6f;

    private TextMeshProUGUI labelText;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI messageText;
    private RectTransform barRoot;
    private Image[] segments = new Image[0];
    private float messageStartTime = -100f;

    private static readonly Color EmptySegment = NierStyle.WithAlpha(NierStyle.Ink, .25f);

    void Awake()
    {
        RectTransform root = NierStyle.CreateRect("PodMeter", transform);
        NierStyle.Stretch(root);
        // First child, so the start/end screens (built later by NierScreens) draw over it
        root.SetAsFirstSibling();

        Vector2 bottomLeft = Vector2.zero;

        messageText = NierStyle.CreateText("Message", root, readyMessage, 22, NierStyle.Ink,
            TextAlignmentOptions.BottomLeft, font, 6);
        NierStyle.Place(messageText.rectTransform, bottomLeft, position + new Vector2(0, 84), new Vector2(800, 34));
        messageText.alpha = 0;

        labelText = NierStyle.CreateText("Label", root, label, 22, NierStyle.InkDark,
            TextAlignmentOptions.BottomLeft, font, 8);
        NierStyle.Place(labelText.rectTransform, bottomLeft, position + new Vector2(0, 36), new Vector2(barWidth, 32));

        keyText = NierStyle.CreateText("Key", root, keyHint, 18, NierStyle.Ink,
            TextAlignmentOptions.BottomRight, font, 4);
        NierStyle.Place(keyText.rectTransform, bottomLeft, position + new Vector2(0, 36), new Vector2(barWidth, 32));

        barRoot = NierStyle.CreateRect("Bar", root);
        NierStyle.Place(barRoot, bottomLeft, position + new Vector2(0, 12), new Vector2(barWidth, 16));

        Image underline = NierStyle.CreateImage("Underline", root, NierStyle.Ink);
        NierStyle.Place(underline.rectTransform, bottomLeft, position, new Vector2(barWidth, 2));
    }

    void Start()
    {
        if (pod == null)
        {
            pod = FindAnyObjectByType<Pod>();
        }
        if (pod == null)
        {
            Debug.LogWarning("PodMeter: no Pod in the scene.", this);
            enabled = false;
            return;
        }

        BuildSegments(pod.KillsToCharge);
        pod.ChargeChanged += OnChargeChanged;
        pod.Ready += OnReady;
        OnChargeChanged(pod.Charge, pod.KillsToCharge);
    }

    void OnDestroy()
    {
        if (pod != null)
        {
            pod.ChargeChanged -= OnChargeChanged;
            pod.Ready -= OnReady;
        }
    }

    void BuildSegments(int count)
    {
        count = Mathf.Max(1, count);
        float width = (barWidth - segmentGap * (count - 1)) / count;
        segments = new Image[count];
        for (int i = 0; i < count; i++)
        {
            segments[i] = NierStyle.CreateImage("Segment", barRoot, EmptySegment);
            NierStyle.Place(segments[i].rectTransform, Vector2.zero, new Vector2(i * (width + segmentGap), 0),
                new Vector2(width, 16));
        }
    }

    void OnChargeChanged(int charge, int max)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].color = i < charge ? NierStyle.InkDark : EmptySegment;
        }
    }

    void OnReady()
    {
        messageStartTime = Time.unscaledTime;
    }

    void Update()
    {
        // Pulse the label, key hint and segments orange while the laser is ready
        if (pod != null && pod.IsReady)
        {
            float pulse = .55f + .45f * Mathf.Sin(Time.unscaledTime * 6f);
            Color ready = NierStyle.WithAlpha(NierStyle.Accent, pulse);
            foreach (Image segment in segments)
            {
                segment.color = ready;
            }
            labelText.color = Color.Lerp(NierStyle.InkDark, NierStyle.Accent, pulse);
            keyText.color = labelText.color;
        }
        else
        {
            labelText.color = NierStyle.InkDark;
            keyText.color = NierStyle.Ink;
        }

        // Unscaled, so the message still fades if a screen freezes the game
        float t = Time.unscaledTime - messageStartTime;
        float alpha = t < messageFadeIn ? t / messageFadeIn
            : t < messageFadeIn + messageHold ? 1f
            : 1f - (t - messageFadeIn - messageHold) / messageFadeOut;
        messageText.alpha = Mathf.Clamp01(alpha);
    }
}
