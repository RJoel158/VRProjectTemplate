#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool: Tools > Shooting Range > Build Scene
/// Clears old shooting range objects and builds a clean one from scratch.
/// Keeps the Environment hierarchy untouched.
/// </summary>
public static class ShootingRangeBuilder
{
    // ─── Lane Layout ──────────────────────────────────────────────────────────

    // Each entry: (distance from player, height above floor, X offset from center)
    private static readonly (float dist, float height, float xOffset, string label)[] Lanes =
    {
        (10f,  1.4f, -1.5f, "Target_10m"),
        (25f,  1.4f, -1.5f, "Target_25m"),
        (50f,  1.4f, -1.5f, "Target_50m"),
    };

    // Target face sizes scale with distance so they're visible
    private static readonly float[] TargetSizes = { 0.40f, 0.55f, 0.80f };

    // ─── Menu Items ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Shooting Range/▶  Build Scene", false, 10)]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Rebuild Shooting Range",
            "This will DELETE the existing ShootingRange object and rebuild it.\n\nThe Environment will NOT be touched.\n\nContinue?",
            "Build", "Cancel"))
            return;

        // ── 1. Remove old ShootingRange root ──────────────────────────────────
        GameObject oldRoot = GameObject.Find("ShootingRange");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        // ── 2. Create new root ────────────────────────────────────────────────
        GameObject root = new GameObject("ShootingRange");
        Undo.RegisterCreatedObjectUndo(root, "Create ShootingRange");

        // ── 3. Attach ShootingRangeManager ────────────────────────────────────
        ShootingRangeManager manager = root.AddComponent<ShootingRangeManager>();

        // ── 4. Build targets ──────────────────────────────────────────────────
        List<ShootingTarget> builtTargets = new List<ShootingTarget>();

        for (int i = 0; i < Lanes.Length; i++)
        {
            var (dist, height, xOff, label) = Lanes[i];
            float size = TargetSizes[i];

            ShootingTarget tgt = BuildTarget(root.transform, label, dist, height, xOff, size);
            builtTargets.Add(tgt);
        }

        manager.targets = builtTargets;

        // ── 5. Select the new root ────────────────────────────────────────────
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log($"[ShootingRangeBuilder] ✅ Built {builtTargets.Count} targets in ShootingRange.");
        EditorUtility.DisplayDialog("Done!", $"ShootingRange built with {builtTargets.Count} targets.\n\n" +
            "Press Play and use Mouse to aim, Left Click to shoot!", "OK");
    }

    [MenuItem("Tools/Shooting Range/✕  Remove ShootingRange", false, 20)]
    public static void RemoveShootingRange()
    {
        GameObject old = GameObject.Find("ShootingRange");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[ShootingRangeBuilder] ShootingRange removed.");
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found", "No ShootingRange object in the scene.", "OK");
        }
    }

    // ─── Target Builder ───────────────────────────────────────────────────────

    private static ShootingTarget BuildTarget(
        Transform parent, string label,
        float distZ, float height, float xOffset, float faceSize)
    {
        // Container
        GameObject container = new GameObject(label);
        Undo.RegisterCreatedObjectUndo(container, "Create Target");
        container.transform.SetParent(parent, false);
        container.transform.localPosition = new Vector3(xOffset, height, distZ);
        // Face the player (rotate 180 on Y so the front faces -Z toward origin)
        container.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // ── Stand / pole ──────────────────────────────────────────────────────
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(pole, "Create Pole");
        pole.name = "Pole";
        pole.transform.SetParent(container.transform, false);
        pole.transform.localPosition = new Vector3(0f, -height * 0.5f, 0f);
        pole.transform.localScale    = new Vector3(0.04f, height * 0.5f, 0.04f);
        ApplyColor(pole, new Color(0.35f, 0.28f, 0.22f));

        // ── Target face (Quad) ────────────────────────────────────────────────
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(face, "Create Face");
        face.name = "Face";
        face.transform.SetParent(container.transform, false);
        face.transform.localPosition = Vector3.zero;
        face.transform.localScale    = new Vector3(faceSize, faceSize, 1f);
        // Quad forward = +Z by default; we want it to face -Z of container = toward player
        face.transform.localRotation = Quaternion.identity;
        ApplyTargetMaterial(face, faceSize);

        // ── Collider on container ─────────────────────────────────────────────
        BoxCollider col = container.AddComponent<BoxCollider>();
        col.size   = new Vector3(faceSize, faceSize, 0.05f);
        col.center = Vector3.zero;

        // ── ShootingTarget script ─────────────────────────────────────────────
        ShootingTarget st = container.AddComponent<ShootingTarget>();
        st.targetLabel     = label;
        st.targetRadius    = faceSize * 0.5f;
        st.bullseyeRadius  = faceSize * 0.08f;

        // Store MeshRenderer reference on the face for flash effect
        // (ShootingTarget uses GetComponentInChildren)

        return st;
    }

    // ─── Material Helpers ─────────────────────────────────────────────────────

    private static void ApplyColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = color;
        r.sharedMaterial = m;
    }

    private static void ApplyTargetMaterial(GameObject face, float faceSize)
    {
        Renderer r = face.GetComponent<Renderer>();
        if (r == null) return;

        // Generate a procedural ring texture
        Texture2D tex = GenerateTargetTexture(512);
        tex.name = "TargetTex_" + face.transform.parent.name;

        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.mainTexture = tex;
        m.color       = Color.white;
        r.sharedMaterial = m;
    }

    /// <summary>Generates a white paper target texture with classic concentric rings.</summary>
    private static Texture2D GenerateTargetTexture(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Color[] px    = new Color[res * res];

        // Ring colors from outer to inner
        // Zones: 2=white, 4=white, 6=black, 8=black, 10=yellow
        Color[] ringColors = {
            Color.white,                         // 2 – outer (white)
            Color.white,                         // 4 – white
            new Color(0.15f, 0.15f, 0.15f),      // 6 – dark
            new Color(0.15f, 0.15f, 0.15f),      // 8 – dark
            new Color(1f, 0.92f, 0f),            // 10 – gold bullseye
        };

        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float   maxR   = res * 0.48f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t    = dist / maxR;          // 0 = center, 1 = outer edge

                if (t > 1f)
                {
                    px[y * res + x] = new Color(0, 0, 0, 0); // transparent outside circle
                    continue;
                }

                // Pick ring color based on normalized distance
                int ring = Mathf.Clamp(Mathf.FloorToInt(t * ringColors.Length), 0, ringColors.Length - 1);
                // Invert: ring 0 = center (bullseye), we want center = innermost color
                ring = ringColors.Length - 1 - ring;
                Color c = ringColors[ring];

                // Draw thin black division lines between rings
                float ringEdge = (Mathf.FloorToInt(t * ringColors.Length)) / (float)ringColors.Length;
                float edgeDist = Mathf.Abs(t - ringEdge) * maxR;
                if (edgeDist < 1.5f)
                    c = Color.Lerp(c, Color.black, 1f - (edgeDist / 1.5f));

                px[y * res + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
#endif
