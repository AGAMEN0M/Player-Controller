/*
 * ---------------------------------------------------------------------------
 * Description: Extension methods for MonoBehaviour to simplify reflection-based property access.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Reflection;
using UnityEngine;

/// <summary>
/// Extension methods for MonoBehaviour to simplify reflection-based property access.
/// </summary>
public static class MonoBehaviourExtensions
{
    /// <summary>
    /// Attempts to retrieve a public property value by name using reflection.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="mono">The MonoBehaviour to inspect.</param>
    /// <param name="propName">Name of the property.</param>
    /// <param name="value">Output value if property exists and is of type T.</param>
    /// <returns>True if the property was found and value retrieved; otherwise, false.</returns>
    public static bool TryGetProperty<T>(this MonoBehaviour mono, string propName, out T value)
    {
        var prop = mono.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(T))
        {
            value = (T)prop.GetValue(mono);
            return true;
        }

        value = default;
        return false;
    }
}