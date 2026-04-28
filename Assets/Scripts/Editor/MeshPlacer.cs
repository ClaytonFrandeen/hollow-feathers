using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window that lets you:
///   1. Drag or select a mesh/prefab from the Project browser
///   2. Click in the Scene view to place it (Single mode)
///   3. Click-drag to paint a line of meshes along a surface (Paint mode)
///
/// Usage:  Window → Mesh Placer
/// </summary>
public class MeshPlacer : EditorWindow
{
    // --- Placement asset ---
    private GameObject prefabToPlace;
    private Mesh meshToPlace;

    // --- Mode ---
    private enum PlacementMode { Single, Paint }
    private PlacementMode placementMode = PlacementMode.Paint;

    // --- Placement options ---
    private bool alignToNormal = true;
    private float scaleMultiplier = 1f;

    // --- Organization ---
    private bool markStatic = true;
    private bool parentToContainer = true;
    private Transform containerParent;

    // --- Randomization ---
    private bool randomYRotation = false;
    private Vector2 randomYRange = new Vector2(0f, 360f);
    private bool randomXRotation = false;
    private Vector2 randomXRange = new Vector2(-15f, 15f);
    private bool randomZRotation = false;
    private Vector2 randomZRange = new Vector2(-15f, 15f);
    private bool randomScale = false;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    // --- Paint mode ---
    private float spacing = 1f;
    private Vector3 lastPlacedPosition;
    private bool isDragging = false;
    private bool hasPlacedFirst = false;

    // --- State ---
    private bool isPlacing = false;

    // --- Preview ---
    private GameObject previewInstance;
    private Material previewMaterial;

    // --- Undo grouping ---
    private List<GameObject> currentStrokeObjects = new List<GameObject>();

    // --- Scroll ---
    private Vector2 scrollPos;

    [MenuItem("Window/Mesh Placer")]
    public static void ShowWindow()
    {
        GetWindow<MeshPlacer>("Mesh Placer");
    }

    // ──────────────────────────────────────────────
    //  GUI
    // ──────────────────────────────────────────────
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Mesh Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Asset field
        EditorGUI.BeginChangeCheck();

        prefabToPlace = (GameObject)EditorGUILayout.ObjectField(
            "Prefab / Model", prefabToPlace, typeof(GameObject), false);

        meshToPlace = (Mesh)EditorGUILayout.ObjectField(
            "Raw Mesh (optional)", meshToPlace, typeof(Mesh), false);

        if (EditorGUI.EndChangeCheck())
            DestroyPreview();

        EditorGUILayout.Space(8);

        // ── Mode ──
        GUILayout.Label("Mode", EditorStyles.boldLabel);
        placementMode = (PlacementMode)EditorGUILayout.EnumPopup("Placement Mode", placementMode);

        if (placementMode == PlacementMode.Paint)
        {
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
            spacing = Mathf.Max(0.01f, spacing);
        }

        EditorGUILayout.Space(8);

        // ── Alignment ──
        GUILayout.Label("Alignment", EditorStyles.boldLabel);
        alignToNormal = EditorGUILayout.Toggle("Align to Surface Normal", alignToNormal);

        EditorGUILayout.Space(8);

        // ── Scale ──
        GUILayout.Label("Scale", EditorStyles.boldLabel);
        scaleMultiplier = EditorGUILayout.FloatField("Base Scale", scaleMultiplier);

        randomScale = EditorGUILayout.Toggle("Randomize Scale", randomScale);
        if (randomScale)
        {
            EditorGUI.indentLevel++;
            scaleRange = EditorGUILayout.Vector2Field("Scale Range (multiplier)", scaleRange);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Rotation ──
        GUILayout.Label("Rotation Randomization", EditorStyles.boldLabel);

        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);
        if (randomYRotation)
        {
            EditorGUI.indentLevel++;
            randomYRange = EditorGUILayout.Vector2Field("Y Range (degrees)", randomYRange);
            EditorGUI.indentLevel--;
        }

        randomXRotation = EditorGUILayout.Toggle("Random X Rotation", randomXRotation);
        if (randomXRotation)
        {
            EditorGUI.indentLevel++;
            randomXRange = EditorGUILayout.Vector2Field("X Range (degrees)", randomXRange);
            EditorGUI.indentLevel--;
        }

