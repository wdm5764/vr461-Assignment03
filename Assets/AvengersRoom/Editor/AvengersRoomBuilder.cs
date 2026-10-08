using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Builds a subtle Avengers-themed room around a stationary VR player standing at the world origin,
// facing +Z. Run from the menu: Avengers > Build Room. Running it again replaces the old room.
//
// Layout (meters, floor at y = 0):
//   Player spot ...... (0, 0, 0)
//   Assembly table ... centered at z = 0.75, top surface at y = 0.90
//   Back wall ........ Avengers logo + skyline windows
//   Left wall ........ Captain America shield emblem
//   Right wall ....... Arc reactor emblem
public static class AvengersRoomBuilder
{
    const string RootName = "AvengersRoom";
    const string MaterialFolder = "Assets/AvengersRoom/Materials";
    const string MeshFolder = "Assets/AvengersRoom/Meshes";

    // Room bounds
    const float RoomHalfWidth = 3f;
    const float RoomBack = 3.5f;
    const float RoomFront = -2.5f;
    const float RoomHeight = 3.2f;

    // Table
    public const float TableTopY = 0.90f;
    const float TableZ = 0.75f;
    const float TableWidth = 2.2f;
    const float TableDepth = 0.9f;
    const float TableThickness = 0.05f;

    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    [MenuItem("Avengers/Build Room")]
    public static void BuildRoom()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        CreateMaterials();

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Avengers Room");

