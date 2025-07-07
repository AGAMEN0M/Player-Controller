/*
 * ---------------------------------------------------------------------------
 * Description: First-person camera controller for Unity.
 *              Rotates the player horizontally and camera vertically using input data.
 *              Includes pitch clamping and Y-axis inversion support.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

[AddComponentMenu("Player Controller/3D/Extra Modules/Camera Controller (First Person)")]
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ValidateReference] private LookInputHandler lookInputHandler; // Input handler for camera look.
    [SerializeField, ValidateReference] private Transform playerTransform; // Player's transform (rotated on Y axis).
    [SerializeField, ValidateReference] private Transform cameraPivot; // Camera pivot for pitch rotation (X axis).

    [Header("Camera Settings")]
    [SerializeField, Range(0.05f, 1)] private float cameraSensitivity = 0.15f; // Global sensitivity multiplier.
    [SerializeField] private Vector2 angleSensitivity = new(1f, 1f); // Angle multipliers (X = yaw, Y = pitch).
    [Space(5)]
    [SerializeField] private float minPitch = -80f; // Minimum pitch angle (look down limit).
    [SerializeField] private float maxPitch = 80f;  // Maximum pitch angle (look up limit).
    [Space(5)]
    [SerializeField] private bool invertY = false; // Inverts vertical input when true.

    private float currentPitch; // Tracks current pitch (vertical camera rotation).

    /// <summary>
    /// Validates required references.
    /// </summary>
    private void Awake()
    {
        if (!lookInputHandler) Debug.LogWarning("LookInputHandler not assigned.", this);
        if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
        if (!cameraPivot) Debug.LogWarning("Camera Pivot not assigned.", this);
    }

    /// <summary>
    /// Handles camera movement each frame.
    /// </summary>
    private void Update() => HandleMouseLook();

    /// <summary>
    /// Applies look input to rotate player and camera.
    /// </summary>
    private void HandleMouseLook()
    {
        if (!lookInputHandler || !playerTransform || !cameraPivot) return;

        Vector2 lookInput = lookInputHandler.lookDirection * cameraSensitivity;

        float yaw = lookInput.x * angleSensitivity.x;
        float pitch = lookInput.y * angleSensitivity.y * (invertY ? 1f : -1f);

        // Yaw rotation (left/right): applies to player.
        playerTransform.Rotate(Vector3.up * yaw);

        // Pitch rotation (up/down): applies to camera pivot.
        currentPitch = Mathf.Clamp(currentPitch + pitch, minPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }

    /// <summary>
    /// Sets the base camera sensitivity multiplier.
    /// </summary>
    /// <param name="sensitivity">New sensitivity value (clamped between 0.05 and 1.0).</param>
    public void SetSensitivity(float sensitivity)
    {
        cameraSensitivity = Mathf.Clamp(sensitivity, 0.05f, 1f);
    }

    /// <summary>
    /// Enables or disables Y-axis inversion for camera pitch.
    /// </summary>
    /// <param name="enabled">True to invert Y-axis, false otherwise.</param>
    public void SetInvertY(bool enabled) => invertY = enabled;
}