        randomZRotation = EditorGUILayout.Toggle("Random Z Rotation", randomZRotation);
        if (randomZRotation)
        {
            EditorGUI.indentLevel++;
            randomZRange = EditorGUILayout.Vector2Field("Z Range (degrees)", randomZRange);
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
            containerParent = (Transform)EditorGUILayout.ObjectField(
                "Container", containerParent, typeof(Transform), true);

            if (containerParent == null)
                EditorGUILayout.HelpBox(
                    "No container assigned — one will be created automatically when you start placing.",
                    MessageType.None);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Toggle button ──
        Color defaultBg = GUI.backgroundColor;
        if (isPlacing) GUI.backgroundColor = Color.green;

        string modeLabel = placementMode == PlacementMode.Paint ? "Paint" : "Single";
        string label = isPlacing
            ? $"● Placing ({modeLabel}) — click in Scene (Esc to cancel)"
            : $"Start Placing ({modeLabel})";

        if (GUILayout.Button(label, GUILayout.Height(32)))
        {
            if (!isPlacing && (prefabToPlace != null || meshToPlace != null))
                StartPlacing();
            else
                StopPlacing();
        }
        GUI.backgroundColor = defaultBg;

        if (prefabToPlace == null && meshToPlace == null)
            EditorGUILayout.HelpBox(
                "Assign a Prefab/Model or a raw Mesh above, then click Start Placing.",
                MessageType.Info);

        EditorGUILayout.Space(4);

        string helpText = placementMode == PlacementMode.Paint
            ? "Paint mode: Click-drag to paint meshes along a surface.\n" +
              "Hold Shift + Click for single placement without leaving paint mode.\n" +
              "Press Esc or click the button again to stop."
            : "Single mode: Click to place one mesh.\n" +
              "Hold Shift + Click to place multiple copies without leaving placement mode.\n" +
              "Press Esc or click the button again to stop.";

        EditorGUILayout.HelpBox(helpText, MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    // ──────────────────────────────────────────────
    //  Start / Stop
    // ──────────────────────────────────────────────
    private void StartPlacing()
    {
        isPlacing = true;
        SceneView.duringSceneGui += OnSceneGUI;
        CreatePreview();
        Repaint();
    }

    private void StopPlacing()
    {
        isPlacing = false;
        isDragging = false;
        hasPlacedFirst = false;
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyPreview();
        Repaint();
    }

    private void OnDestroy()
    {
        StopPlacing();
    }

    // ──────────────────────────────────────────────
    //  Scene GUI — raycast + preview + placement
    // ──────────────────────────────────────────────
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPlacing) return;

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        // Esc cancels
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            StopPlacing();
            e.Use();
            return;
        }

        // Raycast from mouse position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity);

        // Update preview ghost
        if (previewInstance != null)
        {
            if (hit)
            {
                previewInstance.SetActive(true);
                previewInstance.transform.position = hitInfo.point;

                if (alignToNormal)
                    previewInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                else
                    previewInstance.transform.rotation = Quaternion.identity;

                previewInstance.transform.localScale = Vector3.one * scaleMultiplier;
            }
            else
            {
                previewInstance.SetActive(false);
            }
        }

        // ── Paint Mode ──
        if (placementMode == PlacementMode.Paint)
        {
            HandlePaintMode(e, controlId, hit, hitInfo);
        }
        // ── Single Mode (original behavior) ──
        else
        {
            HandleSingleMode(e, controlId, hit, hitInfo);
        }

