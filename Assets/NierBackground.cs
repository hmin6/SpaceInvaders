using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// NieR hacking-minigame backdrop: replaces the skybox with a flat parchment color and puts a
// subtle grid on a large quad behind the playfield. The grid texture is generated in code.
[RequireComponent(typeof(Camera))]
public class NierBackground : MonoBehaviour
{
    [Tooltip("Unlit material for the grid, e.g. one using Universal Render Pipeline/Particles/Unlit. " +
             "Needed for builds; in the Editor one is created automatically if this is empty.")]
    [SerializeField] private Material material;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = NierStyle.Background;
    [SerializeField] private Color lineColor = NierStyle.Ink;
    [Range(0, 1)]
    [SerializeField] private float minorLineStrength = .08f;
    [Range(0, 1)]
    [SerializeField] private float majorLineStrength = .2f;

    [Header("Grid")]
    [Tooltip("World units per grid cell")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("A slightly darker line every this many cells")]
    [SerializeField] private int majorLineEvery = 5;
    [Tooltip("Distance from the camera to the grid; must be behind the playfield")]
    [SerializeField] private float distance = 30f;

    [Tooltip("The scene's post-processing (tonemapping, bloom, vignette) shifts the palette colors, so turn it off")]
    [SerializeField] private bool disablePostProcessing = true;

    private const int PixelsPerCell = 32;

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;

        if (disablePostProcessing)
        {
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        CreateGrid(cam);
    }

    void CreateGrid(Camera cam)
    {
        Material gridMaterial = CreateMaterial();
        if (gridMaterial == null)
        {
            Debug.LogWarning("NierBackground: no material assigned and no unlit shader found, so there's no grid.", this);
            return;
        }

        // Tall enough to fill the view, and wide enough for any aspect ratio up to 4:1
        float height = cam.orthographic
            ? 2 * cam.orthographicSize
            : 2 * distance * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
        height *= 1.2f;
        float width = height * 4;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "NierGrid";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = new Vector3(0, 0, distance);
        quad.transform.localScale = new Vector3(width, height, 1);

        float tileSize = cellSize * majorLineEvery;
        gridMaterial.mainTexture = CreateGridTexture();
        gridMaterial.mainTextureScale = new Vector2(width / tileSize, height / tileSize);

        MeshRenderer quadRenderer = quad.GetComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = gridMaterial;
        quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
    }

    Material CreateMaterial()
    {
        if (material != null)
        {
            return new Material(material);
        }

        // Shader.Find only works in builds if the shader is included, which is why the material field exists
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        return shader != null ? new Material(shader) : null;
    }

    // One tile = majorLineEvery x majorLineEvery cells, with the major line on its edge
    Texture2D CreateGridTexture()
    {
        int size = PixelsPerCell * majorLineEvery;
        Color32 fill = backgroundColor;
        Color32 minor = Color.Lerp(backgroundColor, lineColor, minorLineStrength);
        Color32 major = Color.Lerp(backgroundColor, lineColor, majorLineStrength);

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 pixel = fill;
                if (x < 2 || y < 2)
                {
                    pixel = major;
                }
                else if (x % PixelsPerCell == 0 || y % PixelsPerCell == 0)
                {
                    pixel = minor;
                }
                pixels[y * size + x] = pixel;
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
