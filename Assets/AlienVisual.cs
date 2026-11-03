using UnityEngine;

// Idle animation for an alien's code-built model: a gentle bob, plus, on anomaly levels, short
// glitch bursts where the model jitters and flickers. Only moves the model, never the alien itself,
// so collisions and the lose line are unaffected.
public class AlienVisual : MonoBehaviour
{
    [SerializeField] private float bobHeight = .05f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Anomaly glitch")]
    [SerializeField] private float glitchJitter = .12f;
    [SerializeField] private Vector2 glitchLength = new Vector2(.06f, .18f);
    [SerializeField] private Vector2 glitchGap = new Vector2(.5f, 2f);

    private bool anomaly;
    private float phase;
    private Vector3 restPosition;
    private Renderer[] renderers;
    private bool visible = true;

    private float glitchEndTime;
    private float nextGlitchTime;
    private bool glitchFlickers;

    public void Initialize(bool isAnomaly)
    {
        anomaly = isAnomaly;
        phase = Random.Range(0f, Mathf.PI * 2);
        restPosition = transform.localPosition;
        renderers = GetComponentsInChildren<Renderer>();
        // Desynchronize the aliens so they don't all glitch together
        nextGlitchTime = Time.time + Random.Range(0f, glitchGap.y);
    }

    void Update()
    {
        Vector3 position = restPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
        if (anomaly)
        {
            position += GlitchOffset();
        }
        transform.localPosition = position;
    }

    Vector3 GlitchOffset()
    {
        float now = Time.time;
        if (now >= glitchEndTime && now >= nextGlitchTime)
        {
            glitchEndTime = now + Random.Range(glitchLength.x, glitchLength.y);
            nextGlitchTime = glitchEndTime + Random.Range(glitchGap.x, glitchGap.y);
            glitchFlickers = Random.value < .5f;
        }

        bool glitching = now < glitchEndTime;
        SetVisible(!(glitching && glitchFlickers && Time.frameCount % 2 == 0));
        return glitching ? (Vector3)(Random.insideUnitCircle * glitchJitter) : Vector3.zero;
    }

    void SetVisible(bool show)
    {
        if (show == visible)
        {
            return;
        }
        visible = show;
        foreach (Renderer rend in renderers)
        {
            rend.enabled = show;
        }
    }
}
