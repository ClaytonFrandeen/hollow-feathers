using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Editor window that imports scatter points exported from Houdini (CSV/JSON)
/// and instantiates a prefab at each point.
///
/// Supported CSV columns (header row required):
///   px, py, pz          — position (required)
///   nx, ny, nz          — normal (optional, for alignment)
///   pscale              — per-point scale (optional)
///   rx, ry, rz, rw      — quaternion orient (optional)
///   euler_x, euler_y, euler_z — euler rotation in degrees (optional, used if no quaternion)
///
/// Houdini coordinate system (right-handed, Y-up) is converted to Unity (left-handed, Y-up)
/// by negating the X axis.
///
/// Usage:  Window → Houdini Point Importer
/// </summary>
public class HoudiniPointImporter : EditorWindow
{
    // --- Data source ---
    private TextAsset csvFile;
    private string lastFilePath = "";

    // --- Prefab ---
    private GameObject prefabToPlace;
    private Mesh fallbackMesh;

    // --- Coordinate conversion ---
    private bool convertCoordinates = true;

    // --- Alignment ---
    private bool alignToNormal = false;

    // --- Scale ---
    private float globalScale = 1f;
    private bool usePscale = true;
    private Vector2 scaleClamp = new Vector2(0.1f, 10f);

    // --- Additional randomization (on top of Houdini data) ---
    private bool additionalRandomY = false;
    private Vector2 additionalRandomYRange = new Vector2(0f, 360f);
    private bool additionalRandomScale = false;
    private Vector2 additionalScaleRange = new Vector2(0.9f, 1.1f);

    // --- Organization ---
    private bool markStatic = true;
    private bool parentToContainer = true;
    private string containerName = "HoudiniScatter";

    // --- Preview ---
    private List<PointData> parsedPoints = new List<PointData>();
    private bool showPreviewGizmos = false;
    private Vector2 scrollPos;

    private struct PointData
    {
        public Vector3 position;
        public Vector3 normal;
        public bool hasNormal;
        public Quaternion rotation;
        public bool hasRotation;
        public float pscale;
        public bool hasPscale;
    }

    [MenuItem("Window/Houdini Point Importer")]
    public static void ShowWindow()
    {
        GetWindow<HoudiniPointImporter>("Houdini Point Importer");
    }

