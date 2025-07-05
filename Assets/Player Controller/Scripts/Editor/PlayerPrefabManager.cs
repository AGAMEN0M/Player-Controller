/*
 * ---------------------------------------------------------------------------
 * Description: Utility class that provides editor menu options to instantiate 
 *              and configure specific player prefabs directly in the Unity hierarchy. 
 *              Automatically unpacks the prefab, selects it, and triggers rename mode.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;
using UnityEditor;

/// <summary>
/// Provides utility functions to instantiate and configure player-related prefabs through the Unity Editor.
/// </summary>
public static class PlayerPrefabManager
{
    /// <summary>
    /// Instantiates a prefab by name and sets it up in the scene under a given parent object.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to search and instantiate.</param>
    /// <param name="parentObject">The parent GameObject under which the prefab should be instantiated (optional).</param>
    private static void InstantiateAndSetupPrefab(string prefabName, GameObject parentObject)
    {
        // Search the project for prefabs with the specified name.
        string[] prefabGuids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");

        // If no prefab was found, log an error and exit.
        if (prefabGuids.Length == 0)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in the project.");
            return;
        }

        // Convert the first GUID found into a path to the prefab asset.
        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);

        // Load the prefab asset at the found path.
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"Prefab not found: {prefabName}.prefab. Please check if the prefab exists in the project and the name is correct.");
            return;
        }

        // Determine the parent transform, if a parent object was provided.
        Transform parentTransform = parentObject != null ? parentObject.transform : null;

        // Instantiate the prefab into the scene under the parent.
        GameObject instantiatedObject = PrefabUtility.InstantiatePrefab(prefabAsset, parentTransform) as GameObject;

        // Proceed to complete the prefab setup process.
        CompletePrefabSetup(prefabName, instantiatedObject);
    }

    /// <summary>
    /// Finalizes the setup of the instantiated prefab in the scene.
    /// </summary>
    /// <param name="prefabName">The name of the instantiated prefab.</param>
    /// <param name="instantiatedObject">The instance of the prefab that was created.</param>
    private static void CompletePrefabSetup(string prefabName, GameObject instantiatedObject)
    {
        // If the prefab wasn't instantiated properly, exit.
        if (instantiatedObject == null) return;

        // Register the created object with Unity's undo system for editor undo functionality.
        Undo.RegisterCreatedObjectUndo(instantiatedObject, $"Create {prefabName}");

        // Completely unpack the prefab instance to allow full modification in the scene.
        PrefabUtility.UnpackPrefabInstance(instantiatedObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // Select the newly created object in the editor.
        Selection.activeGameObject = instantiatedObject;

        // Delay renaming to the next editor frame to ensure selection is active.
        EditorApplication.delayCall += () =>
        {
            if (Selection.activeGameObject == instantiatedObject)
            {
                // Simulate F2 key press to trigger rename mode in the hierarchy.
                EditorWindow.focusedWindow.SendEvent(new Event { keyCode = KeyCode.F2, type = EventType.KeyDown });
            }
        };
    }

    /// <summary>
    /// Instantiates the "Base Player 3D" prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Base Player 3D", false, 1)]
    public static void CreateBasePlayer()
    {
        InstantiateAndSetupPrefab("Player Test (Base) [3D]", Selection.activeGameObject);
    }

    /// <summary>
    /// Instantiates the "Side View 3D" player prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Side View 3D", false, 2)]
    public static void CreateBasePlayerSideView()
    {
        InstantiateAndSetupPrefab("Player Test (Side View) [3D]", Selection.activeGameObject);
    }
}