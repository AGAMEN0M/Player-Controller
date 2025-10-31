/*
 * ---------------------------------------------------------------------------
 * Description: Controls the camera's active state based on input actions or 
 *              the activity of specified GameObjects. Supports manual toggle, 
 *              input-based toggle, or automatic updates using Update or FixedUpdate 
 *              modes. Useful for enabling/disabling the camera when menus or 
 *              UI elements are shown.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using PlayerController.InputEvents;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// Manages the camera state based on input and/or activation of specific GameObjects.
/// It supports manual, input-driven, or automatic modes to enable or disable the camera dynamically.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Camera State Manager")]
public class CameraStateManager : MonoBehaviour
{
    #region === Enumerations ===

    /// <summary>
    /// Defines how and when the camera state is updated.
    /// </summary>
    public enum UpdateType
    {
        /// <summary>Camera state is controlled manually through external method calls.</summary>
        ManualToggle,

        /// <summary>Camera state is toggled via input events (e.g., button press).</summary>
        InputEvent,

        /// <summary>Camera state is evaluated every frame in Update().</summary>
        Update,

        /// <summary>Camera state is evaluated at fixed intervals in FixedUpdate().</summary>
        FixedUpdate
    }

    #endregion

    #region === Serialized Fields ===

    [Header("Input Settings")]
    [SerializeField, ValidateReference, Tooltip("Input action reference used to manually toggle the camera on or off.")]
    private InputActionReference toggleAction; // Input action used to toggle the camera state.

    [Header("Enable and Disable Camera")]
    [SerializeField, Tooltip("Determines when and how the camera state should be evaluated (manual, input event, Update, or FixedUpdate).")]
    private UpdateType updateType = UpdateType.InputEvent; // Defines how camera state is updated.

    [SerializeField, ValidateReference, Tooltip("List of GameObjects that control camera activation. If any are active, the camera is disabled.")]
    private List<GameObject> activationObjects = new(); // Objects used to control camera activation.

    [Header("Events and Debug")]
    [SerializeField, Tooltip("Enables debug logging for camera state changes and transitions.")]
    private bool debug = false; // Enables debug mode for detailed logs.

    [Tooltip("Event invoked when the camera is enabled.")]
    public UnityEvent OnEnable; // Event triggered when camera is activated.

    [Tooltip("Event invoked when the camera is disabled.")]
    public UnityEvent OnDisable; // Event triggered when camera is deactivated.

    #endregion

    #region === Private Fields ===

    private bool isCameraActive; // Indicates whether the camera is currently active.
    private bool lastActivationState; // Stores the previous activation state of monitored GameObjects.
    private float lastUpdateTime; // Last timestamp of state evaluation.
    private float activeDuration; // Duration that activation objects have been continuously active.
    private float inactiveDuration; // Duration that activation objects have been continuously inactive.
    private OnInputSystemEventConfig<float> toggleEvent; // Cached event listener for the input toggle action.

    #endregion

    #region === Properties ===

    /// <summary>
    /// Gets or sets the InputAction used to toggle the camera state.
    /// </summary>
    public InputActionReference ToggleAction
    {
        get => toggleAction;
        set => toggleAction = value;
    }

    /// <summary>
    /// Gets or sets the update mode used for evaluating the camera state.
    /// </summary>
    public UpdateType ModeType
    {
        get => updateType;
        set => updateType = value;
    }

    /// <summary>
    /// Gets or sets the list of GameObjects that determine whether the camera should be enabled or disabled.
    /// </summary>
    public List<GameObject> ActivationObjects
    {
        get => activationObjects;
        set => activationObjects = value;
    }

    /// <summary>
    /// Gets or sets whether debug messages are printed to the console.
    /// </summary>
    public bool DebugLog
    {
        get => debug;
        set => debug = value;
    }

    #endregion

    #region === Unity Lifecycle ===

    private void Awake()
    {
        // Disable the component if no input asset is assigned.
        if (toggleAction == null)
        {
            Debug.LogWarning($"[{nameof(CameraStateManager)}] Input Asset not assigned.", this);
            enabled = false;
            return;
        }

        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Initialized with updateType: {updateType}.", this);
    }

    private void Start()
    {
        SetCameraState(true); // Initially set the camera as active.

        // Register the toggle input event if in input-based mode.
        toggleEvent = OnInputSystemEvent<float>.WithAction(toggleAction, this).OnPressed(_ =>
        {
            if (updateType == UpdateType.InputEvent) SetCameraState(!isCameraActive);
        });
    }

    /// <summary>
    /// Cleans up input event bindings when the component is destroyed.
    /// </summary>
    private void OnDestroy() => toggleEvent?.Dispose();

    private void Update()
    {
        // Evaluate camera state each frame if using Update mode.
        if (updateType == UpdateType.Update) UpdateCameraStateFromObjects();
    }

    private void FixedUpdate()
    {
        // Evaluate camera state at fixed intervals if using FixedUpdate mode.
        if (updateType == UpdateType.FixedUpdate) UpdateCameraStateFromObjects();
    }

    #endregion

    #region === Camera Control ===

    /// <summary>
    /// Sets the camera's active state, updates cursor lock and visibility,
    /// and triggers corresponding events and debug logs.
    /// </summary>
    /// <param name="active">True to activate the camera; false to deactivate it.</param>
    public void SetCameraState(bool active)
    {
        // Avoid redundant state changes.
        if (isCameraActive == active) return;

        isCameraActive = active;

        // Update cursor lock state and visibility.
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;

        // Invoke the corresponding event.
        if (active) OnEnable?.Invoke(); else OnDisable?.Invoke();

        // Log debug information if enabled.
        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Camera state set to: {active}.", this);
    }

    /// <summary>
    /// Evaluates the active state of monitored GameObjects and updates the camera state accordingly.
    /// A 0.1-second threshold is used to prevent flickering from rapid activation changes.
    /// </summary>
    private void UpdateCameraStateFromObjects()
    {
        if (activationObjects.Count == 0) return;

        bool anyActive = IsAnyActivationObjectActive();

        float now = Time.realtimeSinceStartup;
        float deltaTime = now - lastUpdateTime;
        lastUpdateTime = now;

        // Reset timers when activation state changes.
        if (anyActive != lastActivationState)
        {
            lastActivationState = anyActive;
            activeDuration = 0f;
            inactiveDuration = 0f;
            return;
        }

        // If any object remains active, disable the camera after 0.1 seconds.
        if (anyActive)
        {
            activeDuration += deltaTime;
            if (activeDuration >= 0.1f) SetCameraState(false);
        }
        else // If all objects are inactive, enable the camera after 0.1 seconds.
        {
            inactiveDuration += deltaTime;
            if (inactiveDuration >= 0.1f) SetCameraState(true);
        }
    }

    /// <summary>
    /// Checks whether any monitored GameObject is currently active in the scene.
    /// </summary>
    /// <returns>True if at least one object is active; otherwise, false.</returns>
    private bool IsAnyActivationObjectActive() => activationObjects.Exists(obj => obj != null && obj.activeSelf);

    #endregion
}