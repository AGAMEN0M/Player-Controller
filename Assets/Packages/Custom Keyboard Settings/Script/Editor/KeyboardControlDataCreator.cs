/*
 * ---------------------------------------------------------------------------
 * Description: This script provides functionality to create and manage custom
 *              Keyboard Control Data assets within the Unity Editor.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEditor;
using UnityEngine;
using System.IO;

// Class for creating a custom asset in Unity.
public class KeyboardControlDataCreator
{
    // Menu item for creating Keyboard Control Data asset.
    [MenuItem("Assets/Create/Custom Keyboard Settings/Keyboard Control Data")]
    public static void CreateCustomObjectData()
    {
        // Specify the path where the asset will be saved.
        string path = "Assets/Resources";
        string assetPath = $"{path}/Keyboard Control Data.asset";

        // Check if the Resources folder exists; if not, create it.
        if (!AssetDatabase.IsValidFolder(path)) { AssetDatabase.CreateFolder("Assets", "Resources"); }

        // Check if an asset with the same name already exists.
        if (AssetDatabase.LoadAssetAtPath<KeyboardControlData>(assetPath) != null)
        {
            // Display a dialog to confirm replacing the existing asset.
            if (!EditorUtility.DisplayDialog("Replace File", "There is already a 'Keyboard Control Data'. Do you want to replace it?", "Yes", "No"))
            {
                return;
            }
        }

        // Create an instance of KeyboardControlData and save it as an asset.
        KeyboardControlData asset = ScriptableObject.CreateInstance<KeyboardControlData>();
        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Focus on the Project window in the Unity Editor and select the created asset.
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}

[InitializeOnLoad]
public static class KeyboardControlDataCreatorStartup
{
    static KeyboardControlDataCreatorStartup()
    {
        EditorApplication.delayCall += () =>
        {
            // Specify the path where the asset should be checked.
            string assetPath = "Assets/Resources/Keyboard Control Data.asset";

            // Check if the asset already exists; if not, create it.
            if (!File.Exists(assetPath))
            {
                KeyboardControlDataCreator.CreateCustomObjectData();
            }
        };
    }
}