/*
 * ---------------------------------------------------------------------------
 * Description: A utility class for managing and persisting keyboard control data 
 *              within a Unity project. It handles retrieval, updating, and saving 
 *              of InputData associated with specific keyboard tags using PlayerPrefs.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;

namespace CustomKeyboard
{
    // Helper class for managing keyboard control data.
    public static class KeyboardTagHelper
    {
        /// <summary>
        /// Retrieves the KeyboardControlData resource from the Resources folder.
        /// </summary>
        /// <returns>The loaded KeyboardControlData instance or null if not found.</returns>
        public static KeyboardControlData GetKeyboardControlData()
        {
            KeyboardControlData keyboardControlData = Resources.Load<KeyboardControlData>("Keyboard Control Data");

            if (keyboardControlData == null)
            {
                Debug.LogError("Failed to load KeyboardControlData from Resources. Ensure the resource exists and is named correctly.");
                return null;
            }

            return keyboardControlData;
        }

        /// <summary>
        /// Retrieves the InputData associated with a specific keyboardTag.
        /// </summary>
        /// <param name="tag">The keyboard tag to search for.</param>
        /// <returns>The matching InputData or null if not found.</returns>
        public static InputData GetInputFromTag(string tag)
        {
            KeyboardControlData keyboardControlData = GetKeyboardControlData();
            if (keyboardControlData == null)
            {
                Debug.LogError("KeyboardControlData is null.");
                return null;
            }

            // Search for InputData with the specified keyboardTag.
            foreach (var inputData in keyboardControlData.inputDataList)
            {
                if (inputData.keyboardTag == tag)
                {
                    return inputData; // Return the InputData that matches the tag.
                }
            }

            return null; // Return null if no matching InputData is found.
        }

        /// <summary>
        /// Updates the KeyCode for the specified InputData.
        /// </summary>
        /// <param name="inputData">The InputData to update.</param>
        /// <param name="newKeyCode">The new KeyCode to assign.</param>
        public static void SetKey(InputData inputData, KeyCode newKeyCode)
        {
            if (inputData == null)
            {
                Debug.LogError("InputData is null.");
                return;
            }

            inputData.keyboard = newKeyCode;
        }

        /// <summary>
        /// Updates the KeyCode for the InputData associated with the specified tag.
        /// </summary>
        /// <param name="tag">The keyboard tag of the InputData to update.</param>
        /// <param name="newKeyCode">The new KeyCode to assign.</param>
        public static void SetKeyFromTag(string tag, KeyCode newKeyCode)
        {
            InputData inputData = GetInputFromTag(tag);
            if (inputData == null)
            {
                Debug.LogError($"No InputData found with tag '{tag}'.");
                return;
            }

            inputData.keyboard = newKeyCode;
        }

        /// <summary>
        /// Retrieves the sprite associated with a given KeyCode.
        /// </summary>
        /// <param name="keyCode">The KeyCode to look up.</param>
        /// <returns>The corresponding sprite, or the default sprite if not found.</returns>
        public static Sprite GetKeySprite(KeyCode keyCode)
        {
            KeyboardControlData keyboardControlData = GetKeyboardControlData();
            if (keyboardControlData == null)
            {
                Debug.LogError("KeyboardControlData is null.");
                return null;
            }

            // Look for the sprite associated with the KeyCode
            foreach (var spriteList in keyboardControlData.keyCodesSprites)
            {
                if (spriteList.keyCode == keyCode)
                {
                    return spriteList.sprite;
                }
            }

            // Return default sprite if no specific sprite is found.
            return keyboardControlData.defaultSprite;
        }

        /// <summary>
        /// Saves the current keyboard control data to PlayerPrefs.
        /// </summary>
        public static void SaveKeyboardControlData()
        {
            KeyboardControlData keyboardControlData = GetKeyboardControlData();
            if (keyboardControlData == null)
            {
                Debug.LogError("KeyboardControlData is null.");
                return;
            }

            // Prepare a data model for saving.
            KeyboardControlDataSave data = new()
            {
                inputDataListSaves = new List<InputDataListSave>()
            };

            // Convert each InputData into InputDataListSave and add to the list.
            foreach (var inputData in keyboardControlData.inputDataList)
            {
                data.inputDataListSaves.Add(new InputDataListSave
                {
                    keyboardTag = inputData.keyboardTag,
                    keyboard = inputData.keyboard
                });
            }

            // Serialize the data to JSON and save it to PlayerPrefs.
            string jsonData = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("Keyboard Control Data", jsonData);
        }

        /// <summary>
        /// Loads keyboard control data from PlayerPrefs at runtime and updates the current configuration.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void LoadKeyboardControlData()
        {
            // Check if PlayerPrefs contains saved keyboard control data.
            if (PlayerPrefs.HasKey("Keyboard Control Data"))
            {
                // Retrieve and deserialize the JSON data from PlayerPrefs.
                string jsonData = PlayerPrefs.GetString("Keyboard Control Data");
                var data = JsonUtility.FromJson<KeyboardControlDataSave>(jsonData);

                // Obtain the KeyboardControlData instance.
                KeyboardControlData keyboardControlData = GetKeyboardControlData();
                if (keyboardControlData == null)
                {
                    Debug.LogError("Could not find KeyboardControlData with name 'Keyboard Control Data'");
                    return;
                }

                // Update the KeyboardControlData with the loaded data.
                foreach (var inputDataSave in data.inputDataListSaves)
                {
                    // Find the existing InputData by keyboardTag.
                    InputData existingInputData = GetInputFromTag(inputDataSave.keyboardTag);
                    if (existingInputData != null)
                    {
                        // Update the existing InputData with the saved KeyCode.
                        existingInputData.keyboard = inputDataSave.keyboard;
                    }
                    else
                    {
                        Debug.LogError($"InputData with tag '{inputDataSave.keyboardTag}' not found.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Serializable class for storing saved keyboard control data.
    /// </summary>
    [System.Serializable]
    public class KeyboardControlDataSave
    {
        public List<InputDataListSave> inputDataListSaves; // List of saved InputData objects.
    }

    /// <summary>
    /// Serializable class for storing individual InputData for saving.
    /// </summary>
    [System.Serializable]
    public class InputDataListSave
    {
        public string keyboardTag; // Tag associated with the keyboard input.
        public KeyCode keyboard; // KeyCode for the keyboard input.
    }

    /// <summary>
    /// Represents a visual sprite mapping for a specific keyboard key.
    /// </summary>
    [System.Serializable]
    public class InputSpriteList
    {
        public KeyCode keyCode; // The KeyCode associated with this sprite. 
        public Sprite sprite; // The Sprite that represents the key visually.
    }
}