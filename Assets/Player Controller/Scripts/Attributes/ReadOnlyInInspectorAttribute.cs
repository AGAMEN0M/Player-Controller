/*
 * ---------------------------------------------------------------------------
 * Description: Custom attribute and property drawer that renders fields 
 *              as read-only (disabled) in the Unity Inspector. Useful for displaying 
 *              runtime values or debug info without allowing manual changes.
 * 
 * Using: [SerializeField, ReadOnlyInInspector]
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attribute to display a field as read-only in the Unity Inspector.
/// This does not prevent modification via code; it only disables editing in the Inspector.
/// </summary>
public class ReadOnlyInInspectorAttribute : PropertyAttribute
{
    // This is a marker attribute with no additional logic.
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for the ReadOnlyInInspectorAttribute.
/// Renders the decorated field as disabled (read-only) in the Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
public class ReadOnlyInInspectorDrawer : PropertyDrawer
{
    /// <summary>
    /// Draws the property field in a disabled state, making it read-only in the Inspector.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // Disable GUI to prevent editing.
        EditorGUI.PropertyField(position, property, label); // Draw the property field.
        GUI.enabled = true; // Re-enable GUI for other fields.
    }
}
#endif