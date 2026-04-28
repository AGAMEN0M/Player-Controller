/*
 * ---------------------------------------------------------------------------
 * Description: Provides reflection-based extension methods for MonoBehaviour 
 *              to access property values dynamically at runtime. Includes an adapter 
 *              and interface to extract movement-related states from any controller 
 *              exposing the expected public properties.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Reflection;
using UnityEngine;

namespace PlayerController
{
    #region === MonoBehaviour Extensions ===

    /// <summary>
    /// Provides reflection-based extension methods for <see cref="MonoBehaviour"/> 
    /// to dynamically retrieve public property values at runtime.
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Attempts to retrieve the value of a public property by name using reflection.
        /// </summary>
        /// <typeparam name="T">Expected property type.</typeparam>
        /// <param name="mono">The <see cref="MonoBehaviour"/> to inspect.</param>
        /// <param name="propName">The name of the property to access.</param>
        /// <param name="value">The retrieved value, if successful.</param>
        /// <returns>True if the property exists and is of type <typeparamref name="T"/>; otherwise, false.</returns>
        public static bool TryGetProperty<T>(this MonoBehaviour mono, string propName, out T value)
        {
            // Retrieve public instance property info.
            var prop = mono.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);

            // Validate property and type before attempting to read.
            if (prop != null && prop.PropertyType == typeof(T))
            {
                value = (T)prop.GetValue(mono);
                return true;
            }

            value = default;
            return false;
        }
    }

    #endregion

    #region === Movement State Reflection Helpers ===

    /// <summary>
    /// Defines the required properties for any player movement state representation.
    /// </summary>
    public interface IPlayerMovementState
    {
        bool IsGrounded { get; }  // Indicates if the player is currently grounded.
        bool IsMoving { get; }    // Indicates if the player is currently moving.
        bool IsCrouching { get; } // Indicates if the player is currently crouching.
        bool IsRunning { get; }   // Indicates if the player is currently running.
    }

    /// <summary>
    /// Provides a reflection-based adapter to read movement-related properties
    /// from any <see cref="MonoBehaviour"/> implementing the expected public members.
    /// </summary>
    public class PlayerMovementStateAdapter : IPlayerMovementState
    {
        private readonly MonoBehaviour target; // Reference to the inspected MonoBehaviour.

        /// <summary>
        /// Creates a new adapter for the specified target component.
        /// </summary>
        /// <param name="target">The target <see cref="MonoBehaviour"/> to read movement properties from.</param>
        public PlayerMovementStateAdapter(MonoBehaviour target)
        {
            this.target = target;
        }

        /// <summary>
        /// Indicates whether the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => target.TryGetProperty(nameof(IsGrounded), out bool value) && value;

        /// <summary>
        /// Indicates whether the player is currently moving.
        /// </summary>
        public bool IsMoving => target.TryGetProperty(nameof(IsMoving), out bool value) && value;

        /// <summary>
        /// Indicates whether the player is currently crouching.
        /// </summary>
        public bool IsCrouching => target.TryGetProperty(nameof(IsCrouching), out bool value) && value;

        /// <summary>
        /// Indicates whether the player is currently running.
        /// </summary>
        public bool IsRunning => target.TryGetProperty(nameof(IsRunning), out bool value) && value;
    }

    #endregion
}