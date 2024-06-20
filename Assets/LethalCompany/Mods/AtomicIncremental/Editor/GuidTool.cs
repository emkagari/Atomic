using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class GUIDTool : MonoBehaviour
{
    [MenuItem("Tools/Generate GUID JSON")]
    public static void GenerateGUIDJson()
    {
        string path = EditorUtility.OpenFolderPanel("Select Folder to Scan", "Assets", "");
        if (string.IsNullOrEmpty(path))
            return;

        if (!path.StartsWith(Application.dataPath))
        {
            Debug.LogError("Please select a folder within the Assets directory.");
            return;
        }

        string relativePath = "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/");
        var guidData = new GUIDData();
        var assetPaths = AssetDatabase.FindAssets("", new[] { relativePath });

        int totalCount = assetPaths.Length;
        int currentCount = 0;

        foreach (var assetPath in assetPaths)
        {
            string assetPathString = AssetDatabase.GUIDToAssetPath(assetPath);
            if (assetPathString.EndsWith(".meta")) continue;

            string guid = AssetDatabase.AssetPathToGUID(assetPathString);
            guidData.assetsGuids.Add(new AssetGUID()
            {
                assetPath = assetPathString,
                originalGuid = guid,
                fileId = ""
            });

            currentCount++;
            float progress = (float)currentCount / totalCount;
            EditorUtility.DisplayProgressBar("Generating GUID JSON", $"Processing {assetPathString}", progress);
        }

        string json = JsonConvert.SerializeObject(guidData, Formatting.Indented);
        string tempFilePath = Path.Combine(Application.persistentDataPath, "guids.json").Replace("\\", "/");
        File.WriteAllText(tempFilePath, json);
        Debug.Log("GUID JSON generated at " + tempFilePath);

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Tools/Patch GUIDs From JSON")]
    public static void PatchGUIDsFromJson()
    {
        string jsonFilePath = EditorUtility.OpenFilePanel("Select GUID JSON File", "Assets", "json");
        if (string.IsNullOrEmpty(jsonFilePath))
            return;

        if (!jsonFilePath.StartsWith(Application.dataPath))
        {
            Debug.LogError("Please select a JSON file within the Assets directory.");
            return;
        }

        string relativePath = "Assets" + jsonFilePath.Substring(Application.dataPath.Length).Replace("\\", "/");
        string jsonContent = File.ReadAllText(relativePath);
        var guidData = JsonConvert.DeserializeObject<GUIDData>(jsonContent);

        // Step 1: Create a dictionary mapping current GUIDs to original GUIDs
        Dictionary<string, string> guidMapping = new Dictionary<string, string>();
        foreach (var assetGuid in guidData.assetsGuids)
        {
            guidMapping[AssetDatabase.AssetPathToGUID(assetGuid.assetPath)] = assetGuid.originalGuid;
        }

        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int totalCount = allAssetPaths.Length;
        int currentCount = 0;

        foreach (string assetPath in allAssetPaths)
        {
            if (assetPath.EndsWith(".meta") || !assetPath.StartsWith("Assets/") || Directory.Exists(assetPath)) continue;

            HashSet<string> dependencies = new HashSet<string>(AssetDatabase.GetDependencies(assetPath));
            dependencies.Remove(assetPath);

            currentCount++;
            EditorUtility.DisplayProgressBar("Patching GUIDs", $"Processing {assetPath}", (float)currentCount / totalCount);

            foreach (string dependency in dependencies)
            {
                string dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                if (!guidMapping.ContainsKey(dependencyGuid)) continue;

                string assetContent = File.ReadAllText(assetPath);
                string updatedContent = Regex.Replace(assetContent, @"guid:\s*([0-9a-fA-F]+)", match =>
                {
                    string foundGuid = match.Groups[1].Value;
                    return guidMapping.ContainsKey(foundGuid) ? $"guid: {guidMapping[foundGuid]}" : match.Value;
                });

                if (assetContent == updatedContent) continue;

                string tempFilePath = Path.Combine(Application.persistentDataPath, Path.GetFileName(assetPath));
                File.WriteAllText(tempFilePath, updatedContent);
                FileUtil.ReplaceFile(tempFilePath, assetPath);
            }
        }

        // Patch .meta files with original GUIDs
        currentCount = 0;
        totalCount = guidData.assetsGuids.Count;

        foreach (var assetGuid in guidData.assetsGuids)
        {
            string metaFilePath = assetGuid.assetPath + ".meta";
            if (File.Exists(metaFilePath))
            {
                string metaContent = File.ReadAllText(metaFilePath);
                metaContent = metaContent.Replace(AssetDatabase.AssetPathToGUID(assetGuid.assetPath), assetGuid.originalGuid);
                string tempMetaFilePath = Path.Combine(Application.persistentDataPath, Path.GetFileName(metaFilePath));
                File.WriteAllText(tempMetaFilePath, metaContent);
                FileUtil.ReplaceFile(tempMetaFilePath, metaFilePath);
            }

            currentCount++;
            float progress = (float)currentCount / totalCount;
            EditorUtility.DisplayProgressBar("Patching .meta files", $"Processing {metaFilePath}", progress);
        }

        AssetDatabase.Refresh();
        Debug.Log("GUIDs patched from JSON.");

        EditorUtility.ClearProgressBar();
    }
}

[System.Serializable]
public class GUIDData
{
    public List<GUIDEntry> guids = new List<GUIDEntry>();
    public List<AssetGUID> assetsGuids = new List<AssetGUID>();
}

[System.Serializable]
public class GUIDEntry
{
    public string assemblyName;
    public string fullTypeName;
    public string originalGuid;
}

[System.Serializable]
public class AssetGUID
{
    public string assetPath;
    public string originalGuid;
    public string fileId;
}
