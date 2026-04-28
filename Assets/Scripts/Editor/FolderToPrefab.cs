using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class FolderToPrefab : EditorWindow
{
    [MenuItem("Tools/Folder To Prefab")]
    static void ShowWindow()
    {
        GetWindow<FolderToPrefab>("Folder To Prefab");
    }

    // Where generated materials go
    string materialOutputPath = "Assets/GeneratedMaterials";
    // Where generated prefabs go
    string prefabOutputPath = "Assets/GeneratedPrefabs";
    // Process subfolders recursively
    bool recursive = true;

    void OnGUI()
    {
        GUILayout.Label("Folder To Prefab Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Select a folder containing a mesh and textures (color, mask, normal, roughness, height).\n" +
            "The prefab and material will be named after the folder.\n\n" +
            "Expected texture names: color, mask, normal, roughness, height",
            MessageType.Info
        );

        EditorGUILayout.Space(10);
        materialOutputPath = EditorGUILayout.TextField("Material Output", materialOutputPath);
        prefabOutputPath = EditorGUILayout.TextField("Prefab Output", prefabOutputPath);
        recursive = EditorGUILayout.Toggle("Process Subfolders", recursive);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Process Selected Folder(s)", GUILayout.Height(30)))
        {
            ProcessSelectedFolders();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Process Single Folder (Pick)", GUILayout.Height(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Asset Folder", "Assets", "");
            if (!string.IsNullOrEmpty(folder))
            {
                // Convert absolute path to relative Assets path
                if (folder.StartsWith(Application.dataPath))
                    folder = "Assets" + folder.Substring(Application.dataPath.Length);

                ProcessFolder(folder);
            }
        }
    }

    void ProcessSelectedFolders()
    {
        Object[] selected = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Select one or more folders in the Project window.", "OK");
            return;
        }

        int count = 0;
        foreach (Object obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
            {
                if (recursive)
                    count += ProcessFolderRecursive(path);
                else
                    count += ProcessFolder(path) ? 1 : 0;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Created {count} prefab(s).", "OK");
    }

    int ProcessFolderRecursive(string folderPath)
    {
        int count = 0;

        // Try this folder first
        if (FolderHasMesh(folderPath))
            count += ProcessFolder(folderPath) ? 1 : 0;

        // Then recurse into subfolders
        string[] subfolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (string sub in subfolders)
            count += ProcessFolderRecursive(sub);

        return count;
    }

    bool FolderHasMesh(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { folderPath });
        // Only count meshes directly in this folder, not subfolders
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetDir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            if (assetDir == folderPath)
                return true;
        }
        return false;
    }

    bool ProcessFolder(string folderPath)
    {
        string folderName = Path.GetFileName(folderPath);
        Debug.Log($"[FolderToPrefab] Processing: {folderPath}");

        // --- Find the mesh ---
        Mesh mesh = FindAssetInFolder<Mesh>(folderPath);

        // Mesh might be inside an FBX/OBJ model — try loading from model importers
        if (mesh == null)
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            foreach (string guid in modelGuids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                string modelDir = Path.GetDirectoryName(modelPath).Replace("\\", "/");
                if (modelDir != folderPath) continue;

                // Grab the first mesh from the model asset
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (Object sub in subAssets)
                {
                    if (sub is Mesh m)
                    {
                        mesh = m;
                        break;
                    }
                }
                if (mesh != null) break;
            }
        }

        if (mesh == null)
        {
            Debug.LogWarning($"[FolderToPrefab] No mesh found in {folderPath}, skipping.");
            return false;
        }

        // --- Find textures by name convention ---
        Texture2D colorMap     = FindTextureByName(folderPath, "color");
        Texture2D maskMap      = FindTextureByName(folderPath, "mask");
        Texture2D normalMap    = FindTextureByName(folderPath, "normal");
        Texture2D roughnessMap = FindTextureByName(folderPath, "roughness");
        Texture2D heightMap    = FindTextureByName(folderPath, "height");

        // --- Ensure the normal map import settings are correct ---
        if (normalMap != null)
            EnsureNormalMapImportSettings(normalMap);

        // --- Create material ---
        EnsureDirectoryExists(materialOutputPath);

        string matPath = $"{materialOutputPath}/{folderName}_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                Debug.LogError("[FolderToPrefab] Could not find HDRP/Lit shader. Is HDRP installed?");
                return false;
            }

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        AssignTexturesHDRP(mat, colorMap, maskMap, normalMap, roughnessMap, heightMap);

        EditorUtility.SetDirty(mat);

        // --- Create prefab ---
        EnsureDirectoryExists(prefabOutputPath);

        GameObject go = new GameObject(folderName);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;

        string prefabPath = $"{prefabOutputPath}/{folderName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);

        Debug.Log($"[FolderToPrefab] Created: {prefabPath} with material {matPath}");
        return true;
    }

    // --- Texture assignment for HDRP Lit ---
    void AssignTexturesHDRP(Material mat, Texture2D color, Texture2D mask,
                            Texture2D normal, Texture2D roughness, Texture2D height)
    {
        // Base color
        if (color != null)
        {
            mat.SetTexture("_BaseColorMap", color);
            mat.SetColor("_BaseColor", Color.white);
        }

        // Mask map — HDRP packs channels: R=Metallic, G=AO, B=Detail Mask, A=Smoothness
        if (mask != null)
        {
            mat.SetTexture("_MaskMap", mask);
            mat.EnableKeyword("_MASKMAP");
        }

        // Normal map
        if (normal != null)
        {
            mat.SetTexture("_NormalMap", normal);
            mat.SetFloat("_NormalScale", 1.0f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
        }

        // Roughness — HDRP expects smoothness (1 - roughness)
        // If a mask map is present, smoothness is in the mask alpha already.
        // If only a standalone roughness map exists, we need to invert it into
        // a smoothness-based mask map. For now we set the remapping range to
        // flip roughness → smoothness via the smoothness remap.
        if (roughness != null && mask == null)
        {
            // No mask map — assign roughness as the mask and invert via remap
            mat.SetTexture("_MaskMap", roughness);
            mat.EnableKeyword("_MASKMAP");
            // Remap smoothness: the A channel contains roughness (0=smooth,1=rough)
            // so flip the remap range to invert it
            mat.SetFloat("_SmoothnessRemapMin", 1.0f);
            mat.SetFloat("_SmoothnessRemapMax", 0.0f);
            Debug.Log($"[FolderToPrefab] Roughness map assigned to MaskMap with inverted " +
                      "smoothness remap. For best results, pack a proper MaskMap (R=Metal, G=AO, B=Detail, A=Smoothness).");
        }

        // Height / displacement map
        if (height != null)
        {
            mat.SetTexture("_HeightMap", height);
            mat.EnableKeyword("_HEIGHTMAP");
            // Pixel displacement is heavy — default to 1cm amplitude
            mat.SetFloat("_HeightAmplitude", 0.01f);
            mat.SetFloat("_HeightCenter", 0.5f);
            // Use parallax occlusion mapping (less expensive than tessellation)
            // DisplacementMode: 0=None, 1=Vertex, 2=Pixel
            mat.SetFloat("_DisplacementMode", 2);
        }

        // Make sure the material validates its keyword state
        // HDMaterial.ValidateMaterial is internal, but setting the float
        // properties above and calling SetDirty is usually enough.
    }

    // --- Helpers ---

    T FindAssetInFolder<T>(string folderPath) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetDir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            if (assetDir == folderPath)
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
        return null;
    }

    Texture2D FindTextureByName(string folderPath, string textureName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetDir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            if (assetDir != folderPath) continue;

            string fileName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
            if (fileName == textureName.ToLower())
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        return null;
    }

    void EnsureNormalMapImportSettings(Texture2D normalMap)
    {
        string path = AssetDatabase.GetAssetPath(normalMap);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            Debug.Log($"[FolderToPrefab] Set {path} to NormalMap texture type.");
        }
    }

    void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