    // ──────────────────────────────────────────────
    //  GUI
    // ──────────────────────────────────────────────
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Houdini Point Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── Data Source ──
        GUILayout.Label("Data Source", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV File", csvFile, typeof(TextAsset), false);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Or browse", lastFilePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFilePanel("Select scatter CSV", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
                lastFilePath = path;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Parse Points"))
        {
            ParseCSV();
        }

        if (parsedPoints.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"Parsed {parsedPoints.Count} points successfully.", MessageType.Info);
        }

        EditorGUILayout.Space(8);

        // ── Prefab ──
        GUILayout.Label("Mesh to Place", EditorStyles.boldLabel);

        prefabToPlace = (GameObject)EditorGUILayout.ObjectField(
            "Prefab / Model", prefabToPlace, typeof(GameObject), false);

        fallbackMesh = (Mesh)EditorGUILayout.ObjectField(
            "Raw Mesh (if no prefab)", fallbackMesh, typeof(Mesh), false);

        EditorGUILayout.Space(8);

        // ── Coordinate System ──
        GUILayout.Label("Coordinate System", EditorStyles.boldLabel);

        convertCoordinates = EditorGUILayout.Toggle(
            new GUIContent("Houdini → Unity Conversion",
                "Negates X axis to convert from Houdini's right-handed to Unity's left-handed coordinates."),
            convertCoordinates);

        EditorGUILayout.Space(8);

        // ── Alignment ──
        GUILayout.Label("Alignment", EditorStyles.boldLabel);

        alignToNormal = EditorGUILayout.Toggle(
            new GUIContent("Align to Exported Normal",
                "Rotates the up axis to match the N attribute from Houdini."),
            alignToNormal);

        EditorGUILayout.Space(8);

        // ── Scale ──
        GUILayout.Label("Scale", EditorStyles.boldLabel);

        globalScale = EditorGUILayout.FloatField("Global Scale", globalScale);
        usePscale = EditorGUILayout.Toggle("Use pscale from CSV", usePscale);
        scaleClamp = EditorGUILayout.Vector2Field("Scale Clamp (min, max)", scaleClamp);

        EditorGUILayout.Space(8);

        // ── Extra Randomization ──
        GUILayout.Label("Additional Randomization", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Applied on top of whatever rotation/scale came from Houdini.",
            MessageType.None);

        additionalRandomY = EditorGUILayout.Toggle("Extra Random Y Rotation", additionalRandomY);
        if (additionalRandomY)
        {
            EditorGUI.indentLevel++;
            additionalRandomYRange = EditorGUILayout.Vector2Field("Y Range (degrees)", additionalRandomYRange);
            EditorGUI.indentLevel--;
        }

        additionalRandomScale = EditorGUILayout.Toggle("Extra Random Scale", additionalRandomScale);
        if (additionalRandomScale)
        {
            EditorGUI.indentLevel++;
            additionalScaleRange = EditorGUILayout.Vector2Field("Scale Range (multiplier)", additionalScaleRange);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Organization ──
        GUILayout.Label("Organization", EditorStyles.boldLabel);

        markStatic = EditorGUILayout.Toggle("Mark as Static", markStatic);
        parentToContainer = EditorGUILayout.Toggle("Parent to Container", parentToContainer);
        if (parentToContainer)
        {
            EditorGUI.indentLevel++;
            containerName = EditorGUILayout.TextField("Container Name", containerName);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Preview ──
        showPreviewGizmos = EditorGUILayout.Toggle("Show Preview Gizmos", showPreviewGizmos);

        EditorGUILayout.Space(8);

        // ── Action Buttons ──
        GUI.enabled = parsedPoints.Count > 0 && (prefabToPlace != null || fallbackMesh != null);

        Color defaultBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button($"Place {parsedPoints.Count} Meshes", GUILayout.Height(36)))
        {
            PlaceAll();
        }
        GUI.backgroundColor = defaultBg;

        GUI.enabled = true;

        EditorGUILayout.Space(4);
        EditorGUILayout.EndScrollView();
    }

    // ──────────────────────────────────────────────
    //  CSV Parsing
    // ──────────────────────────────────────────────
    private void ParseCSV()
    {
        string text = null;

        if (csvFile != null)
        {
            text = csvFile.text;
        }
        else if (!string.IsNullOrEmpty(lastFilePath) && File.Exists(lastFilePath))
        {
            text = File.ReadAllText(lastFilePath);
        }

        if (string.IsNullOrEmpty(text))
        {
            EditorUtility.DisplayDialog("Error", "No CSV file provided or file is empty.", "OK");
            return;
        }

        parsedPoints.Clear();

        string[] lines = text.Split('\n');
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Error", "CSV must have a header row and at least one data row.", "OK");
            return;
        }

        // Parse header
        string[] headers = lines[0].Trim().ToLower().Split(',');
        var colIndex = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
            colIndex[headers[i].Trim()] = i;

        // Validate required columns
        if (!colIndex.ContainsKey("px") || !colIndex.ContainsKey("py") || !colIndex.ContainsKey("pz"))
        {
            EditorUtility.DisplayDialog("Error",
                "CSV must contain px, py, pz columns.\n\n" +
                $"Found columns: {string.Join(", ", headers)}", "OK");
            return;
        }

        bool hasNx = colIndex.ContainsKey("nx") && colIndex.ContainsKey("ny") && colIndex.ContainsKey("nz");
        bool hasQuat = colIndex.ContainsKey("rx") && colIndex.ContainsKey("ry") &&
                       colIndex.ContainsKey("rz") && colIndex.ContainsKey("rw");
        bool hasEuler = colIndex.ContainsKey("euler_x") && colIndex.ContainsKey("euler_y") &&
                        colIndex.ContainsKey("euler_z");
        bool hasPscale = colIndex.ContainsKey("pscale");

        // Parse rows
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');
            if (cols.Length < headers.Length) continue;

            PointData pt = new PointData();

            // Position
            float px = ParseFloat(cols[colIndex["px"]]);
            float py = ParseFloat(cols[colIndex["py"]]);
            float pz = ParseFloat(cols[colIndex["pz"]]);

            if (convertCoordinates)
                pt.position = new Vector3(-px, py, pz); // Negate X for handedness
            else
                pt.position = new Vector3(px, py, pz);

            // Normal
            if (hasNx)
            {
                float nx = ParseFloat(cols[colIndex["nx"]]);
                float ny = ParseFloat(cols[colIndex["ny"]]);
                float nz = ParseFloat(cols[colIndex["nz"]]);

                if (convertCoordinates)
                    pt.normal = new Vector3(-nx, ny, nz);
                else
                    pt.normal = new Vector3(nx, ny, nz);

                pt.hasNormal = true;
            }

            // Rotation — quaternion takes priority
            if (hasQuat)
            {
                float rx = ParseFloat(cols[colIndex["rx"]]);
                float ry = ParseFloat(cols[colIndex["ry"]]);
                float rz = ParseFloat(cols[colIndex["rz"]]);
                float rw = ParseFloat(cols[colIndex["rw"]]);

                if (convertCoordinates)
                    pt.rotation = new Quaternion(rx, -ry, -rz, rw); // Convert handedness
                else
                    pt.rotation = new Quaternion(rx, ry, rz, rw);

                pt.hasRotation = true;
            }
            else if (hasEuler)
            {
                float ex = ParseFloat(cols[colIndex["euler_x"]]);
                float ey = ParseFloat(cols[colIndex["euler_y"]]);
                float ez = ParseFloat(cols[colIndex["euler_z"]]);

                if (convertCoordinates)
                    pt.rotation = Quaternion.Euler(ex, -ey, -ez);
                else
                    pt.rotation = Quaternion.Euler(ex, ey, ez);

                pt.hasRotation = true;
            }

            // pscale
            if (hasPscale)
            {
                pt.pscale = ParseFloat(cols[colIndex["pscale"]]);
                pt.hasPscale = true;
            }
            else
            {
                pt.pscale = 1f;
            }

            parsedPoints.Add(pt);
        }

