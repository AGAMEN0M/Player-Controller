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

namespace PlayerController.Attributes
{
    #region === Attribute Definition ===

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

    #endregion

#if UNITY_EDITOR

    #region === Property Drawer ===

    /// <summary>
    /// Custom property drawer for ValidateReferenceAttribute.
    /// Highlights empty object reference fields and shows a help message.
    /// </summary>
    [CustomPropertyDrawer(typeof(ValidateReferenceAttribute))]
    public class ValidateReferenceDrawer : PropertyDrawer
    {
        #region === OnGUI ===

        /// <summary>
        /// Draws the field in the Inspector and displays a message if the reference is missing.
        /// </summary>
        /// <param name="position">The rect for the property field.</param>
        /// <param name="property">The property being drawn.</param>
        /// <param name="label">The GUI label of the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Early exit if the property is not an object reference.
            // This prevents misuse of the attribute on unsupported field types.
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Store the original background color to restore it later.
            Color previousColor = GUI.backgroundColor;

            // Check if the reference is null.
            bool isEmpty = property.objectReferenceValue == null;

            // Get the attribute instance to determine behavior (error or warning).
            var attr = (ValidateReferenceAttribute)attribute;

            // Apply softer highlight colors for better visual comfort.
            if (isEmpty)
            {
                GUI.backgroundColor = attr.UseError
                    ? new Color(1f, 0.5f, 0.5f) // Soft red.
                    : new Color(1f, 0.85f, 0.4f); // Soft yellow/orange.
            }

            // Define the rect for the property field.
            Rect propertyRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Draw the property field.
            EditorGUI.PropertyField(propertyRect, property, label);

            // Restore the original background color.
            GUI.backgroundColor = previousColor;

            // If the reference is missing, draw a help box below.
            if (isEmpty)
            {
                string typeName = "Unknown";

                // Use built-in fieldInfo instead of fragile reflection.
                if (fieldInfo != null)
                {
                    typeName = fieldInfo.FieldType.Name;
                }

                // Prepare help box content.
                GUIContent helpContent = new($"Put an item of type '{typeName}' here!");

                // Calculate dynamic height for the help box.
                float helpHeight = EditorStyles.helpBox.CalcHeight(helpContent, position.width);

                // Define help box rect.
                Rect helpBoxRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, helpHeight);

                // Determine message type.
                MessageType messageType = attr.UseError ? MessageType.Error : MessageType.Warning;

                // Draw the help box.
                EditorGUI.HelpBox(helpBoxRect, helpContent.text, messageType);
            }
        }

        #endregion

        #region === GetPropertyHeight ===

        /// <summary>
        /// Returns the required height for the property and message box.
        /// </summary>
        /// <param name="property">The property being drawn.</param>
        /// <param name="label">The GUI label of the property.</param>
        /// <returns>Total height required to draw the property.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // If not an object reference, fallback to default height.
            if (property.propertyType != SerializedPropertyType.ObjectReference) return EditorGUIUtility.singleLineHeight;

            // Check if the reference is null.
            bool isEmpty = property.objectReferenceValue == null;

            // If not empty, only one line is needed.
            if (!isEmpty) return EditorGUIUtility.singleLineHeight;

            // Determine the expected type name.
            string typeName = fieldInfo != null ? fieldInfo.FieldType.Name : "Unknown";

            // Prepare help box content.
            GUIContent helpContent = new($"Put an item of type '{typeName}' here!");

            // Calculate dynamic help box height.
            float helpHeight = EditorStyles.helpBox.CalcHeight(helpContent, EditorGUIUtility.currentViewWidth);

            // Return total height: field + spacing + help box.
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + helpHeight;
        }

        #endregion

    }

    #endregion

#endif
}