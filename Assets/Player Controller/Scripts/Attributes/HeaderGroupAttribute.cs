/*
 * ---------------------------------------------------------------------------
 * Description: Provides a collapsible header grouping system for Unity inspectors.
 *              Can be used automatically through [UseHeaderGroupInspector]
 *              or manually in custom inspectors via HeaderGroupGUI API.
 * 
 * Usage (automatic):
 *   [UseHeaderGroupInspector]
 *   public class ExampleComponent : MonoBehaviour
 *   {
 *       [Header("Player Settings"), HeaderGroup]
 *       public int health;
 *   }
 * 
 * Usage (manual in CustomEditor):
 *   HeaderGroupGUI.DrawGroup("Stats", () =>
 *   {
 *       EditorGUILayout.PropertyField(serializedObject.FindProperty("health"));
 *       EditorGUILayout.PropertyField(serializedObject.FindProperty("stamina"));
 *   });
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;
using System;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#endif

#region === Attribute Definition ===

/// <summary>
/// Marks a header as the start of a grouped section in the Unity Inspector.
/// Used together with [Header("...")] to visually group related serialized fields.
/// </summary>
public class HeaderGroupAttribute : PropertyAttribute { }

/// <summary>
/// Marks a MonoBehaviour to use the HeaderGroup custom inspector automatically.
/// </summary>
public class UseHeaderGroupInspectorAttribute : Attribute { }

#endregion

#if UNITY_EDITOR

#region === Public HeaderGroup GUI API ===

/// <summary>
/// Public static helper for drawing collapsible header groups manually
/// in custom inspectors or editor windows.
/// </summary>
public static class HeaderGroupGUI
{
    // Stores foldout states per group name.
    private static readonly Dictionary<string, bool> foldoutStates = new();

    /// <summary>
    /// Draws a collapsible group block with automatic state saving.
    /// </summary>
    public static void DrawGroup(string groupName, Action drawContent)
    {
        bool isOpen = GetGroupState(groupName);

        GUILayout.BeginVertical();
        DrawGroupHeader(groupName, ref isOpen);

        if (isOpen)
        {
            EditorGUI.indentLevel++;
            drawContent?.Invoke();
            EditorGUI.indentLevel--;
        }

        GUILayout.EndVertical();
    }

    /// <summary>
    /// Begins a header group section (manual version).
    /// You must call EndGroup() after this.
    /// </summary>
    public static bool BeginGroup(string groupName)
    {
        bool isOpen = GetGroupState(groupName);
        GUILayout.BeginVertical();
        DrawGroupHeader(groupName, ref isOpen);
        if (isOpen) EditorGUI.indentLevel++;
        return isOpen;
    }

    /// <summary>
    /// Ends a header group section started with BeginGroup().
    /// </summary>
    public static void EndGroup()
    {
        EditorGUI.indentLevel = Mathf.Max(0, EditorGUI.indentLevel - 1);
        GUILayout.EndVertical();
    }

    // === Internal utilities ===

    private static void DrawGroupHeader(string groupName, ref bool isOpen)
    {
        Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
        Rect backgroundRect = new(foldoutRect.x - 20, foldoutRect.y - 2, foldoutRect.width + 30, foldoutRect.height + 4);

        // Semi-transparent black background.
        EditorGUI.DrawRect(backgroundRect, new(0f, 0f, 0f, 0.35f));

        // Custom foldout style.
        GUIStyle foldoutStyle = new(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
        };

        bool newState = EditorGUI.Foldout(foldoutRect, isOpen, $" {groupName}", true, foldoutStyle);

        if (newState != isOpen)
        {
            isOpen = newState;
            SaveGroupState(groupName, newState);
        }
    }

    private static bool GetGroupState(string groupName)
    {
        if (!foldoutStates.ContainsKey(groupName))
        {
            foldoutStates[groupName] = EditorPrefs.GetBool($"HeaderGroupGUI_{groupName}", true);
        }
        return foldoutStates[groupName];
    }

    private static void SaveGroupState(string groupName, bool state)
    {
        foldoutStates[groupName] = state;
        EditorPrefs.SetBool($"HeaderGroupGUI_{groupName}", state);
    }
}

#endregion

#region === Custom Inspector ===

/// <summary>
/// Custom inspector drawer that visually groups serialized fields in a
/// collapsible section using [Header("..."), HeaderGroup].
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class HeaderGroupDrawer : Editor
{
    private readonly Dictionary<string, bool> foldoutStates = new();

    public override void OnInspectorGUI()
    {
        // Skip if not marked with [UseHeaderGroupInspector].
        if (target.GetType().GetCustomAttribute<UseHeaderGroupInspectorAttribute>() == null)
        {
            DrawDefaultInspector();
            return;
        }

        // Draw the script field at the top (read-only).
        using (new EditorGUI.DisabledScope(true))
        {
            GUILayout.Space(5);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            GUILayout.Space(5);
        }

        serializedObject.Update();

        var property = serializedObject.GetIterator();
        property.NextVisible(true); // Skip "Script" field.

        string currentGroup = null;
        bool inGroup = false;

        while (property.NextVisible(false))
        {
            var field = target.GetType().GetField(property.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                EditorGUILayout.PropertyField(property, true);
                continue;
            }

            var header = field.GetCustomAttribute<HeaderAttribute>();
            var headerGroup = field.GetCustomAttribute<HeaderGroupAttribute>();

            if (header != null && headerGroup != null)
            {
                if (inGroup)
                {
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }

                currentGroup = header.header;

                if (!foldoutStates.ContainsKey(currentGroup))
                {
                    foldoutStates[currentGroup] = LoadFoldoutState(currentGroup);
                }

                DrawGroupBackground(foldoutStates[currentGroup], currentGroup);

                inGroup = true;

                if (foldoutStates[currentGroup])
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                continue;
            }

            if (header != null && headerGroup == null && inGroup)
            {
                GUILayout.EndVertical();
                GUILayout.Space(5);
                inGroup = false;
                currentGroup = null;
            }

            if (!inGroup || (foldoutStates.TryGetValue(currentGroup, out bool open) && open))
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        if (inGroup) GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGroupBackground(bool isOpen, string groupName)
    {
        GUILayout.BeginVertical();

        Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
        Rect backgroundRect = new(foldoutRect.x - 20, foldoutRect.y - 2, foldoutRect.width + 30, foldoutRect.height + 4);

        EditorGUI.DrawRect(backgroundRect, new(0f, 0f, 0f, 0.35f));

        GUIStyle foldoutStyle = new(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
        };

        bool newState = EditorGUI.Foldout(foldoutRect, foldoutStates[groupName], $" {groupName}", true, foldoutStyle);

        if (newState != foldoutStates[groupName])
        {
            foldoutStates[groupName] = newState;
            SaveFoldoutState(groupName, newState);
        }

        if (!isOpen) GUILayout.Space(2);
    }

    private string GetPrefsKey(string groupName)
    {
        return $"{target.GetType().FullName}_Foldout_{groupName}";
    }

    private bool LoadFoldoutState(string groupName)
    {
        string key = GetPrefsKey(groupName);
        return EditorPrefs.GetBool(key, true);
    }

    private void SaveFoldoutState(string groupName, bool isOpen)
    {
        string key = GetPrefsKey(groupName);
        EditorPrefs.SetBool(key, isOpen);
    }
}

#endregion

#endif