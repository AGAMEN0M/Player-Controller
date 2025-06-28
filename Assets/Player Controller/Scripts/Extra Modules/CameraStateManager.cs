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
    [SerializeField, HighlightEmptyReference] private InputActionAsset inputAsset; // Input Action Asset containing defined actions.
    [SerializeField] private string toggleInput = "UI/Menu"; // Action name used to toggle camera manually.

    [Header("Enable and Disable Camera")]
    [SerializeField] private UpdateType updateType = UpdateType.InputEvent; // Defines when camera state should be checked.
    [Tooltip("Executed in Update and FixedUpdate")]
    [SerializeField, HighlightEmptyReference] private List<GameObject> activationObjects = new(); // Objects that determine camera activation.

    [Header("Events")]
    [SerializeField] private bool debug = false; // Enables debug logging for camera state changes.
    [SerializeField] private UnityEvent onEnable; // Event invoked when camera is enabled.
    [SerializeField] private UnityEvent onDisable; // Event invoked when camera is disabled.

    private bool isCameraActive; // Indicates whether the camera is currently active.
    private bool lastActivationState; // Stores the last known activation state of the monitored objects.
    private float lastUpdateTime; // Time of the last activation state check.
    private float activeDuration; // Duration the activation objects have remained active.
    private float inactiveDuration; // Duration the activation objects have remained inactive.

    private OnInputSystemEventConfig<float> toggleEvent; // Cached input event listener for toggle action.

    /// <summary>
    /// Defines how the camera state is updated.
    /// </summary>
    public enum UpdateType
    {
        ManualToggle, // Camera is only toggled via external method calls.
        InputEvent,   // Camera is toggled on input event (e.g., button press).
        Update,       // Camera state is checked every frame in Update().
        FixedUpdate,  // Camera state is checked in FixedUpdate().
    }

    private void Awake()
    {
        // Disable component if no input asset is assigned.
        if (!inputAsset)
        {
            Debug.LogWarning("Input Asset not assigned.", this);
            enabled = false;
            return;
        }

        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Initialized with updateType: {updateType}", this);
    }

    private void Start()
    {
        SetCameraState(true); // Set camera initially active.

        // Register input event to toggle camera if updateType allows it.
        toggleEvent = OnInputSystemEvent<float>.WithAction(inputAsset, toggleInput).OnPressed(_ =>
        {
            if (updateType == UpdateType.InputEvent) SetCameraState(!isCameraActive);
        });
    }

    // Clean up input event bindings when object is destroyed.
    private void OnDestroy() => toggleEvent?.UnbindAll();

    private void Update()
    {
        // If Update mode is selected, check camera state every frame.
        if (updateType == UpdateType.Update) UpdateCameraStateFromObjects();
    }

    private void FixedUpdate()
    {
        // If FixedUpdate mode is selected, check camera state in fixed intervals.
        if (updateType == UpdateType.FixedUpdate) UpdateCameraStateFromObjects();
    }

    /// <summary>
    /// Sets the current camera state and updates cursor lock and visibility.
    /// Also triggers UnityEvents and optionally logs debug info.
    /// </summary>
    /// <param name="active">If true, camera is activated; otherwise, deactivated.</param>
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

        if (debug) Debug.Log($"[{nameof(CameraStateManager)}] Camera State set to: {active}", this);
    }

    /// <summary>
    /// Checks if monitored objects are active or not and updates camera state accordingly.
    /// Uses a time threshold (0.1s) to prevent immediate toggling on flickering.
    /// </summary>
    private void UpdateCameraStateFromObjects()
    {
        if (activationObjects.Count == 0) return;

        bool anyActive = IsAnyActivationObjectActive();

        float now = Time.realtimeSinceStartup;
        float deltaTime = now - lastUpdateTime;
        lastUpdateTime = now;

        // If activation state changed, reset timers.
        if (anyActive != lastActivationState)
        {
            lastActivationState = anyActive;
            activeDuration = 0f;
            inactiveDuration = 0f;
            return;
        }

        // If objects remain active for threshold time, disable camera.
        if (anyActive)
        {
            activeDuration += deltaTime;
            if (activeDuration >= 0.1f) SetCameraState(false);
        }
        else // If objects remain inactive for threshold time, enable camera.
        {
            inactiveDuration += deltaTime;
            if (inactiveDuration >= 0.1f) SetCameraState(true);
        }
    }

    /// <summary>
    /// Checks whether any of the monitored objects are currently active in the scene.
    /// </summary>
    /// <returns>True if at least one object is active; false otherwise.</returns>
    private bool IsAnyActivationObjectActive()
    {
        return activationObjects.Exists(obj => obj != null && obj.activeSelf);
    }
}