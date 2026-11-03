using System;
using System.Collections.Generic;
using UnityEngine;

// Shared resources for the code-built models (NierModels): one unlit material per palette color
// and cached meshes. Put it in the scene with the Nier_Unlit material assigned. Without it, the
// ship, aliens, bullets and darts keep their original prefab visuals.
// Runs before other scripts so the ship can build its model in Awake.
[DefaultExecutionOrder(-200)]
public class NierArt : MonoBehaviour
{
    public static NierArt Instance { get; private set; }

    [Tooltip("Unlit material the models are colored from, e.g. Nier_Unlit (URP Particles/Unlit). " +
             "Needed for builds; in the Editor one is created automatically if empty.")]
    [SerializeField] private Material baseMaterial;

    private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

    public Mesh CubeMesh { get; private set; }
    public Mesh SphereMesh { get; private set; }

    // Enter Play Mode Options has domain reload disabled, so reset the static here
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Debug.LogWarning("More than one NierArt in the scene; destroying the extra one.", this);
            Destroy(this);
            return;
        }

        if (baseMaterial == null)
        {
            // Shader.Find only works in builds if the shader is included, which is why the field exists
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("NierArt: no material assigned and the URP unlit shader wasn't found; keeping the original visuals.", this);
                return;
            }
            baseMaterial = new Material(shader);
            created.Add(baseMaterial);
        }

        // Unity's built-in primitive meshes (assets, so they're never destroyed here)
        CubeMesh = TakePrimitiveMesh(PrimitiveType.Cube);
        SphereMesh = TakePrimitiveMesh(PrimitiveType.Sphere);

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // Materials and meshes made at runtime aren't cleaned up by scene unloads
        foreach (UnityEngine.Object obj in created)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    static Mesh TakePrimitiveMesh(PrimitiveType type)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        Mesh mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(primitive);
        return mesh;
    }

    // Unlit material in a solid color, shared by every part with that color
    public Material GetMaterial(Color color)
    {
        if (!materials.TryGetValue(color, out Material material))
        {
            material = new Material(baseMaterial);
            material.color = color;
            material.SetColor("_BaseColor", color); // URP's name for the main color
            materials[color] = material;
            created.Add(material);
        }
        return material;
    }

    // Builds a mesh once per key and reuses it
    public Mesh GetMesh(string key, Func<Mesh> build)
    {
        if (!meshes.TryGetValue(key, out Mesh mesh))
        {
            mesh = build();
            mesh.name = key;
            meshes[key] = mesh;
            created.Add(mesh);
        }
        return mesh;
    }
}
