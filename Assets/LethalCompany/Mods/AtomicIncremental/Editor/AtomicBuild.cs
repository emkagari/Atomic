using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

#if UNITY_EDITOR
public class AtomicTool : EditorWindow
{
    private AtomicSettings settings;
    private ReorderableList reorderableList;

    [MenuItem("Tools/Atomic")]
    public static void ShowWindow()
    {
        GetWindow<AtomicTool>("Atomic");
    }

    private void OnEnable()
    {
        LoadSettings();
        reorderableList = new ReorderableList(settings.installationPaths, typeof(string), true, false, true, true);
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            float verticalCenter = (rect.height - EditorGUIUtility.singleLineHeight) / 2;

            Rect textFieldRect = new Rect(rect.x, rect.y + verticalCenter, rect.width - 30, EditorGUIUtility.singleLineHeight);
    
            // EditorGUI.BeginDisabledGroup(true);
            EditorGUI.LabelField(textFieldRect, settings.installationPaths[index]);
            // EditorGUI.EndDisabledGroup();

            Rect buttonRect = new Rect(rect.x + rect.width - 25, rect.y + verticalCenter, 25, EditorGUIUtility.singleLineHeight);
            GUIContent iconContent = EditorGUIUtility.IconContent("Folder Icon");
            if (GUI.Button(buttonRect, iconContent))
            {
                string currentPath = settings.installationPaths[index];
                string path = EditorUtility.OpenFolderPanel("Select Installation Path", currentPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    settings.installationPaths[index] = NormalizePath(path);
                    SaveSettings();
                    Repaint();
                }
            }
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            // settings.InstallationPaths.Add();
            // Add a new path based on the last path in the list
            if (settings.installationPaths.Count > 0)
                settings.installationPaths.Add(NormalizePath(settings.installationPaths.Last()));
            else
                settings.installationPaths.Add(NormalizePath("./Assets"));
            SaveSettings();
        };

        reorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            settings.installationPaths.RemoveAt(list.index);
            SaveSettings();
        };
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Atomic Tool", new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.Load("console.warnicon") as Texture2D, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20));
        GUILayout.Label("This is the default build path, you shouldn't change it unless you know what you're doing", EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Path:", EditorStyles.boldLabel);
        settings.buildPath = EditorGUILayout.TextField(settings.buildPath, GUILayout.Width(position.width - EditorStyles.boldLabel.CalcSize(new GUIContent("Build Path:")).x - 20));
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.Label("Installation Paths");
        reorderableList.DoLayoutList();

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Build", GUILayout.MaxWidth(200))) BuildBundles();
        if (GUILayout.Button("Install Bundles", GUILayout.MaxWidth(200)))
            foreach (string path in settings.installationPaths)
                InstallBundles(path);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void BuildBundles()
    {
        CleanPath(settings.buildPath);
        
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (!string.IsNullOrEmpty(result.Error)) return;

        foreach (string path in settings.installationPaths)
            InstallBundles(path);
    }

    private string NormalizePath(string path)
    {
        string projectPath = Path.GetFullPath(Application.dataPath + "/../");
        string relativePath = Path.GetRelativePath(projectPath, path);
        return relativePath.Replace("\\", "/");
    }

    private void InstallBundles(string path)
    {
        string directory = Path.GetDirectoryName($"{path}/"); 
        Directory.CreateDirectory(directory);
        CleanPath(directory, "*.lethalbundle");
        
        foreach (string file in Directory.GetFiles(settings.buildPath, "*.bundle"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string destination = Path.Combine(directory, $"{fileName}.lethalbundle");

            try
            {
                File.Copy(file, destination, true);
                Debug.Log($"Copied bundle file to: {destination}");
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to copy bundle file: {e.Message}");
            }
        }
    }

    private void CleanPath(string path, string pattern = "*.bundle")
    {
        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"Path does not exist: {path}");
            return;
        }
    
        foreach (string file in Directory.GetFiles(path, pattern))
        {
            Debug.Log($"Deleting file: {file}");
            File.Delete(file);
        }
    }
    
    private void LoadSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:AtomicSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<AtomicSettings>(path);
        }

        if (settings == null)
        {
            settings = CreateInstance<AtomicSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/AtomicSettings.asset");
            AssetDatabase.SaveAssets();
        }
        for (int i = 0; i < settings.installationPaths.Count; i++)
            settings.installationPaths[i] = NormalizePath(settings.installationPaths[i]);
        SaveSettings();
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
#endif