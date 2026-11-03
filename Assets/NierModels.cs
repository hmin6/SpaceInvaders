using System.Collections.Generic;
using UnityEngine;

// Original NieR hacking-minigame-style models, built from primitives and NierShapes meshes at runtime.
// Each Build method hides the prefab's own renderers and adds a "Model" child. Models are authored in
// world units and sized to match the prefabs' colliders. Parts have no colliders.
public static class NierModels
{
    // Arrowhead pointing up, star-shaped around the ship's pivot (counter-clockwise from the nose)
    private static readonly Vector2[] ShipOutline =
    {
        new Vector2(0, 1.05f),       // nose
        new Vector2(-1.25f, -.75f),  // left wing tip
        new Vector2(-.35f, -.42f),   // left notch
        new Vector2(0, -.6f),        // tail
        new Vector2(.35f, -.42f),    // right notch
        new Vector2(1.25f, -.75f),   // right wing tip
    };

    private static readonly Vector2[] Spike = { new Vector2(0, .16f), new Vector2(-.07f, -.06f), new Vector2(.07f, -.06f) };

    // ---------- Player ----------

    // Light arrowhead with a dark outline, edge accents and a pulsing orange core
    public static Transform BuildShip(NierArt art, Transform owner)
    {
        Transform model = CreateRoot(owner);

        Mesh outline = art.GetMesh("ship-outline", () => NierShapes.Prism(NierShapes.Scale(ShipOutline, 1.15f), .2f));
        Mesh body = art.GetMesh("ship-body", () => NierShapes.Prism(ShipOutline, .2f));

        Part(art, model, "Outline", outline, NierStyle.InkDark, new Vector3(0, 0, .08f), 0, Vector3.one);
        Part(art, model, "Body", body, NierStyle.Hull, Vector3.zero, 0, Vector3.one);

        // Spine and wing accents parallel to the leading edges
        Part(art, model, "Spine", art.CubeMesh, NierStyle.InkDark, new Vector3(0, .3f, -.12f), 0, new Vector3(.06f, 1.1f, .02f));
        Part(art, model, "WingLeft", art.CubeMesh, NierStyle.InkDark, new Vector3(-.58f, -.12f, -.12f), 55, new Vector3(.8f, .05f, .02f));
        Part(art, model, "WingRight", art.CubeMesh, NierStyle.InkDark, new Vector3(.58f, -.12f, -.12f), -55, new Vector3(.8f, .05f, .02f));

        // Core: a soft halo that pulses behind a small orange diamond
        Transform halo = Part(art, model, "CoreGlow", art.CubeMesh, NierStyle.Glow, new Vector3(0, -.08f, -.14f), 45, new Vector3(.42f, .42f, .02f));
        halo.gameObject.AddComponent<Pulse>();
        Part(art, model, "Core", art.CubeMesh, NierStyle.Accent, new Vector3(0, -.08f, -.16f), 45, new Vector3(.22f, .22f, .02f));
        return model;
    }

    // Convex collider mesh matching the arrowhead (the convex hull fills the tail notch)
    public static Mesh ShipColliderMesh(NierArt art)
    {
        return art.GetMesh("ship-collider", () => NierShapes.Prism(ShipOutline, 1f));
    }

    // Thin light dart with a dark edge
    public static Transform BuildDart(NierArt art, Transform owner)
    {
        Transform model = CreateRoot(owner);
        Part(art, model, "Edge", art.CubeMesh, NierStyle.InkDark, new Vector3(0, .2f, .05f), 0, new Vector3(.16f, .82f, .02f));
        Part(art, model, "Body", art.CubeMesh, NierStyle.Hull, new Vector3(0, .2f, 0), 0, new Vector3(.08f, .74f, .02f));
        return model;
    }

    // ---------- Enemies ----------

