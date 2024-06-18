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
                    settings.installationPaths[index] = path;
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
                settings.installationPaths.Add(settings.installationPaths.Last());
            else
                settings.installationPaths.Add("");
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
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        GUILayout.Space(10);
        GUILayout.Label("Atomic Tool", titleStyle);
        GUILayout.Space(10);

        // Display the build path with a warning, like this is a default value, you shouldn't change it unless you know what you're doing
        
        Texture2D warningIcon = EditorGUIUtility.Load("console.warnicon") as Texture2D;

        GUILayout.BeginHorizontal();
        GUILayout.Label(warningIcon, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20));
        GUILayout.Label("This is the default build path, you shouldn't change it unless you know what you're doing", EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Path:", EditorStyles.boldLabel);
        float labelWidth = EditorStyles.boldLabel.CalcSize(new GUIContent("Build Path:")).x;
        settings.buildPath = EditorGUILayout.TextField(settings.buildPath, GUILayout.Width(position.width - labelWidth - 20));
        GUILayout.EndHorizontal();
        
        
        GUILayout.Space(20);
        // Display the installation paths list, and a button to add a new path
        GUILayout.Label("Installation Paths");
        reorderableList.DoLayoutList();
        
        GUILayout.Space(15);
        
        // Display the addressable assets group
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // Add flexible space before the button
        if (GUILayout.Button("Build", GUILayout.MaxWidth(200))) BuildBundles();
        if (GUILayout.Button("Install Bundles", GUILayout.MaxWidth(200)))
            foreach (string path in settings.installationPaths)
                InstallBundles(path);
        GUILayout.FlexibleSpace(); // Add flexible space after the button
        GUILayout.EndHorizontal();
    }

    private void BuildBundles()
    {
        CleanPath(settings.buildPath);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if (!success) return;
    
        if (settings.installationPaths.Count == 0) return;

        foreach (string path in settings.installationPaths)
            InstallBundles(path);
    }

    private void InstallBundles(string path)
    {
        string directory = System.IO.Path.GetDirectoryName($"{path}/"); 
        
        string[] files = System.IO.Directory.GetFiles(settings.buildPath, "*.bundle");
        if (files.Length == 0)
        {
            Debug.LogWarning($"No bundle files found in build path: {settings.buildPath}");
            return;
        }
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        CleanPath(directory, "*.lethalbundle");
        
        foreach (string file in files)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
            string destination = System.IO.Path.Combine(directory, $"{fileName}.lethalbundle");

            try
            {
                System.IO.File.Copy(file, destination, true);
                Debug.Log($"Copied bundle file to: {destination}");
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError($"Failed to copy bundle file: {e.Message}");
            }
        }
    }

    private void CleanPath(string path, string pattern = "*.bundle")
    {
        System.IO.Directory.GetFiles(path, pattern).ToList().ForEach(f =>
            {
                Debug.Log("Deleting file: " + f);
                System.IO.File.Delete(f);
            }
        );
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
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
#endif