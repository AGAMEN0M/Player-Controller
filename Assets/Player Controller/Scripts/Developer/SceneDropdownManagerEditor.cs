/*
 * ---------------------------------------------------------------------------
 * Description: Dropdown scene selector that loads scenes using a ScriptableObject,
 *              without using Build Settings. Works in Editor and Play Mode.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PlayerController.Attributes;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using TMPro;
#endif

namespace PlayerController.Developer
{
    /// <summary>
    /// Component responsible for listing scenes in a dropdown UI and loading the selected one.
    /// It uses a ScriptableObject reference to store scene assets instead of relying on Build Settings.
    /// Intended for use within the Unity Editor only.
    /// </summary>
    [AddComponentMenu("Tools/Player Controller/Developer/Scene Dropdown Manager (Editor)")]
    public class SceneDropdownManagerEditor : MonoBehaviour
    {
    #if UNITY_EDITOR

        #region === Serialized Fields ===

        [Header("UI References")]
        [SerializeField, ValidateReference, Tooltip("Reference to the TMP_Dropdown component that lists available scenes.")]
        private TMP_Dropdown sceneDropdown;

        [SerializeField, ValidateReference, Tooltip("Reference to the Button component that triggers the scene loading process.")]
        private Button loadSceneButton;

        [Header("Scene Settings")]
        [SerializeField, ValidateReference, Tooltip("Reference to ScriptableObject that contains scene list.")]
        private SceneListEditor sceneList;

        #endregion

        #region === Private Fields ===

        /// <summary>
        /// Internal list of scene paths derived from SceneAssets.
        /// </summary>
        private readonly List<string> scenePaths = new();

        #endregion

        #region === Public Properties ===

        /// <summary>
        /// Gets or sets the TMP_Dropdown used to display scene options.
        /// </summary>
        public TMP_Dropdown SceneDropdown
        {
            get => sceneDropdown;
            set => sceneDropdown = value;
        }

        /// <summary>
        /// Gets or sets the button used to trigger the scene loading.
        /// </summary>
        public Button LoadSceneButton
        {
            get => loadSceneButton;
            set => loadSceneButton = value;
        }

        /// <summary>
        /// Gets or sets the SceneList ScriptableObject reference.
        /// </summary>
        public SceneListEditor SceneList
        {
            get => sceneList;
            set => sceneList = value;
        }

        #endregion

        #region === Unity Callbacks ===

        /// <summary>
        /// Called on scene load. Initializes the dropdown and registers button callback.
        /// </summary>
        private void Start()
        {
            // Validate the SceneList SO before proceeding.
            if (sceneList == null || sceneList.scenes == null || sceneList.scenes.Count == 0)
            {
                Debug.LogWarning("SceneList is not assigned or contains no scenes.", this);
                return;
            }

            SetupDropdown(); // Populate dropdown options from SceneList.
            loadSceneButton.onClick.AddListener(OnLoadSceneButtonClicked); // Register button event.
        }

        #endregion

        #region === Private Methods ===

        /// <summary>
        /// Sets up dropdown options from SceneList ScriptableObject.
        /// Converts SceneAssets into scene names and paths.
        /// Automatically selects the current open scene in the dropdown.
        /// </summary>
        private void SetupDropdown()
        {
            sceneDropdown.ClearOptions(); // Clear any previous options.
            scenePaths.Clear(); // Clear stored scene paths.

            List<string> options = new(); // List of scene names for the dropdown.

            // Get the name of the current active scene in the Editor.
            string currentSceneName = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path);

            int defaultIndex = 0; // Index to be selected by default.
            int index = 0;

            // Loop through each SceneAsset stored in the ScriptableObject.
            foreach (var sceneAsset in sceneList.scenes)
            {
                if (sceneAsset == null) continue;

                // Get the full asset path of the scene.
                string path = AssetDatabase.GetAssetPath(sceneAsset);

                // Extract the file name without extension to use as label.
                string sceneName = Path.GetFileNameWithoutExtension(path);

                options.Add(sceneName); // Add scene name to dropdown options.
                scenePaths.Add(path);   // Add path to internal scenePaths list.

                // Check if this scene matches the currently open scene.
                if (sceneName == currentSceneName) defaultIndex = index;

                index++;
            }

            sceneDropdown.AddOptions(options); // Apply options to dropdown.
            sceneDropdown.value = defaultIndex; // Select the current scene by default.
            sceneDropdown.RefreshShownValue(); // Ensure UI shows updated selection.
        }

        /// <summary>
        /// Called when the Load Scene button is clicked.
        /// Loads the selected scene based on the dropdown selection.
        /// </summary>
        private void OnLoadSceneButtonClicked()
        {
            // Validate the selected index.
            if (sceneDropdown.value < 0 || sceneDropdown.value >= scenePaths.Count)
            {
                Debug.LogWarning("Invalid scene selected.", this);
                return;
            }

            // Get the path of the selected scene.
            string scenePath = scenePaths[sceneDropdown.value];

            // Load the scene depending on current editor mode.
            if (EditorApplication.isPlaying)
            {
                // In Play Mode, load the scene asynchronously using PlayMode-compatible API.
                EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            }
            else
            {
                // In Edit Mode, open the scene in the editor.
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        #endregion

    #endif
    }
}