    // shape: 0 = cube with a rotating ring, 1 = octahedron, 2 = spiked shell.
    // anomaly: a corrupted version with a missing or displaced piece (AlienVisual adds the glitching).
    public static Transform BuildAlien(NierArt art, Transform owner, int shape, bool anomaly)
    {
        Transform model = CreateRoot(owner);
        List<Transform> pieces = new List<Transform>(); // parts that can be corrupted

        switch (shape)
        {
            case 0:
            {
                pieces.Add(Part(art, model, "Core", art.CubeMesh, NierStyle.InkDark, Vector3.zero, 0, new Vector3(.46f, .46f, .3f)));
                Transform ring = new GameObject("Ring").transform;
                ring.SetParent(model, false);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .42f;
                    // Rotated to lie along the circle
                    pieces.Add(Part(art, ring, "RingSegment", art.CubeMesh, NierStyle.Ink, position, i * 45f + 90f, new Vector3(.14f, .07f, .1f)));
                }
                ring.gameObject.AddComponent<Spin>().speed = 40f;
                break;
            }
            case 1:
            {
                Mesh octahedron = art.GetMesh("octahedron", () => NierShapes.Octahedron(.75f, .92f, .35f));
                Transform body = Part(art, model, "Octahedron", octahedron, NierStyle.InkDark, Vector3.zero, 0, Vector3.one);
                Spin turn = body.gameObject.AddComponent<Spin>();
                turn.axis = Vector3.up;
                turn.speed = 70f;
                pieces.Add(body);
                // In front of the spinning octahedron's nearest point (z -.375), behind the eye
                pieces.Add(Part(art, model, "Band", art.CubeMesh, NierStyle.Ink, new Vector3(0, 0, -.42f), 0, new Vector3(.5f, .05f, .02f)));
                break;
            }
            default:
            {
                Transform shell = new GameObject("Shell").transform;
                shell.SetParent(model, false);
                Mesh octagon = art.GetMesh("octagon", () => NierShapes.Prism(NierShapes.RegularPolygon(8, .3f, 22.5f), .3f));
                Mesh spike = art.GetMesh("spike", () => NierShapes.Prism(Spike, .2f));
                pieces.Add(Part(art, shell, "Core", octagon, NierStyle.InkDark, Vector3.zero, 0, Vector3.one));
                for (int i = 0; i < 8; i++)
                {
                    float degrees = i * 45f;
                    float angle = (degrees + 90f) * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .36f;
                    pieces.Add(Part(art, shell, "Spike", spike, NierStyle.Ink, position, degrees, Vector3.one));
                }
                shell.gameObject.AddComponent<Spin>().speed = -25f;
                break;
            }
        }

        // Orange eye in front of everything
        Part(art, model, "Eye", art.CubeMesh, NierStyle.Accent, new Vector3(0, 0, -.45f), 45, new Vector3(.14f, .14f, .02f));

        if (anomaly)
        {
            Corrupt(pieces);
        }
        model.gameObject.AddComponent<AlienVisual>().Initialize(anomaly);
        return model;
    }

    // Knock one piece out of place and (usually) remove another
    static void Corrupt(List<Transform> pieces)
    {
        if (pieces.Count < 2)
        {
            return;
        }
        Transform displaced = pieces[Random.Range(0, pieces.Count)];
        displaced.localPosition += new Vector3(Random.Range(-.15f, .15f), Random.Range(-.1f, .1f), 0);

        if (Random.value < .7f)
        {
            Transform missing = pieces[Random.Range(0, pieces.Count)];
            if (missing != displaced)
            {
                missing.gameObject.SetActive(false);
            }
        }
    }

    // Orange shots are destructible; dark ones (a dark core with a thin orange outline) are not
    public static Transform BuildAlienBullet(NierArt art, Transform owner, bool dark)
    {
        Transform model = CreateRoot(owner);
        // Spin an inner pivot, not the model root: the bullet prefab is scaled unevenly, so rotating
        // the scale-cancelling root would shear the shape
        Transform spinner = new GameObject("Spinner").transform;
        spinner.SetParent(model, false);
        if (dark)
        {
            Part(art, spinner, "Outline", art.CubeMesh, NierStyle.Accent, new Vector3(0, 0, .03f), 45, new Vector3(.36f, .36f, .02f));
            Part(art, spinner, "Core", art.CubeMesh, NierStyle.InkDark, Vector3.zero, 45, new Vector3(.27f, .27f, .02f));
            spinner.gameObject.AddComponent<Spin>().speed = 160f;
        }
        else
        {
            Part(art, spinner, "Body", art.CubeMesh, NierStyle.Accent, Vector3.zero, 45, new Vector3(.3f, .3f, .02f));
            Part(art, spinner, "Center", art.CubeMesh, NierStyle.Paper, new Vector3(0, 0, -.03f), 45, new Vector3(.1f, .1f, .02f));
            spinner.gameObject.AddComponent<Spin>().speed = 360f;
        }
        return model;
    }

    // ---------- Helpers ----------

    // Disables every renderer on the prefab, so only the code-built model shows. Call before building.
    public static void HideOriginalRenderers(GameObject owner)
    {
        foreach (Renderer rend in owner.GetComponentsInChildren<Renderer>())
        {
            rend.enabled = false;
        }
    }

    // A child that cancels the owner's scale, so models are authored in world units
    static Transform CreateRoot(Transform owner)
    {
        Transform root = new GameObject("Model").transform;
        root.gameObject.layer = owner.gameObject.layer;
        root.SetParent(owner, false);
        Vector3 scale = owner.lossyScale;
        root.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        return root;
    }

    static Transform Part(NierArt art, Transform parent, string name, Mesh mesh, Color color,
        Vector3 position, float rotationZ, Vector3 scale)
    {
        GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(0, 0, rotationZ);
        part.transform.localScale = scale;

        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
        partRenderer.sharedMaterial = art.GetMaterial(color);
        partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        return part.transform;
    }
}