        BuildShell(root.transform);
        BuildTable(root.transform);
        BuildBackWall(root.transform);
        BuildSideWalls(root.transform);
        BuildPlayerSpot(root.transform);
        BuildLighting(root.transform);
        PlaceSampleGrabbable();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Avengers room built. Puzzle anchors are under AvengersRoom/AssemblyTable/Stations.");
    }

    // ------------------------------------------------------------------ Materials

    static void CreateMaterials()
    {
        Mats.Clear();
        EnsureFolder(MaterialFolder);

        Lit("Floor", new Color(0.10f, 0.11f, 0.13f), 0.6f, 0.75f);
        Lit("Wall", new Color(0.17f, 0.18f, 0.21f), 0.2f, 0.35f);
        Lit("WallPanel", new Color(0.12f, 0.13f, 0.16f), 0.5f, 0.55f);
        Lit("Ceiling", new Color(0.08f, 0.08f, 0.10f), 0f, 0.2f);
        Lit("DarkMetal", new Color(0.07f, 0.07f, 0.08f), 0.9f, 0.7f);
        Lit("BrushedSteel", new Color(0.55f, 0.57f, 0.60f), 1f, 0.6f);
        Lit("TableTop", new Color(0.05f, 0.06f, 0.07f), 0.4f, 0.85f);
        Lit("PartsTray", new Color(0.13f, 0.14f, 0.17f), 0.2f, 0.4f);
        Lit("Building", new Color(0.02f, 0.02f, 0.03f), 0f, 0.1f);
        Lit("CapRed", new Color(0.62f, 0.07f, 0.09f), 0.6f, 0.6f);
        Lit("CapWhite", new Color(0.85f, 0.85f, 0.85f), 0.6f, 0.6f);
        Lit("CapBlue", new Color(0.06f, 0.12f, 0.38f), 0.6f, 0.6f);
        Lit("Gold", new Color(0.83f, 0.62f, 0.22f), 1f, 0.7f);

        Emissive("GlowBlue", new Color(0.25f, 0.65f, 1f), 2.0f);
        Emissive("GlowSoftBlue", new Color(0.25f, 0.65f, 1f), 0.8f);
        Emissive("GlowRed", new Color(1f, 0.18f, 0.12f), 1.2f);
        Emissive("GlowGold", new Color(1f, 0.72f, 0.3f), 1.0f);
        Emissive("GlowWhite", new Color(1f, 0.97f, 0.92f), 1.5f);
        Emissive("ArcCore", new Color(0.6f, 0.95f, 1f), 3.0f);
        Emissive("NightSky", new Color(0.03f, 0.06f, 0.14f), 1.0f);
        Emissive("CityLights", new Color(1f, 0.82f, 0.5f), 1.2f);
        Emissive("Logo", new Color(0.85f, 0.88f, 0.95f), 0.9f);

        AssetDatabase.SaveAssets();
    }

    static Shader LitShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    static Material GetOrCreate(string name)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(LitShader());
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = LitShader();
        }
        Mats[name] = mat;
        return mat;
    }

    static void Lit(string name, Color color, float metallic, float smoothness)
    {
        var mat = GetOrCreate(name);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Glossiness", smoothness);
        mat.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(mat);
    }

    static void Emissive(string name, Color color, float intensity)
    {
        var mat = GetOrCreate(name);
        mat.SetColor("_BaseColor", color * 0.5f);
        mat.SetColor("_Color", color * 0.5f);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.5f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
    }

    // ------------------------------------------------------------------ Room shell

    static void BuildShell(Transform root)
    {
        var shell = Group("Shell", root);
        float width = RoomHalfWidth * 2f;
        float depth = RoomBack - RoomFront;
        float midZ = (RoomBack + RoomFront) * 0.5f;
        const float t = 0.1f;

        // Structural pieces keep colliders so dropped parts can't leave the room.
        Box("Floor", shell, new Vector3(0, -t / 2, midZ), new Vector3(width, t, depth), "Floor", true);
        Box("Ceiling", shell, new Vector3(0, RoomHeight + t / 2, midZ), new Vector3(width, t, depth), "Ceiling", true);
        Box("Wall_Back", shell, new Vector3(0, RoomHeight / 2, RoomBack + t / 2), new Vector3(width, RoomHeight, t), "Wall", true);
        Box("Wall_Front", shell, new Vector3(0, RoomHeight / 2, RoomFront - t / 2), new Vector3(width, RoomHeight, t), "Wall", true);
        Box("Wall_Left", shell, new Vector3(-RoomHalfWidth - t / 2, RoomHeight / 2, midZ), new Vector3(t, RoomHeight, depth), "Wall", true);
        Box("Wall_Right", shell, new Vector3(RoomHalfWidth + t / 2, RoomHeight / 2, midZ), new Vector3(t, RoomHeight, depth), "Wall", true);

        // Floor seams: a subtle tiled look.
        var seams = Group("FloorSeams", shell);
        for (float x = -RoomHalfWidth + 1f; x < RoomHalfWidth; x += 1f)
            Box("SeamX", seams, new Vector3(x, 0.001f, midZ), new Vector3(0.01f, 0.002f, depth), "DarkMetal");
        for (float z = RoomFront + 1f; z < RoomBack; z += 1f)
            Box("SeamZ", seams, new Vector3(0, 0.001f, z), new Vector3(width, 0.002f, 0.01f), "DarkMetal");

        // Dark baseboards with a thin blue glow line around the room.
        var trim = Group("Trim", shell);
        const float inset = 0.02f;
        Box("Baseboard_Back", trim, new Vector3(0, 0.06f, RoomBack - inset), new Vector3(width, 0.12f, 0.04f), "DarkMetal");
        Box("Baseboard_Front", trim, new Vector3(0, 0.06f, RoomFront + inset), new Vector3(width, 0.12f, 0.04f), "DarkMetal");
        Box("Baseboard_Left", trim, new Vector3(-RoomHalfWidth + inset, 0.06f, midZ), new Vector3(0.04f, 0.12f, depth), "DarkMetal");
        Box("Baseboard_Right", trim, new Vector3(RoomHalfWidth - inset, 0.06f, midZ), new Vector3(0.04f, 0.12f, depth), "DarkMetal");
        Box("Glow_Back", trim, new Vector3(0, 0.125f, RoomBack - inset - 0.005f), new Vector3(width, 0.01f, 0.045f), "GlowSoftBlue");
        Box("Glow_Front", trim, new Vector3(0, 0.125f, RoomFront + inset + 0.005f), new Vector3(width, 0.01f, 0.045f), "GlowSoftBlue");
        Box("Glow_Left", trim, new Vector3(-RoomHalfWidth + inset + 0.005f, 0.125f, midZ), new Vector3(0.045f, 0.01f, depth), "GlowSoftBlue");
        Box("Glow_Right", trim, new Vector3(RoomHalfWidth - inset - 0.005f, 0.125f, midZ), new Vector3(0.045f, 0.01f, depth), "GlowSoftBlue");

        // Ceiling light panels.
        var lights = Group("CeilingPanels", shell);
        for (int i = -1; i <= 1; i++)
        {
            Box("Panel", lights, new Vector3(i * 1.6f, RoomHeight - 0.01f, 0.6f), new Vector3(1.2f, 0.02f, 0.25f), "GlowWhite");
            Box("Panel", lights, new Vector3(i * 1.6f, RoomHeight - 0.01f, 2.4f), new Vector3(1.2f, 0.02f, 0.25f), "GlowWhite");
        }

        // Front wall (behind the player): a plain door so the room reads as a real space.
        var door = Group("Door", shell);
        Box("Frame", door, new Vector3(0, 1.1f, RoomFront + 0.02f), new Vector3(1.1f, 2.2f, 0.04f), "DarkMetal");
        Box("Leaf_L", door, new Vector3(-0.255f, 1.08f, RoomFront + 0.045f), new Vector3(0.49f, 2.1f, 0.02f), "WallPanel");
        Box("Leaf_R", door, new Vector3(0.255f, 1.08f, RoomFront + 0.045f), new Vector3(0.49f, 2.1f, 0.02f), "WallPanel");
        Box("Seam", door, new Vector3(0, 1.08f, RoomFront + 0.056f), new Vector3(0.01f, 2.1f, 0.005f), "GlowSoftBlue");
    }

    // ------------------------------------------------------------------ Table + puzzle stations

    static void BuildTable(Transform root)
    {
        var table = Group("AssemblyTable", root);
        table.localPosition = new Vector3(0, 0, TableZ);

        float topCenterY = TableTopY - TableThickness / 2;
        Box("TableTop", table, new Vector3(0, topCenterY, 0), new Vector3(TableWidth, TableThickness, TableDepth), "TableTop", true);

        // Steel edge band with a soft blue glow strip facing the player.
        Box("EdgeBand_Front", table, new Vector3(0, topCenterY, -TableDepth / 2 - 0.01f), new Vector3(TableWidth + 0.04f, TableThickness + 0.01f, 0.02f), "BrushedSteel");
        Box("EdgeBand_Back", table, new Vector3(0, topCenterY, TableDepth / 2 + 0.01f), new Vector3(TableWidth + 0.04f, TableThickness + 0.01f, 0.02f), "BrushedSteel");
        Box("EdgeBand_Left", table, new Vector3(-TableWidth / 2 - 0.01f, topCenterY, 0), new Vector3(0.02f, TableThickness + 0.01f, TableDepth), "BrushedSteel");
        Box("EdgeBand_Right", table, new Vector3(TableWidth / 2 + 0.01f, topCenterY, 0), new Vector3(0.02f, TableThickness + 0.01f, TableDepth), "BrushedSteel");
        Box("EdgeGlow", table, new Vector3(0, topCenterY - 0.005f, -TableDepth / 2 - 0.021f), new Vector3(TableWidth, 0.008f, 0.004f), "GlowBlue");

        // Low lip at the back and sides so pieces don't roll off.
        Box("Lip_Back", table, new Vector3(0, TableTopY + 0.015f, TableDepth / 2 - 0.01f), new Vector3(TableWidth, 0.03f, 0.02f), "BrushedSteel", true);
        Box("Lip_Left", table, new Vector3(-TableWidth / 2 + 0.01f, TableTopY + 0.015f, 0), new Vector3(0.02f, 0.03f, TableDepth), "BrushedSteel", true);
        Box("Lip_Right", table, new Vector3(TableWidth / 2 - 0.01f, TableTopY + 0.015f, 0), new Vector3(0.02f, 0.03f, TableDepth), "BrushedSteel", true);

        // Pedestal base: two angled steel legs and a floor plate.
        float legH = TableTopY - TableThickness;
        foreach (float x in new[] { -0.8f, 0.8f })
        {
            Box("Leg", table, new Vector3(x, legH / 2, 0.05f), new Vector3(0.12f, legH, 0.5f), "DarkMetal", true);
            Box("LegGlow", table, new Vector3(x, legH / 2, -0.201f), new Vector3(0.015f, legH * 0.8f, 0.002f), "GlowSoftBlue");
            Box("Foot", table, new Vector3(x, 0.015f, 0.05f), new Vector3(0.2f, 0.03f, 0.7f), "BrushedSteel");
        }
        Box("Crossbar", table, new Vector3(0, 0.25f, 0.2f), new Vector3(1.6f, 0.06f, 0.06f), "DarkMetal");

        BuildStations(table);
        BuildInstructionPanel(table);
    }

    // Three equal zones on the table. Each has an assembly pad (where the object is built)
    // and a parts tray in front of it (where loose pieces spawn), plus empty anchors for teammates.
    static void BuildStations(Transform table)
    {
        var stations = Group("Stations", table);
        string[] names = { "Station_Left", "Station_Center", "Station_Right" };
        string[] accents = { "GlowBlue", "GlowRed", "GlowGold" };
        float spacing = TableWidth / 3f;

        for (int i = 0; i < 3; i++)
        {
            var st = Group(names[i], stations);
            st.localPosition = new Vector3((i - 1) * spacing, TableTopY, 0);

            // Assembly pad at the back of the zone.
            var pad = Group("AssemblyPad", st);
            pad.localPosition = new Vector3(0, 0, 0.2f);
            Cylinder("Base", pad, new Vector3(0, 0.005f, 0), new Vector3(0.3f, 0.005f, 0.3f), "DarkMetal");
            Ring("AccentRing", pad, new Vector3(0, 0.011f, 0), Quaternion.identity, 0.15f, 0.008f, 0.004f, 32, accents[i]);

            var assemblyAnchor = Group("AssemblyAnchor", pad);
            assemblyAnchor.localPosition = new Vector3(0, 0.012f, 0);

            // Parts tray in front, closest to the player.
            var tray = Group("PartsTray", st);
            tray.localPosition = new Vector3(0, 0, -0.18f);
            float trayW = spacing - 0.1f;
            Box("Tray", tray, new Vector3(0, 0.003f, 0), new Vector3(trayW, 0.006f, 0.34f), "PartsTray");
            Box("TrayEdge", tray, new Vector3(0, 0.007f, 0.17f), new Vector3(trayW, 0.002f, 0.006f), accents[i]);

            // Suggested spawn points for 8 parts (2 rows x 4), spread so pieces are clearly separated.
            var spawns = Group("PartSpawnPoints", tray);
            int n = 0;
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 4; col++)
                {
                    var p = Group($"Spawn_{n++:00}", spawns);
                    p.localPosition = new Vector3(-trayW / 2 + trayW * (col + 0.5f) / 4f, 0.01f, row == 0 ? -0.08f : 0.08f);
                }
        }
    }

    static void BuildInstructionPanel(Transform table)
    {
        // Frame for a world-space UI canvas (step instructions). Attach your Canvas to "CanvasAnchor".
        var panel = Group("InstructionPanel", table);
        // Sits low behind the table like a monitor so it never hides the wall logo.
        const float panelY = 1.35f;
        panel.localPosition = new Vector3(0, panelY, TableDepth / 2 + 0.06f);

        Box("Backing", panel, new Vector3(0, 0, 0.01f), new Vector3(0.9f, 0.45f, 0.015f), "DarkMetal");
        Box("Glass", panel, Vector3.zero, new Vector3(0.86f, 0.41f, 0.005f), "WallPanel");
        Box("TopGlow", panel, new Vector3(0, 0.226f, 0), new Vector3(0.9f, 0.006f, 0.02f), "GlowBlue");
        float poleH = panelY - 0.225f;
        Box("Pole", panel, new Vector3(0, -0.225f - poleH / 2, 0.03f), new Vector3(0.04f, poleH, 0.03f), "BrushedSteel");
        Box("PoleFoot", panel, new Vector3(0, -panelY + 0.01f, 0.03f), new Vector3(0.25f, 0.02f, 0.2f), "DarkMetal");

        // A world-space Canvas placed here faces the player with no extra rotation.
        var anchor = Group("CanvasAnchor", panel);
        anchor.localPosition = new Vector3(0, 0, -0.005f);
    }

    // ------------------------------------------------------------------ Back wall: logo + skyline

    static void BuildBackWall(Transform root)
    {
        var wall = Group("BackWall", root);
        float z = RoomBack - 0.01f;

        // Recessed dark panel behind the logo.
        Box("LogoPanel", wall, new Vector3(0, 2.2f, z - 0.01f), new Vector3(1.6f, 1.6f, 0.02f), "WallPanel");
        Box("LogoPanelGlow", wall, new Vector3(0, 1.395f, z - 0.02f), new Vector3(1.6f, 0.01f, 0.01f), "GlowSoftBlue");

        var logo = Group("AvengersLogo", wall);
        logo.localPosition = new Vector3(0, 2.2f, z - 0.03f);
        BuildAvengersLogo(logo, 0.55f);

        // Skyline windows on either side of the logo.
        BuildWindow(wall, new Vector3(-1.95f, 1.75f, z), 1.6f, 1.5f, 1);
        BuildWindow(wall, new Vector3(1.95f, 1.75f, z), 1.6f, 1.5f, 2);
    }

    // The "A" in a circle. Built from flat boxes in the local XY plane.
    static void BuildAvengersLogo(Transform parent, float radius)
    {
        const float depth = 0.02f;
        float stroke = radius * 0.13f;

        // Ring with a gap at the upper right where the A's leg breaks through.
        int segments = 40;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments;
            float deg = a0 * Mathf.Rad2Deg;
            if (deg > 52f && deg < 72f) continue;
            float a = (i + 0.5f) * Mathf.PI * 2f / segments;
            var pos = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            float segLen = 2f * Mathf.PI * radius / segments * 1.08f;
            var seg = Box("Ring", parent, pos, new Vector3(stroke, segLen, depth), "Logo");
            seg.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
        }

        // Right leg: from bottom-right of ring, past the top of the circle.
        Vector3 rightBottom = new Vector3(radius * 0.42f, -radius * 0.9f, 0);
        Vector3 rightTop = new Vector3(radius * 0.62f, radius * 1.25f, 0);
        Strut("A_RightLeg", parent, rightBottom, rightTop, stroke * 1.15f, depth, "Logo");

        // Left leg: from bottom-left up to meet the right leg.
        Vector3 leftBottom = new Vector3(-radius * 0.62f, -radius * 0.78f, 0);
        Vector3 apex = new Vector3(radius * 0.55f, radius * 0.5f, 0);
        Strut("A_LeftLeg", parent, leftBottom, apex, stroke, depth, "Logo");

        // Crossbar as an arrow running out to the left of the ring.
        Vector3 barRight = new Vector3(radius * 0.47f, -radius * 0.18f, 0);
        Vector3 barLeft = new Vector3(-radius * 1.2f, -radius * 0.18f, 0);
        Strut("A_Crossbar", parent, barLeft, barRight, stroke * 0.8f, depth, "Logo");
        var tip = Box("A_ArrowTip", parent, barLeft + new Vector3(-stroke * 0.3f, 0, 0), new Vector3(stroke * 1.2f, stroke * 1.2f, depth), "Logo");
        tip.transform.localRotation = Quaternion.Euler(0, 0, 45f);
    }

    static void BuildWindow(Transform parent, Vector3 center, float w, float h, int seed)
    {
        var win = Group("SkylineWindow", parent);
        win.localPosition = center;

        Box("Frame", win, new Vector3(0, 0, -0.01f), new Vector3(w + 0.08f, h + 0.08f, 0.03f), "DarkMetal");
        Box("Sky", win, new Vector3(0, 0, -0.025f), new Vector3(w, h, 0.01f), "NightSky");
        Box("Mullion", win, new Vector3(0, 0, -0.06f), new Vector3(0.03f, h, 0.02f), "DarkMetal");
        Box("Sill", win, new Vector3(0, -h / 2 - 0.03f, -0.07f), new Vector3(w + 0.12f, 0.03f, 0.12f), "BrushedSteel");

        // Building silhouettes with a few lit windows.
        var rng = new System.Random(seed * 97);
        var city = Group("City", win);
        float x = -w / 2;
        while (x < w / 2 - 0.02f)
        {
            float bw = Mathf.Min(0.08f + (float)rng.NextDouble() * 0.14f, w / 2 - x);
            float bh = 0.2f + (float)rng.NextDouble() * 0.75f;
            float cx = x + bw / 2;
            float cy = -h / 2 + bh / 2;
            Box("Building", city, new Vector3(cx, cy, -0.03f), new Vector3(bw, bh, 0.005f), "Building");

            for (float wy = -h / 2 + 0.06f; wy < -h / 2 + bh - 0.04f; wy += 0.06f)
                for (float wx = x + 0.02f; wx < x + bw - 0.02f; wx += 0.035f)
                    if (rng.NextDouble() < 0.18)
                        Box("Light", city, new Vector3(wx + 0.006f, wy, -0.034f), new Vector3(0.012f, 0.018f, 0.002f), "CityLights");

            x += bw + 0.005f;
        }
    }

    // ------------------------------------------------------------------ Side walls

    static void BuildSideWalls(Transform root)
    {
        // Left wall: Captain America's shield.
        var left = Group("LeftWall", root);
        Box("Panel", left, new Vector3(-RoomHalfWidth + 0.01f, 1.7f, 1.6f), new Vector3(0.02f, 1.5f, 1.5f), "WallPanel");
        var shield = Group("CapShield", left);
        shield.localPosition = new Vector3(-RoomHalfWidth + 0.03f, 1.7f, 1.6f);
        shield.localRotation = Quaternion.Euler(0, 90f, 0); // local +Z faces into the room (+X)
        BuildCapShield(shield, 0.5f);
        WallStrips(left, -RoomHalfWidth + 0.015f, 1.6f, "GlowRed");

        // Right wall: Iron Man's arc reactor.
        var right = Group("RightWall", root);
        Box("Panel", right, new Vector3(RoomHalfWidth - 0.01f, 1.7f, 1.6f), new Vector3(0.02f, 1.5f, 1.5f), "WallPanel");
        var arc = Group("ArcReactor", right);
        arc.localPosition = new Vector3(RoomHalfWidth - 0.03f, 1.7f, 1.6f);
        arc.localRotation = Quaternion.Euler(0, -90f, 0);
        BuildArcReactor(arc, 0.42f);
        WallStrips(right, RoomHalfWidth - 0.015f, 1.6f, "GlowGold");

        // Trophy plinths in the back corners: a place to show the finished builds.
        Plinth(root, new Vector3(-2.3f, 0, 2.8f), "GlowBlue");
        Plinth(root, new Vector3(2.3f, 0, 2.8f), "GlowGold");
    }

    static void WallStrips(Transform parent, float x, float centerZ, string glow)
    {
        foreach (float dz in new[] { -0.95f, 0.95f })
            Box("Strip", parent, new Vector3(x, 1.7f, centerZ + dz), new Vector3(0.01f, 1.5f, 0.025f), glow);
    }

    static void BuildCapShield(Transform parent, float r)
    {
        // Concentric discs, each slightly in front of the last.
        Disc("Ring1_Red", parent, r, 0.000f, "CapRed");
        Disc("Ring2_White", parent, r * 0.80f, 0.004f, "CapWhite");
        Disc("Ring3_Red", parent, r * 0.60f, 0.008f, "CapRed");
        Disc("Center_Blue", parent, r * 0.40f, 0.012f, "CapBlue");

        var star = MeshObject("Star", parent, StarMesh(r * 0.37f, r * 0.15f), "CapWhite");
        star.transform.localPosition = new Vector3(0, 0, 0.017f);
    }

    static void BuildArcReactor(Transform parent, float r)
    {
        Disc("Housing", parent, r, 0f, "DarkMetal");
        Ring("OuterRing", parent, new Vector3(0, 0, 0.01f), Quaternion.Euler(90, 0, 0), r * 0.92f, r * 0.06f, 0.01f, 40, "BrushedSteel");
        Disc("GlowField", parent, r * 0.78f, 0.006f, "GlowSoftBlue");

        // Ten coil segments around the core.
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            var coil = Box("Coil", parent, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r * 0.58f + new Vector3(0, 0, 0.015f),
                new Vector3(r * 0.22f, r * 0.12f, 0.01f), "BrushedSteel");
            coil.transform.localRotation = Quaternion.Euler(0, 0, i * 36f);
        }

        Ring("InnerRing", parent, new Vector3(0, 0, 0.02f), Quaternion.Euler(90, 0, 0), r * 0.32f, r * 0.05f, 0.01f, 28, "BrushedSteel");
        Disc("Core", parent, r * 0.27f, 0.022f, "ArcCore");
    }

    static void Plinth(Transform parent, Vector3 pos, string glow)
    {
        var p = Group("TrophyPlinth", parent);
        p.localPosition = pos;
        Box("Base", p, new Vector3(0, 0.05f, 0), new Vector3(0.6f, 0.1f, 0.6f), "DarkMetal", true);
        Box("Column", p, new Vector3(0, 0.55f, 0), new Vector3(0.45f, 0.9f, 0.45f), "WallPanel", true);
        Box("Top", p, new Vector3(0, 1.02f, 0), new Vector3(0.55f, 0.04f, 0.55f), "BrushedSteel", true);
        Box("Glow", p, new Vector3(0, 0.995f, 0), new Vector3(0.56f, 0.01f, 0.56f), glow);
        var anchor = Group("DisplayAnchor", p);
        anchor.localPosition = new Vector3(0, 1.04f, 0);
    }

    // ------------------------------------------------------------------ Player spot + lighting

    static void BuildPlayerSpot(Transform root)
    {
        var spot = Group("PlayerSpot", root);
        Cylinder("Plate", spot, new Vector3(0, 0.003f, 0), new Vector3(0.9f, 0.003f, 0.9f), "DarkMetal");
        Ring("Ring", spot, new Vector3(0, 0.007f, 0), Quaternion.identity, 0.45f, 0.012f, 0.003f, 48, "GlowSoftBlue");
        // Small arrow pointing toward the table.
        var arrow = Box("Arrow", spot, new Vector3(0, 0.007f, 0.36f), new Vector3(0.05f, 0.003f, 0.05f), "GlowSoftBlue");
        arrow.transform.localRotation = Quaternion.Euler(0, 45f, 0);
    }

    static void BuildLighting(Transform root)
    {
        var lights = Group("Lights", root);

        // A key light over the table and a soft fill in the room. Few realtime lights keeps Quest performance up.
        var key = new GameObject("TableKeyLight").AddComponent<Light>();
        key.transform.SetParent(lights, false);
        key.transform.localPosition = new Vector3(0, 2.6f, 0.9f);
        key.transform.localRotation = Quaternion.Euler(80f, 0, 0);
        key.type = LightType.Spot;
        key.spotAngle = 85f;
        key.range = 4f;
        key.intensity = 6f;
        key.color = new Color(1f, 0.96f, 0.9f);
        key.shadows = LightShadows.Soft;

        var fill = new GameObject("RoomFillLight").AddComponent<Light>();
        fill.transform.SetParent(lights, false);
        fill.transform.localPosition = new Vector3(0, 2.8f, 1.5f);
        fill.type = LightType.Point;
        fill.range = 7f;
        fill.intensity = 1.5f;
        fill.color = new Color(0.75f, 0.85f, 1f);
        fill.shadows = LightShadows.None;

        // The default sun shouldn't pour through the ceiling; keep it dim and shadowless as a general fill.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            Undo.RecordObject(l, "Dim directional light");
            l.intensity = 0.25f;
            l.shadows = LightShadows.None;
            l.color = new Color(0.8f, 0.85f, 1f);
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.28f, 0.30f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.19f, 0.23f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.10f);
    }

    // Puts the existing Distance Grab sample cube on the table so grabbing can be tested right away.
    static void PlaceSampleGrabbable()
    {
        var cube = GameObject.Find("[BuildingBlock] Cube");
        if (cube == null) return;
        Undo.RecordObject(cube.transform, "Move sample cube");
        float half = cube.transform.lossyScale.y * 0.5f;
        cube.transform.position = new Vector3(0, TableTopY + half + 0.002f, TableZ - 0.18f);
        cube.transform.rotation = Quaternion.identity;
    }

    // ------------------------------------------------------------------ Primitive helpers

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, string mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = Mats[mat];
        // Big flat surfaces don't need to cast shadows; saves GPU time on Quest.
        r.shadowCastingMode = keepCollider && scale.y > 0.3f ? ShadowCastingMode.Off : ShadowCastingMode.On;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, string mat, bool keepCollider = false)
        => Primitive(PrimitiveType.Cube, name, parent, pos, scale, mat, keepCollider);

    static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, string mat)
        => Primitive(PrimitiveType.Cylinder, name, parent, pos, scale, mat, false);

    // A flat disc whose face points along the parent's local +Z.
    static void Disc(string name, Transform parent, float radius, float zOffset, string mat)
    {
        var d = Cylinder(name, parent, new Vector3(0, 0, zOffset), new Vector3(radius * 2f, 0.002f, radius * 2f), mat);
        d.transform.localRotation = Quaternion.Euler(90f, 0, 0);
    }

    // A ring of box segments lying in the XZ plane of `rotation`.
    static void Ring(string name, Transform parent, Vector3 center, Quaternion rotation, float radius, float width, float height, int segments, string mat)
    {
        var ring = Group(name, parent);
        ring.localPosition = center;
        ring.localRotation = rotation;
        float segLen = 2f * Mathf.PI * radius / segments * 1.05f;
        for (int i = 0; i < segments; i++)
        {
            float a = i * 360f / segments;
            float rad = a * Mathf.Deg2Rad;
            var seg = Box("Seg", ring, new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * radius, new Vector3(segLen, height, width), mat);
            seg.transform.localRotation = Quaternion.Euler(0, a, 0);
        }
    }

    // A box stretched between two points in the local XY plane.
    static void Strut(string name, Transform parent, Vector3 a, Vector3 b, float thickness, float depth, string mat)
    {
        Vector3 d = b - a;
        var go = Box(name, parent, (a + b) / 2f, new Vector3(thickness, d.magnitude, depth), mat);
        go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
    }

    static GameObject MeshObject(string name, Transform parent, Mesh mesh, string mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = Mats[mat];
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    // Flat five-point star in the XY plane, facing +Z. Saved as an asset so the scene keeps it.
    static Mesh StarMesh(float outer, float inner)
    {
        EnsureFolder(MeshFolder);
        string path = $"{MeshFolder}/Star.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Star" };
            AssetDatabase.CreateAsset(mesh, path);
        }

        var verts = new List<Vector3> { Vector3.zero };
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? outer : inner;
            verts.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
        }
        var tris = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            // Wound so the face is visible from +Z.
            tris.Add(0);
            tris.Add(1 + i);
            tris.Add(1 + (i + 1) % 10);
        }
        var normals = new Vector3[verts.Count];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.forward;

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.normals = normals;
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
