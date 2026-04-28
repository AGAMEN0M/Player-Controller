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

namespace PlayerController.Editor
{
    /// <summary>
    /// Provides utility functions to instantiate and configure player-related prefabs through the Unity Editor.
    /// </summary>
    public static class PlayerPrefabManager
    {
        #region === Core Methods ===

        /// <summary>
        /// Instantiates a prefab by name and sets it up in the scene under a given parent object.
        /// Only prefabs located inside "Player Controller/Controllers" will be considered.
        /// </summary>
        /// <param name="prefabName">The name of the prefab to search and instantiate.</param>
        /// <param name="parentObject">The parent GameObject under which the prefab should be instantiated (optional).</param>
        private static void InstantiateAndSetupPrefab(string prefabName, GameObject parentObject)
        {
            // Search the project for prefabs with the specified name.
            var prefabGuids = AssetDatabase.FindAssets($"\"{prefabName}\" t:Prefab");

            // If no prefab was found, log an error and exit.
            if (prefabGuids.Length == 0)
            {
                Debug.LogError($"Prefab '{prefabName}' not found in the project.");
                return;
            }

            string validPrefabPath = null;

            // Iterate through all found prefabs and filter by path.
            foreach (var guid in prefabGuids)
            {
                // Convert GUID to asset path.
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Check if the path contains the required folder structure.
                if (path.Contains("Player Controller/Controllers"))
                {
                    validPrefabPath = path;
                    break; // Stop at the first valid match.
                }
            }

            // If no valid prefab was found in the required folder, log an error.
            if (string.IsNullOrEmpty(validPrefabPath))
            {
                Debug.LogError($"Prefab '{prefabName}' was found, but not inside 'Player Controller/Controllers'.");
                return;
            }

            // If multiple prefabs exist, warn the user (optional but useful).
            if (prefabGuids.Length > 1)
            {
                Debug.LogWarning($"Multiple prefabs found with name '{prefabName}'. Using filtered path: {validPrefabPath}");
            }

            // Load the prefab asset from the validated path.
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(validPrefabPath);

            // Validate loading result.
            if (prefabAsset == null)
            {
                Debug.LogError($"Failed to load prefab at path: {validPrefabPath}");
                return;
            }

            // Determine the parent transform.
            var parentTransform = parentObject != null ? parentObject.transform : null;

            // Instantiate the prefab into the scene.
            var instantiatedObject = PrefabUtility.InstantiatePrefab(prefabAsset, parentTransform) as GameObject;

            // Complete setup.
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
        [MenuItem("GameObject/Tools/Player Controller/3D/Base Player")]
        public static void CreateBasePlayer() => InstantiateAndSetupPrefab("Player Test (Base) [3D]", Selection.activeGameObject);

        /// <summary>
        /// Instantiates the "Side View 3D" player prefab under the currently selected object in the editor.
        /// </summary>
        [MenuItem("GameObject/Tools/Player Controller/3D/Side View")]
        public static void CreateBasePlayerSideView() => InstantiateAndSetupPrefab("Player Test (Side View) [3D]", Selection.activeGameObject);

        /// <summary>
        /// Instantiates the "First Person 3D" player prefab under the currently selected object in the editor.
        /// </summary>
        [MenuItem("GameObject/Tools/Player Controller/3D/First Person")]
        public static void CreateBasePlayerFirstPerson() => InstantiateAndSetupPrefab("Player Test (First Person) [3D]", Selection.activeGameObject);

        /// <summary>
        /// Instantiates the "Third Person 3D" player prefab under the currently selected object in the editor.
        /// </summary>
        [MenuItem("GameObject/Tools/Player Controller/3D/Third Person/Player Controller")]
        public static void CreateBasePlayerThirdPerson() => InstantiateAndSetupPrefab("Player Test (Third Person) [3D]", Selection.activeGameObject);

        /// <summary>
        /// Instantiates the "Third Person 3D" camera controller prefab under the currently selected object in the editor.
        /// </summary>
        [MenuItem("GameObject/Tools/Player Controller/3D/Third Person/Camera Controller")]
        public static void CreateBasePlayerThirdPersonCameraController() => InstantiateAndSetupPrefab("Camera Controller (Third Person) [3D]", Selection.activeGameObject);

        #endregion
    }
}