        // Eat Layout so the default scene tools don't fight us
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        sceneView.Repaint();
    }

    private void HandleSingleMode(Event e, int controlId, bool hit, RaycastHit hitInfo)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlId;

            if (hit)
                PlaceObject(hitInfo);

            e.Use();

            if (!e.shift)
                StopPlacing();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    private void HandlePaintMode(Event e, int controlId, bool hit, RaycastHit hitInfo)
    {
        // Mouse down — start dragging, place first mesh
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlId;
            isDragging = true;
            hasPlacedFirst = false;
            currentStrokeObjects.Clear();

            Undo.SetCurrentGroupName("Paint Meshes");

            if (hit)
            {
                PlaceObject(hitInfo);
                lastPlacedPosition = hitInfo.point;
                hasPlacedFirst = true;
            }

            e.Use();
        }

        // Mouse drag — place meshes along the stroke at spacing intervals
        if (e.type == EventType.MouseDrag && e.button == 0 && isDragging && !e.alt)
        {
            GUIUtility.hotControl = controlId;

            if (hit)
            {
                if (!hasPlacedFirst)
                {
                    PlaceObject(hitInfo);
                    lastPlacedPosition = hitInfo.point;
                    hasPlacedFirst = true;
                }
                else
                {
                    float dist = Vector3.Distance(hitInfo.point, lastPlacedPosition);
                    if (dist >= spacing)
                    {
                        // Place meshes along the line at spacing intervals
                        Vector3 direction = (hitInfo.point - lastPlacedPosition).normalized;
                        float covered = spacing;

                        while (covered <= dist)
                        {
                            Vector3 placePos = lastPlacedPosition + direction * covered;

                            // Re-raycast downward at each placement point for accurate surface following
                            Ray downRay = new Ray(placePos + Vector3.up * 50f, Vector3.down);
                            if (Physics.Raycast(downRay, out RaycastHit surfaceHit, 200f))
                            {
                                PlaceObject(surfaceHit);
                            }
                            else
                            {
                                // Fallback: use the interpolated position with the current hit normal
                                RaycastHit fakeHit = hitInfo;
                                // Place at the interpolated position on the original surface
                                PlaceObjectAtPosition(placePos, hitInfo.normal);
                            }

                            covered += spacing;
                        }

                        lastPlacedPosition = hitInfo.point;
                    }
                }
            }

            e.Use();
        }

        // Mouse up — end the stroke
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
            hasPlacedFirst = false;
            GUIUtility.hotControl = 0;

            // Collapse all objects from this stroke into one undo group
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            currentStrokeObjects.Clear();

            e.Use();
        }
    }

    // ──────────────────────────────────────────────
    //  Place the actual object
    // ──────────────────────────────────────────────
    private void PlaceObject(RaycastHit hit)
    {
        PlaceObjectAtPosition(hit.point, hit.normal);
    }

    private void PlaceObjectAtPosition(Vector3 position, Vector3 normal)
    {
        GameObject placed;

        if (prefabToPlace != null)
        {
            placed = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
        }
        else if (meshToPlace != null)
        {
            placed = new GameObject(meshToPlace.name);
            MeshFilter mf = placed.AddComponent<MeshFilter>();
            mf.sharedMesh = meshToPlace;
            MeshRenderer mr = placed.AddComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }
        else return;

        // Position
        placed.transform.position = position;

        // Rotation
        Quaternion rot = Quaternion.identity;

        if (alignToNormal)
            rot = Quaternion.FromToRotation(Vector3.up, normal);

        // Apply random rotations
        float rx = randomXRotation ? Random.Range(randomXRange.x, randomXRange.y) : 0f;
        float ry = randomYRotation ? Random.Range(randomYRange.x, randomYRange.y) : 0f;
        float rz = randomZRotation ? Random.Range(randomZRange.x, randomZRange.y) : 0f;
        rot *= Quaternion.Euler(rx, ry, rz);

        placed.transform.rotation = rot;

        // Scale
        float finalScale = scaleMultiplier;
        if (randomScale)
            finalScale *= Random.Range(scaleRange.x, scaleRange.y);

        placed.transform.localScale = Vector3.one * finalScale;

        // Parent to container
        if (parentToContainer)
        {
            if (containerParent == null)
            {
                string name = prefabToPlace != null ? prefabToPlace.name : meshToPlace.name;
                GameObject container = new GameObject($"{name}_Container");
                Undo.RegisterCreatedObjectUndo(container, "Create Container");
                containerParent = container.transform;
            }
            placed.transform.SetParent(containerParent, true);
        }

        // Mark static for batching
        if (markStatic)
            GameObjectUtility.SetStaticEditorFlags(placed, StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.OccludeeStatic
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.ContributeGI);

        // Undo
        Undo.RegisterCreatedObjectUndo(placed, "Place Mesh");
        currentStrokeObjects.Add(placed);

        Selection.activeGameObject = placed;
    }

    // ──────────────────────────────────────────────
    //  Preview ghost
    // ──────────────────────────────────────────────
    private void CreatePreview()
    {
        DestroyPreview();

        if (prefabToPlace != null)
        {
            previewInstance = Instantiate(prefabToPlace);
        }
        else if (meshToPlace != null)
        {
            previewInstance = new GameObject("MeshPlacerPreview");
            MeshFilter mf = previewInstance.AddComponent<MeshFilter>();
            mf.sharedMesh = meshToPlace;
            previewInstance.AddComponent<MeshRenderer>();
        }
        else return;

        previewInstance.name = "MeshPlacerPreview";
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        // Make the preview semi-transparent
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.hideFlags = HideFlags.HideAndDontSave;
            Color c = new Color(0.3f, 0.8f, 1f, 0.4f);
            previewMaterial.color = c;
            previewMaterial.SetFloat("_Mode", 3); // Transparent
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
        }

        foreach (var r in previewInstance.GetComponentsInChildren<Renderer>())
            r.sharedMaterial = previewMaterial;

        foreach (var c in previewInstance.GetComponentsInChildren<Collider>())
            DestroyImmediate(c);

        previewInstance.SetActive(false);
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
            DestroyImmediate(previewInstance);
        previewInstance = null;
    }
}