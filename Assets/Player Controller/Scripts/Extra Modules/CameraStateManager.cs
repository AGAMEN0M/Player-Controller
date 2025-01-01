using System.Collections.Generic;
using CustomKeyboard;
using UnityEngine;

[AddComponentMenu("Player/Extra Modules/Camera State Manager")]
public class CameraStateManager : MonoBehaviour
{
    [Header("Enable and Disable Camera")]
    [SerializeField][KeyboardTagDropdown] private string cameraToggleInputTag = "MenuActivation"; // Input tag used for toggling the camera.
    [Space(10)]
    [Tooltip("The objects whose activation will be checked to toggle the camera state.")]
    [HighlightEmptyReference] public List<GameObject> activationObjects = new(); // List of objects checked for camera activation.

    private bool isCameraActive = true; // Tracks whether the camera is enabled.
    private bool lastActivationState; // Stores the previous activation state of the camera activation objects.

    private float lastUpdateTime = 0f; // Timestamp of the last update.
    private float activeDuration = 0f; // Accumulates the time the camera activation objects have been active.
    private float inactiveDuration = 0f; // Accumulates the time the camera activation objects have been inactive.

    private InputData cameraToggleInput; // Stores input data for toggling the camera.

    private void Start()
    {
        // Initialize camera toggle input based on the assigned tag.
        cameraToggleInput = KeyboardTagHelper.GetInputFromTag(cameraToggleInputTag);
        // Set the initial camera state and lock the cursor if the camera is enabled at the start.
        SetCameraState(isCameraActive);
    }

    public void ToggleCameraState(out bool currentCameraState)
    {
        // Toggle the camera state based on the activation state of objects.
        UpdateCameraStateBasedOnActivation();
        currentCameraState = isCameraActive; // Return the current camera state.
    }

    public void SetCameraState(bool active)
    {
        // Set the camera state and update the cursor lock state accordingly.
        isCameraActive = active;
        Cursor.lockState = isCameraActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isCameraActive; // Hide cursor when the camera is active.
    }

    private void UpdateCameraStateBasedOnActivation()
    {
        if (activationObjects.Count > 0)
        {
            bool currentActivationState = false; // Tracks the current activation state.

            // Check if any of the activation objects are active.
            foreach (var obj in activationObjects)
            {
                if (obj != null && obj.activeSelf)
                {
                    currentActivationState = true;
                    break;
                }
            }

            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - lastUpdateTime; // Calculate the time since the last update.
            lastUpdateTime = currentTime;

            // If the activation state has changed, reset the duration counters.
            if (currentActivationState != lastActivationState)
            {
                lastActivationState = currentActivationState;
                activeDuration = 0f;
                inactiveDuration = 0f;
            }
            else
            {
                // Update the duration counters based on the current activation state.
                UpdateDurationCounters(currentActivationState, elapsedTime);
            }
        }
        else
        {
            // If there are no activation objects, toggle the camera based on user input.
            if (Input.GetKeyDown(cameraToggleInput.keyboard)) SetCameraState(!isCameraActive);
        }
    }

    private void UpdateDurationCounters(bool isActive, float elapsedTime)
    {
        if (isActive)
        {
            activeDuration += elapsedTime; // Increment the active duration counter.

            // Disable the camera if the activation objects have been active for more than 0.1 seconds.
            if (activeDuration >= 0.1f)
            {
                isCameraActive = false;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        else
        {
            inactiveDuration += elapsedTime; // Increment the inactive duration counter.

            // Enable the camera if the activation objects have been inactive for more than 0.1 seconds.
            if (inactiveDuration >= 0.1f)
            {
                isCameraActive = true;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}