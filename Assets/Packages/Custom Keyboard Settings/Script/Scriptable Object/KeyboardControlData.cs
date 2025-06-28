/*
 * ---------------------------------------------------------------------------
 * Description: Manages and provides access to keyboard control configurations 
 *              through a ScriptableObject that holds a list of InputData objects. 
 *              This class allows for the customization of keyboard inputs and 
 *              associated sprite representations for key codes. It also provides 
 *              an interface to modify and access these configurations in the Unity Editor.
 * Author: Lucas Gomes Cecchini.
 * Pseudonym: AGAMENOM.
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using CustomKeyboard;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

/*[CreateAssetMenu(fileName = "KeyboardControlData", menuName = "ScriptableObjects/KeyboardControlData", order = 2)]*/
// Class to manage a list of InputData objects.
// This is a ScriptableObject that contains a list of InputData, used for keyboard control configurations.
public class KeyboardControlData : ScriptableObject
{
    public List<InputData> inputDataList; // List of InputData objects, each representing a keyboard input configuration.
    public Sprite defaultSprite; // Default sprite used for the keyboard configuration.
    public List<InputSpriteList> keyCodesSprites; // List of sprites associated with specific key codes.
}

#if UNITY_EDITOR
public class KeyboardControlDataEditor
{
    [MenuItem("Window/Custom Keyboard Settings/Keyboard Control Data")]
    public static void OpenKeyboardSettingsData()
    {
        // Retrieve the KeyboardControlData object using a custom tool or method.
        ScriptableObject settingsData = KeyboardTagHelper.GetKeyboardControlData();

        // If the settings data is found, open it in Unity's Property Editor.
        if (settingsData != null)
        {
            // Attempt to find an existing open window of the Property Editor.
            EditorWindow existingWindow = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(window => window.titleContent.text == settingsData.name);

            if (existingWindow != null)
            {
                existingWindow.Focus(); // If the window is already open, focus on it.
            }
            else
            {
                EditorUtility.OpenPropertyEditor(settingsData); // If not, open a new Property Editor window.
            }
        }
        else
        {
            Debug.LogError("Failed to find or load KeyboardControlData. Ensure that the ScriptableObject exists and is properly referenced.");
        }
    }
}
#endif