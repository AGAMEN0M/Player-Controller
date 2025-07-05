/*
 * ---------------------------------------------------------------------------
 * Description: Custom attribute and property drawer for validating required 
 *              object references in the Unity Inspector. Highlights missing references with 
 *              colored fields and help messages (error or warning).
 * 
 * Using: [ValidateReference], [ValidateReference(true)], [ValidateReference(false)]
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
/// Attribute to highlight object reference fields when they are empty.
/// Displays a message in the Inspector if the reference is missing.
/// </summary>
public class ValidateReferenceAttribute : PropertyAttribute
{
    /// <summary>
    /// If true, the message will be shown as an error. If false, as a warning.
    /// </summary>
    public readonly bool UseError;

    /// <summary>
    /// Constructor for ValidateReferenceAttribute.
    /// </summary>
    /// <param name="useError">If true, the message appears as an error; otherwise, as a warning.</param>
    public ValidateReferenceAttribute(bool useError = true)
    {
        UseError = useError;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for ValidateReferenceAttribute.
/// Highlights empty object reference fields and shows a help message.
/// </summary>
[CustomPropertyDrawer(typeof(ValidateReferenceAttribute))]
public class ValidateReferenceDrawer : PropertyDrawer
{
    /// <summary>
    /// Draws the field in the Inspector and displays a message if the reference is missing.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Color previousColor = GUI.backgroundColor;

        // Check if property is a null object reference.
        bool isEmpty = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;

        // Get attribute instance to check the error mode.
        var attr = (ValidateReferenceAttribute)attribute;

        // Highlight field: red for error, yellow for warning.
        if (isEmpty)
        {
            GUI.backgroundColor = attr.UseError ? Color.red : Color.yellow;
        }

        // Draw the property field.
        Rect propertyRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propertyRect, property, label);

        GUI.backgroundColor = previousColor;

        // Show a help box if the reference is missing.
        if (isEmpty)
        {
            string typeName = "Unknown";

            // Attempt to get the expected type name.
            if (property.serializedObject.targetObject != null)
            {
                var targetObject = property.serializedObject.targetObject;
                var fieldInfo = targetObject.GetType().GetField(property.name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (fieldInfo != null) typeName = fieldInfo.FieldType.Name;
            }

            // Help box below the property.
            Rect helpBoxRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight * 2);

            // Show error or warning.
            var messageType = attr.UseError ? MessageType.Error : MessageType.Warning;
            EditorGUI.HelpBox(helpBoxRect, $"Put an item of type '{typeName}' here!", messageType);
        }
    }

    /// <summary>
    /// Returns the required height for the property and message box.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        bool isEmpty = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
        return isEmpty ? EditorGUIUtility.singleLineHeight * 3 + 4 : EditorGUIUtility.singleLineHeight;
    }
}
#endif