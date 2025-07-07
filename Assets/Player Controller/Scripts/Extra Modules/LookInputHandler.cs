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
using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// MonoBehaviour responsible for handling look input for the camera.
/// It listens to a configured input action and updates a direction vector,
/// typically used to rotate the camera based on player input.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Look Input Handler")]
public class LookInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField, ValidateReference] private InputActionAsset inputActions; // Reference to the InputActionAsset containing all input actions.
    [SerializeField] private string lookActionPath = "Player/Look"; // Path to the input action responsible for camera look.

    /// <summary>
    /// Current directional input value for look control (e.g., mouse delta or right stick).
    /// Updated while the input is held and reset when released.
    /// </summary>
    [Header("Runtime Look Input")]
    [ReadOnlyInInspector] public Vector2 lookDirection;

    private OnInputSystemEventConfig<Vector2> lookInputEvent; // Event configuration for binding and unbinding look input events.
    private bool isPlayable = true; // Indicates whether input processing is currently enabled.

    private void Awake()
    {
        if (!inputActions) Debug.LogWarning("InputActions not assigned in LookInputHandler.", this);
    }

    /// <summary>
    /// Initializes the look input action and sets up handlers for hold and release events.
    /// Input is processed only if 'isPlayable' is true.
    /// </summary>
    private void Start()
    {
        lookInputEvent = OnInputSystemEvent<Vector2>.WithAction(inputActions, lookActionPath, () => isPlayable)
            .OnHold(value =>
            {
                lookDirection = value; // Update look direction when input is held.
            })
            .OnReleased(() =>
            {
                lookDirection = Vector2.zero; // Reset look direction when input is released.
            });
    }

    /// <summary>
    /// Unbinds all input events to prevent memory leaks or unintended behavior.
    /// </summary>
    private void OnDestroy() => lookInputEvent?.UnbindAll();

    /// <summary>
    /// Enables or disables look input processing at runtime.
    /// </summary>
    /// <param name="enabled">If true, input will be processed; otherwise, it will be ignored.</param>
    public void TogglePlayable(bool enabled)
    {
        isPlayable = enabled;
        if (!isPlayable)
        {
            lookDirection = Vector2.zero;
        }
    }
}