        Debug.Log($"[HoudiniPointImporter] Parsed {parsedPoints.Count} points.");
        SceneView.RepaintAll();
        Repaint();
    }

    private float ParseFloat(string s)
    {
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float val);
        return val;
    }

    // ──────────────────────────────────────────────
    //  Placement
    // ──────────────────────────────────────────────
    private void PlaceAll()
    {
        if (parsedPoints.Count == 0) return;

        Undo.SetCurrentGroupName("Import Houdini Scatter");
        int undoGroup = Undo.GetCurrentGroup();

        // Create container
        Transform container = null;
        if (parentToContainer)
        {
            GameObject containerGo = new GameObject(containerName);
            Undo.RegisterCreatedObjectUndo(containerGo, "Create Scatter Container");
            container = containerGo.transform;
        }

        int placed = 0;
        foreach (var pt in parsedPoints)
        {
            GameObject obj;

            if (prefabToPlace != null)
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
            }
            else if (fallbackMesh != null)
            {
                obj = new GameObject(fallbackMesh.name);
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.sharedMesh = fallbackMesh;
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            else continue;

            // Position
            obj.transform.position = pt.position;

            // Rotation
            Quaternion rot = Quaternion.identity;

            if (pt.hasRotation)
            {
                rot = pt.rotation;
            }
            else if (alignToNormal && pt.hasNormal)
            {
                rot = Quaternion.FromToRotation(Vector3.up, pt.normal);
            }

            // Additional random Y
            if (additionalRandomY)
                rot *= Quaternion.Euler(0f, Random.Range(additionalRandomYRange.x, additionalRandomYRange.y), 0f);

            obj.transform.rotation = rot;

            // Scale
            float scale = globalScale;
            if (usePscale && pt.hasPscale)
                scale *= pt.pscale;
            if (additionalRandomScale)
                scale *= Random.Range(additionalScaleRange.x, additionalScaleRange.y);

            scale = Mathf.Clamp(scale, scaleClamp.x, scaleClamp.y);
            obj.transform.localScale = Vector3.one * scale;

            // Parent
            if (container != null)
                obj.transform.SetParent(container, true);

            // Static flags
            if (markStatic)
                GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic
                    | StaticEditorFlags.OccludeeStatic
                    | StaticEditorFlags.OccluderStatic
                    | StaticEditorFlags.ContributeGI);

            Undo.RegisterCreatedObjectUndo(obj, "Place Scatter Point");
            placed++;

            // Progress bar for large scatters
            if (placed % 100 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Placing meshes...",
                    $"{placed} / {parsedPoints.Count}",
                    (float)placed / parsedPoints.Count))
                {
                    EditorUtility.ClearProgressBar();
                    break;
                }
            }
        }

        EditorUtility.ClearProgressBar();
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[HoudiniPointImporter] Placed {placed} meshes.");
    }

    // ──────────────────────────────────────────────
    //  Scene Gizmos — preview point cloud
    // ──────────────────────────────────────────────
    private void OnEnable()
    {
        SceneView.duringSceneGui += DrawPreviewGizmos;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawPreviewGizmos;
    }

    private void DrawPreviewGizmos(SceneView sceneView)
    {
        if (!showPreviewGizmos || parsedPoints.Count == 0) return;

        Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);

        // Cap gizmo count for performance
        int drawCount = Mathf.Min(parsedPoints.Count, 5000);
        float step = (float)parsedPoints.Count / drawCount;

        for (int i = 0; i < drawCount; i++)
        {
            int idx = Mathf.Min((int)(i * step), parsedPoints.Count - 1);
            PointData pt = parsedPoints[idx];

            float size = globalScale * 0.15f;
            if (usePscale && pt.hasPscale)
                size *= pt.pscale;

            Handles.SphereHandleCap(0, pt.position, Quaternion.identity,
                Mathf.Max(size, 0.05f), EventType.Repaint);

            // Draw normal direction
            if (pt.hasNormal && alignToNormal)
            {
                Handles.color = new Color(1f, 0.6f, 0.2f, 0.5f);
                Handles.DrawLine(pt.position, pt.position + pt.normal * size * 3f);
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
            }
        }
    }
}
