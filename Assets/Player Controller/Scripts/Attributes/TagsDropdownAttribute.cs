/*
 * ---------------------------------------------------------------------------
 * Description: Custom attribute and property drawer for displaying Unity's 
 *              tag list as a dropdown in the Inspector. Useful for string fields that 
 *              should reference existing tags.
 * 
 * Using: [TagsDropdown]
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

#region === Attribute Definition ===

/// <summary>
/// Attribute to display a dropdown of Unity tags in the Inspector.
/// </summary>
public class TagsDropdownAttribute : PropertyAttribute { }

#endregion

#if UNITY_EDITOR

#region === Property Drawer ===

/// <summary>
/// Custom property drawer for the TagsDropdownAttribute.
/// Displays a dropdown list of Unity tags in the Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(TagsDropdownAttribute))]
public class TagsDropdownDrawer : PropertyDrawer
{
    /// <summary>
    /// Draws the property field in the Inspector with a dropdown of Unity tags.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Make sure the property is of type string.
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [TagsDropdown] with strings.");
            return;
        }

        // Get the list of Unity tags.
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

        // Prepare the label for a missing tag, if the current value is not found in the tag list.
        string currentString = property.stringValue;
        string currentText = string.IsNullOrEmpty(currentString) ? "Tag Missing" : $"Tag Missing ({currentString})";
        string missingTagText = System.Array.Exists(tags, tag => tag == currentString) ? "" : currentText;

        // Build the tag list to display in the dropdown.
        List<string> tagList = new() { missingTagText };
        tagList.AddRange(tags);

        // Find the index of the current tag in the list.
        int currentIndex = tagList.IndexOf(currentString);
        if (currentIndex == -1) currentIndex = 0; // If not found, default to the "Tag Missing" entry.

        // Label with tooltip above the popup.
        EditorGUI.LabelField(position, new GUIContent("", label.tooltip));

        // Display the dropdown in the Inspector.
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, tagList.ToArray());

        // If the selected tag is valid (not "Tag Missing"), update the property value.
        if (newIndex != 0) property.stringValue = tagList[newIndex];

        // Show a warning if the selected tag is "Tag Missing".
        if (newIndex == 0)
        {
            // Define the rectangle for the HelpBox below the dropdown field.
            Rect helpBoxRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight * 2);
            EditorGUI.HelpBox(helpBoxRect, "String value does not match any tag!", MessageType.Warning);
        }
    }

    /// <summary>
    /// Calculates the required height for the property including the warning box if needed.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Check if the current tag exists. If not, add height for the warning HelpBox.
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
        if (tags != null && !System.Array.Exists(tags, tag => tag == property.stringValue))
        {
            return EditorGUIUtility.singleLineHeight * 3; // Adds extra space for the warning.
        }

        return EditorGUIUtility.singleLineHeight;
    }
}

#endregion

#endif