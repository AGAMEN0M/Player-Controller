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
    #region === Core Methods ===

    /// <summary>
    /// Instantiates a prefab by name and sets it up in the scene under a given parent object.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to search and instantiate.</param>
    /// <param name="parentObject">The parent GameObject under which the prefab should be instantiated (optional).</param>
    private static void InstantiateAndSetupPrefab(string prefabName, GameObject parentObject)
    {
        // Search the project for prefabs with the specified name.
        string[] prefabGuids = AssetDatabase.FindAssets($"\"{prefabName}\" t:Prefab");

        // If no prefab was found, log an error and exit.
        if (prefabGuids.Length == 0)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in the project.");
            return;
        }

        // Convert the first GUID found into a path to the prefab asset.
        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);

        // If multiple prefabs with the same name are found, log a warning and use the first one found.
        if (prefabGuids.Length > 1)
        {
            Debug.LogWarning($"Multiple prefabs found with name '{prefabName}'. Using the first found: {prefabPath}");
        }

        // Load the prefab asset at the found path.
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"Prefab not found: {prefabName}.prefab. Please check if the prefab exists in the project and the name is correct.");
            return;
        }

        // Determine the parent transform, if a parent object was provided.
        var parentTransform = parentObject != null ? parentObject.transform : null;

        // Instantiate the prefab into the scene under the parent.
        var instantiatedObject = PrefabUtility.InstantiatePrefab(prefabAsset, parentTransform) as GameObject;

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

    #endregion

    #region === Menu Items ===

    /// <summary>
    /// Instantiates the "Base Player 3D" prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Base Player", false, 1)]
    public static void CreateBasePlayer()
    {
        InstantiateAndSetupPrefab("Player Test (Base) [3D]", Selection.activeGameObject);
    }

    /// <summary>
    /// Instantiates the "Side View 3D" player prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Side View", false, 2)]
    public static void CreateBasePlayerSideView()
    {
        InstantiateAndSetupPrefab("Player Test (Side View) [3D]", Selection.activeGameObject);
    }

    /// <summary>
    /// Instantiates the "First Person 3D" player prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/First Person", false, 3)]
    public static void CreateBasePlayerFirstPerson()
    {
        InstantiateAndSetupPrefab("Player Test (First Person) [3D]", Selection.activeGameObject);
    }

    /// <summary>
    /// Instantiates the "Third Person 3D" player prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Third Person/Player Controller", false, 4)]
    public static void CreateBasePlayerThirdPerson()
    {
        InstantiateAndSetupPrefab("Player Test (Third Person) [3D]", Selection.activeGameObject);
    }

    /// <summary>
    /// Instantiates the "Third Person 3D" camera controller prefab under the currently selected object in the editor.
    /// </summary>
    [MenuItem("GameObject/Player Controller/3D/Third Person/Camera Controller", false, 5)]
    public static void CreateBasePlayerThirdPersonCameraController()
    {
        InstantiateAndSetupPrefab("Camera Controller (Third Person) [3D]", Selection.activeGameObject);
    }

    #endregion
}