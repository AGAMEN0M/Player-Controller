/*
 * ---------------------------------------------------------------------------
 * Description: Controls the camera's active state based on input actions or 
 *              the activity of specified GameObjects. Supports manual toggle, input-based 
 *              toggle, or automatic updates using Update or FixedUpdate modes. Useful for 
 *              enabling/disabling the camera when menus or UI elements are shown.
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
/// It can toggle the camera manually or automatically based on the selected update type.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Camera State Manager")]
public class CameraStateManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField, ValidateReference] private InputActionAsset inputAsset; // Input Action Asset containing defined actions.
    [SerializeField] private string toggleActionPath = "UI/Menu"; // Input action path used to manually toggle the camera.

    [Header("Enable and Disable Camera")]
    [SerializeField] private UpdateType updateType = UpdateType.InputEvent; // Defines when the camera state should be evaluated.
    [Tooltip("Executed in Update and FixedUpdate.")]
    [SerializeField, ValidateReference] private List<GameObject> activationObjects = new(); // List of objects that control camera activation based on their active state.

    [Header("Events")]
    [SerializeField] private bool debug = false; // Enables debug logs for camera state changes.
    [SerializeField] private UnityEvent onEnable; // Event invoked when the camera is enabled.
    [SerializeField] private UnityEvent onDisable; // Event invoked when the camera is disabled.

    private bool isCameraActive; // Indicates whether the camera is currently active.
    private bool lastActivationState; // Stores the previous activation state of the monitored objects.
    private float lastUpdateTime; // Timestamp of the last state evaluation.
    private float activeDuration; // Time the activation objects have been continuously active.
    private float inactiveDuration; // Time the activation objects have been continuously inactive.

    private OnInputSystemEventConfig<float> toggleEvent; // Cached input event listener for the toggle action.

    /// <summary>
    /// Defines how and when the camera state is updated.
    /// </summary>
    public enum UpdateType
    {
        ManualToggle, // Camera state is controlled manually through external method calls.
        InputEvent,   // Camera state is toggled via input events (e.g., button press).
        Update,       // Camera state is evaluated every frame in Update().
        FixedUpdate,  // Camera state is evaluated at fixed intervals in FixedUpdate().
    }

    private void Awake()
    {
        // Disable the component if no input asset is assigned.
        if (!inputAsset)
        {
            Debug.LogWarning("Input Asset not assigned.", this);
            enabled = false;
            return;
        }

        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Initialized with updateType: {updateType}.", this);
    }

    private void Start()
    {
        SetCameraState(true); // Initially set the camera as active.

        // Register input event for toggling the camera if the update type allows it.
        toggleEvent = OnInputSystemEvent<float>.WithAction(inputAsset, toggleActionPath).OnPressed(_ =>
        {
            if (updateType == UpdateType.InputEvent) SetCameraState(!isCameraActive);
        });
    }

    /// <summary>
    /// Cleans up input event bindings when the object is destroyed.
    /// </summary>
    private void OnDestroy() => toggleEvent?.UnbindAll();

    private void Update()
    {
        // If Update mode is selected, evaluate camera state every frame.
        if (updateType == UpdateType.Update) UpdateCameraStateFromObjects();
    }

    private void FixedUpdate()
    {
        // If FixedUpdate mode is selected, evaluate camera state at fixed intervals.
        if (updateType == UpdateType.FixedUpdate) UpdateCameraStateFromObjects();
    }

    /// <summary>
    /// Sets the current camera state, updates cursor lock/visibility,
    /// invokes related events, and logs debug information if enabled.
    /// </summary>
    /// <param name="active">If true, activates the camera; otherwise, deactivates it.</param>
    public void SetCameraState(bool active)
    {
        if (isCameraActive == active) return;

        isCameraActive = active;

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;

        if (active)
        {
            onEnable?.Invoke();
        }
        else
        {
            onDisable?.Invoke();
        }

        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Camera state set to: {active}.", this);
    }

    /// <summary>
    /// Evaluates the active state of monitored objects and toggles the camera accordingly.
    /// Includes a 0.1s threshold to avoid flickering from rapid state changes.
    /// </summary>
    private void UpdateCameraStateFromObjects()
    {
        if (activationObjects.Count == 0) return;

        bool anyActive = IsAnyActivationObjectActive();

        float now = Time.realtimeSinceStartup;
        float deltaTime = now - lastUpdateTime;
        lastUpdateTime = now;

        // If the activation state has changed, reset duration timers.
        if (anyActive != lastActivationState)
        {
            lastActivationState = anyActive;
            activeDuration = 0f;
            inactiveDuration = 0f;
            return;
        }

        // Disable camera if objects remain active for the threshold duration.
        if (anyActive)
        {
            activeDuration += deltaTime;
            if (activeDuration >= 0.1f) SetCameraState(false);
        }
        else // Enable camera if objects remain inactive for the threshold duration.
        {
            inactiveDuration += deltaTime;
            if (inactiveDuration >= 0.1f) SetCameraState(true);
        }
    }

    /// <summary>
    /// Checks whether any of the monitored GameObjects are currently active in the scene.
    /// </summary>
    /// <returns>True if at least one object is active; otherwise, false.</returns>
    private bool IsAnyActivationObjectActive()
    {
        return activationObjects.Exists(obj => obj != null && obj.activeSelf);
    }
}