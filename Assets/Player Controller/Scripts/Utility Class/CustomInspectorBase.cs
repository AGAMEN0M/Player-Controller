/*
 * ---------------------------------------------------------------------------
 * Description: Base utility for creating structured and safe custom inspectors.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using System;

namespace PlayerController.Editor
{
    /// <summary>
    /// Base class for custom inspectors with helper methods to safely draw properties,
    /// support multi-object editing, and create reusable UI patterns.
    /// </summary>
    public abstract class CustomInspectorBase<T> : UnityEditor.Editor where T : MonoBehaviour
    {
        #region === State Management ===

        /// <summary>
        /// Cached foldout states to minimize EditorPrefs access and improve performance.
        /// </summary>
        private static readonly Dictionary<string, bool> foldoutStates = new();

        /// <summary>
        /// Strongly-typed reference to the inspected target.
        /// </summary>
        protected T script;

        #endregion

        #region === Lifecycle ===

        /// <summary>
        /// Initializes cached references when the inspector becomes active.
        /// </summary>
        protected virtual void OnEnable()
        {
            script = (T)target;
        }

        /// <summary>
        /// Main inspector rendering loop.
        /// Handles serialization lifecycle and delegates layout drawing.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Sync serialized data before drawing.
            serializedObject.Update();

            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            EditorGUILayout.Space(10f);

            // Let derived classes define the inspector layout.
            DrawInspector();

            // Apply any property modifications.
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region === Inspector API ===

        /// <summary>
        /// Override this method to define the inspector UI layout.
        /// </summary>
        protected abstract void DrawInspector();

        #endregion

        #region === Property Drawing ===

        /// <summary>
        /// Draws a SerializedProperty by name, preserving all Unity behaviors and attributes.
        /// </summary>
        /// <param name="propertyName">Field name inside the serialized object.</param>
        protected void GUIProperty(string propertyName)
        {
            // Attempt to find the serialized property.
            var prop = serializedObject.FindProperty(propertyName);

            // If not found, show an error to help debugging.
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"Property '{propertyName}' not found.", MessageType.Error);
                return;
            }

            // Draw property with children support.
            EditorGUILayout.PropertyField(prop, true);
        }

        /// <summary>
        /// Draws a SerializedProperty using a lambda expression for safer refactoring.
        /// </summary>
        /// <param name="expr">Expression pointing to the target field.</param>
        protected void GUIProperty(Expression<Func<T, object>> expr)
        {
            string name = GetPropertyName(expr);
            GUIProperty(name);
        }

        /// <summary>
        /// Extracts the property name from a lambda expression.
        /// </summary>
        /// <param name="expr">Expression to parse.</param>
        /// <returns>Resolved property name.</returns>
        private string GetPropertyName(Expression<Func<T, object>> expr)
        {
            // Direct member access (e.g., x => x.field).
            if (expr.Body is MemberExpression member)
                return member.Member.Name;

            // Handles boxing conversions (e.g., value types).
            if (expr.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                return unaryMember.Member.Name;

            // Invalid expression format.
            throw new Exception("Invalid expression.");
        }

        #endregion

        #region === Groups ===

        /// <summary>
        /// Draws a foldable group with persistent state stored in EditorPrefs.
        /// </summary>
        /// <param name="groupName">Displayed header label.</param>
        /// <param name="drawContent">Callback responsible for rendering group content.</param>
        protected void DrawGroup(GUIContent groupName, Action drawContent)
        {
            // Retrieve stored foldout state.
            bool isOpen = GetGroupState(groupName.text);

            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Reserve space for the header.
            Rect headerRect = GUILayoutUtility.GetRect(0f, lineHeight + 2f, GUILayout.ExpandWidth(true));

            // Force full inspector width (fixes horizontal misalignment).
            headerRect.x = 0f;
            headerRect.width = EditorGUIUtility.currentViewWidth;

            // Draw top border line.
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 1f), new Color(0f, 0f, 0f, 0.3f));

            // Draw subtle background.
            EditorGUI.DrawRect(headerRect, new Color(0f, 0f, 0f, 0.1f));

            // Create inner rect (padding only here, not on headerRect itself).
            Rect contentRect = new(headerRect.x + 14f, headerRect.y, headerRect.width - 14f, headerRect.height);

            // Clone and customize foldout style.
            GUIStyle foldoutStyle = new(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            // Build label with optional tooltip.
            GUIContent label = new($" {groupName.text}", groupName.tooltip);

            // Draw foldout and capture new state.
            bool newState = EditorGUI.Foldout(contentRect, isOpen, label, true, foldoutStyle);

            // Save state if it changed.
            if (newState != isOpen)
            {
                SaveGroupState(groupName.text, newState);
            }

            // Draw contents if expanded.
            if (newState)
            {
                EditorGUI.indentLevel++;
                drawContent?.Invoke();
                EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }
        }

        #endregion

        #region === Utilities ===

        /// <summary>
        /// Gets the foldout state from cache or EditorPrefs.
        /// </summary>
        /// <param name="groupName">Group identifier.</param>
        /// <returns>True if expanded.</returns>
        private bool GetGroupState(string groupName)
        {
            // Try to retrieve from in-memory cache first.
            if (!foldoutStates.TryGetValue(groupName, out bool state))
            {
                // Fallback to EditorPrefs if not cached.
                state = EditorPrefs.GetBool(GetPrefsKey(groupName), true);

                // Store in cache for future use.
                foldoutStates[groupName] = state;
            }

            return state;
        }

        /// <summary>
        /// Stores foldout state in memory and EditorPrefs.
        /// </summary>
        /// <param name="groupName">Group identifier.</param>
        /// <param name="state">New state.</param>
        private void SaveGroupState(string groupName, bool state)
        {
            // Update in-memory cache.
            foldoutStates[groupName] = state;

            // Persist state across editor sessions.
            EditorPrefs.SetBool(GetPrefsKey(groupName), state);
        }

        /// <summary>
        /// Generates a unique key for storing foldout state.
        /// </summary>
        /// <param name="groupName">Group identifier.</param>
        /// <returns>EditorPrefs key.</returns>
        private string GetPrefsKey(string groupName) => $"HeaderGroupGUI_Foldout_{groupName}";

        #endregion
    }
}
#endif