/*
 * ---------------------------------------------------------------------------
 * Description: Handles camera look input using Unity's Input System.
 *              Captures and stores the player's look direction from input,
 *              and allows enabling or disabling input processing dynamically.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using PlayerController.InputEvents;
using PlayerController.Attributes;
using UnityEngine.InputSystem;
using UnityEngine;

namespace PlayerController
{
    /// <summary>
    /// Handles camera look input through Unity's Input System.
    /// Captures and stores the current look direction for use in camera rotation.
    /// </summary>
    [AddComponentMenu("Tools/Player Controller/Extra Modules/Look Input Handler")]
    public class LookInputHandler : MonoBehaviour
    {
        #region === Serialized Fields ===

        [Header("Input Settings")]
        [SerializeField, ValidateReference, Tooltip("Input action that captures look input.")]
        private InputActionReference lookAction;

        [Header("Runtime Look Input")]
        [ReadOnly, Tooltip("Current look direction from input.")]
        public Vector2 LookDirection;

        #endregion

        #region === Private Fields ===

        private OnInputSystemEventConfig<Vector2> lookInputEvent; // Stores the configured look input event.
        private bool isPlayable = true; // Determines if input processing is currently enabled.

        #endregion

        #region === Properties ===

        /// <summary>
        /// Gets or sets the input action used to capture look input.
        /// </summary>
        public InputActionReference LookAction
        {
            get => lookAction;
            set => lookAction = value;
        }

        #endregion

        #region === Unity Lifecycle Methods ===

        /// <summary>
        /// Checks if the input action is assigned and warns if not.
        /// </summary>
        private void Awake()
        {
            if (lookAction == null) Debug.LogWarning("LookInputHandler: InputAction is not assigned.", this);
        }

        /// <summary>
        /// Configures and binds the look input event.
        /// Updates LookDirection when the player moves the camera input.
        /// </summary>
        private void Start()
        {
            // Bind hold and release events for look input.
            lookInputEvent = OnInputSystemEvent<Vector2>.WithAction(lookAction, this, () => isPlayable)
                .OnHold(value =>
                {
                    LookDirection = value; // Update direction while input is held.
                })
                .OnReleased(() =>
                {
                    LookDirection = Vector2.zero; // Reset direction when input stops.
                });
        }

        /// <summary>
        /// Unbinds input events when this object is destroyed.
        /// </summary>
        private void OnDestroy() => lookInputEvent?.Dispose();

        #endregion

        #region === Public Methods ===

        /// <summary>
        /// Enables or disables look input at runtime.
        /// </summary>
        /// <param name="enabled">If true, input will be processed; otherwise ignored.</param>
        public void TogglePlayable(bool enabled)
        {
            isPlayable = enabled;

            // Clear direction when input is disabled.
            if (!isPlayable) LookDirection = Vector2.zero;
        }

        #endregion
    }
}