/*
 * ---------------------------------------------------------------------------
 * Description: Provides reflection-based extension methods for MonoBehaviour 
 *              to access property values dynamically at runtime. Includes adapter 
 *              and interface to extract movement-related states from any controller 
 *              exposing expected public properties.
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

#region Helpers (Interfaces & Reflection)

/// <summary>
/// Interface defining properties required to retrieve the player's movement state.
/// </summary>
public interface IPlayerMovementState
{
    bool IsGrounded { get; }  // Is the player currently on the ground?
    bool IsMoving { get; }    // Is the player currently moving?
    bool IsCrouching { get; } // Is the player crouching?
    bool IsRunning { get; }   // Is the player running?
}

/// <summary>
/// Adapter using reflection to access the required properties from any player controller.
/// </summary>
public class PlayerMovementStateAdapter : IPlayerMovementState
{
    private readonly MonoBehaviour target;

    public PlayerMovementStateAdapter(MonoBehaviour target)
    {
        this.target = target;
    }

    // Uses extension method to safely get property values from the target MonoBehaviour.
    public bool IsGrounded => target.TryGetProperty(nameof(IsGrounded), out bool value) && value;
    public bool IsMoving => target.TryGetProperty(nameof(IsMoving), out bool value) && value;
    public bool IsCrouching => target.TryGetProperty(nameof(IsCrouching), out bool value) && value;
    public bool IsRunning => target.TryGetProperty(nameof(IsRunning), out bool value) && value;
}

#endregion