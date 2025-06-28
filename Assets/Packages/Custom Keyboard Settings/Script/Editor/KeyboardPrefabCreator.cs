/*
 * ---------------------------------------------------------------------------
 * Description: This script provides tools for creating and configuring keyboard-related
 *              UI prefab managers in Unity's scene hierarchy. It supports both legacy 
 *              and TMP (TextMeshPro) prefabs, automatically handling parent assignment, 
 *              Canvas creation (if necessary), and prefab unpacking for further customization. 
 *              Designed for seamless integration into Unity Editor with custom menu options 
 *              under `GameObject/UI`.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class KeyboardPrefabCreator
{
    [MenuItem("GameObject/UI/Keyboard Settings Manager (Legacy)")]
    public static void CreateAudioSourcePrefab()
    {
        CreateAndConfigurePrefab("Keyboard Settings Manager (Legacy)", Selection.activeGameObject, true);
    }

    [MenuItem("GameObject/UI/Keyboard Settings Manager (TMP)")]
    public static void CreateLanguageFilePrefab()
    {
        CreateAndConfigurePrefab("Keyboard Settings Manager (TMP)", Selection.activeGameObject, true);
    }

    // Creates and configures a prefab based on the specified file name and parent GameObject.
    #pragma warning disable IDE0079
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Correctness", "UNT0007:Null coalescing on Unity objects", Justification = "<Pendente>")]
    #pragma warning restore IDE0079
    private static void CreateAndConfigurePrefab(string fileName, GameObject selectedGameObject, bool isUI = false)
    {
        Canvas canvasObject = null; // Initialize canvasObject for UI prefabs.
        if (isUI)
        {
            canvasObject = UnityEngine.Object.FindAnyObjectByType<Canvas>() ?? CreateUICanvas(); // Find an existing Canvas or create a new one.
        }

        // Find the original prefab by its name.
        GameObject originalPrefab = FindPrefabByName(fileName);
        if (originalPrefab == null)
        {
            Debug.LogError($"Prefab not found: {fileName}.prefab. Please check if the prefab exists in the project and the name is correct.");
            return; // Exit if the prefab is not found.
        }

        // Determine the parent transform for the new GameObject based on the provided arguments.
        Transform parentTransform = selectedGameObject != null ? selectedGameObject.transform : (isUI ? canvasObject.transform : null);
        // Instantiate the prefab as a child of the determined parent transform.
        GameObject newGameObject = PrefabUtility.InstantiatePrefab(originalPrefab, parentTransform) as GameObject;

        FinalizePrefabSetup(fileName, newGameObject); // Finalize the setup for the newly created prefab.
    }

    // Creates a new UI Canvas object with necessary components.
    private static Canvas CreateUICanvas()
    {
        GameObject newCanvasObject = new("Canvas"); // Create a new GameObject for the Canvas.
        var canvasObject = newCanvasObject.AddComponent<Canvas>(); // Add the Canvas component to the GameObject.

        // Add CanvasScaler and GraphicRaycaster components for UI scaling and event handling.
        newCanvasObject.AddComponent<CanvasScaler>();
        newCanvasObject.AddComponent<GraphicRaycaster>();

        canvasObject.renderMode = RenderMode.ScreenSpaceOverlay; // Set the render mode to ScreenSpaceOverlay for standard UI display.
        canvasObject.gameObject.layer = LayerMask.NameToLayer("UI"); // Set the layer to UI for proper rendering.

        // Set sorting order for UI elements.
        canvasObject.sortingOrder = 0;
        canvasObject.targetDisplay = 0;

        // Create an Event System for managing input events in the UI.
        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        // Register the creation of the Canvas and Event System objects for Undo functionality.
        Undo.RegisterCreatedObjectUndo(newCanvasObject, "Create Canvas");
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");

        return canvasObject; // Return the created Canvas.
    }

    /// <summary>
    /// Finds and returns a prefab by its name from the project assets.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to search for.</param>
    /// <returns>The prefab GameObject if found; otherwise, null.</returns>
    public static GameObject FindPrefabByName(string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(prefabPath);

            if (fileNameWithoutExtension.Equals(prefabName, StringComparison.OrdinalIgnoreCase))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                return prefab;
            }
        }

        Debug.LogError($"Prefab with the name '{prefabName}' not found.");
        return null;
    }

    // Finalizes the setup for the newly created prefab.
    private static void FinalizePrefabSetup(string fileName, GameObject newGameObject)
    {
        if (newGameObject == null) return; // Exit if the new GameObject is null.

        Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {fileName}"); // Register the new GameObject for Undo functionality.

        // Unpack the prefab instance to allow for modifications.
        PrefabUtility.UnpackPrefabInstance(newGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Selection.activeGameObject = newGameObject; // Select the new GameObject in the hierarchy.

        // Delay the focus event to allow for user interaction after creation.
        EditorApplication.delayCall += () =>
        {
            if (Selection.activeGameObject == newGameObject)
            {
                // Trigger the rename event for the new GameObject.
                EditorWindow.focusedWindow.SendEvent(new Event { keyCode = KeyCode.F2, type = EventType.KeyDown });
            }
        };
    }
}