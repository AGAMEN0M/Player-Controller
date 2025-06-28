/*
 * ---------------------------------------------------------------------------
 * Description: Custom property drawer for Unity that provides a dropdown 
 *              list of keyboard tags for string properties. The dropdown 
 *              is populated with tags from the KeyboardControlData. If the 
 *              current value of the string property does not match any of the 
 *              available tags, a warning message is displayed below the dropdown.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using CustomKeyboard;
using UnityEditor;
#endif

public class KeyboardTagDropdownAttribute : PropertyAttribute
{
    // This attribute is just a marker, it does not need additional implementation.
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(KeyboardTagDropdownAttribute))]
public class KeyboardTagDropdownDrawer : PropertyDrawer
{
    /// <summary>
    /// Draws the property field in the Inspector with a dropdown populated by keyboard tags.
    /// Displays a warning if the current string value does not match any available tag.
    /// </summary>
    /// <param name="position">The position in the Inspector to draw the property.</param>
    /// <param name="property">The serialized property being drawn.</param>
    /// <param name="label">The label of the property field.</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Make sure the property is of type string.
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [KeyboardTagDropdown] with strings.");
            return;
        }

        // Get the list of keyboard tags using the helper.
        KeyboardControlData keyboardControlData = KeyboardTagHelper.GetKeyboardControlData();
        if (keyboardControlData == null)
        {
            EditorGUI.LabelField(position, label.text, "Could not load KeyboardControlData.");
            return;
        }

        // Get all keyboard tags from InputData.
        List<string> keyboardTags = new();
        foreach (var inputData in keyboardControlData.inputDataList)
        {
            keyboardTags.Add(inputData.keyboardTag);
        }

        // Add a "Missing Tag" option with the actual text string to the dropdown if needed.
        string currentString = property.stringValue;
        string currentText = string.IsNullOrEmpty(currentString) ? "Missing Tag" : $"Missing Tag ({currentString})";
        string missingTagText = keyboardTags.Contains(currentString) ? "" : currentText;
        keyboardTags.Insert(0, missingTagText);

        // Find the index of the current property value in the list of tags.
        int currentIndex = keyboardTags.IndexOf(currentString);
        if (currentIndex == -1) currentIndex = 0; // If the current value is not in the list, select "Missing Tag".

        // Display the dropdown in the Inspector.
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, keyboardTags.ToArray());

        // If the selected option is not "Missing Tag", update the string with the new selection.
        if (newIndex != 0) property.stringValue = keyboardTags[newIndex];

        // Show a warning if "Missing Tag" is selected.
        if (newIndex == 0)
        {
            // Define the rectangle for the HelpBox below the dropdown field and adjust its height.
            Rect helpBoxRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight * 2);
            EditorGUI.HelpBox(helpBoxRect, "String value does not match any keyboardTag!", MessageType.Warning);
        }
    }

    /// <summary>
    /// Calculates the required height for the property field, including space for a warning if needed.
    /// </summary>
    /// <param name="property">The serialized property.</param>
    /// <param name="label">The label of the property field.</param>
    /// <returns>The height needed to draw the property and any additional UI.</returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // If the string value does not match any tag, add space to the warning.
        KeyboardControlData keyboardControlData = KeyboardTagHelper.GetKeyboardControlData();
        if (keyboardControlData != null)
        {
            List<string> keyboardTags = new();
            foreach (var inputData in keyboardControlData.inputDataList)
            {
                keyboardTags.Add(inputData.keyboardTag);
            }

            if (!keyboardTags.Contains(property.stringValue))
            {
                return EditorGUIUtility.singleLineHeight * 3; // Adds extra height.
            }
        }

        return EditorGUIUtility.singleLineHeight;
    }
}
#endif