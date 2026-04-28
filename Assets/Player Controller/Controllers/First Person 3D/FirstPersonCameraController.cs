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

using PlayerController.Attributes;
using UnityEngine;

namespace PlayerController
{
    [AddComponentMenu("Tools/Player Controller/3D/Extra Modules/Camera Controller (First Person)")]
    public class FirstPersonCameraController : MonoBehaviour
    {
        #region === Serialized Fields ===

        [Header("References")]
        [SerializeField, ValidateReference, Tooltip("Input handler responsible for capturing camera look input.")]
        private LookInputHandler lookInputHandler;

        [SerializeField, ValidateReference, Tooltip("Player's transform (rotated on Y axis for horizontal rotation).")]
        private Transform playerTransform;

        [SerializeField, ValidateReference, Tooltip("Camera pivot transform used for vertical rotation (pitch).")]
        private Transform cameraPivot;

        [Header("Camera Settings")]
        [SerializeField, Range(0.05f, 1), Tooltip("Global multiplier for camera sensitivity.")]
        private float cameraSensitivity = 0.15f;

        [SerializeField, Tooltip("Separate sensitivity multipliers for yaw (X) and pitch (Y) axes.")]
        private Vector2 angleSensitivity = new(1f, 1f);

        [Space(5)]
        [SerializeField, Tooltip("Minimum vertical angle the camera can rotate to (looking down).")]
        private float minPitch = -80f;

        [SerializeField, Tooltip("Maximum vertical angle the camera can rotate to (looking up).")]
        private float maxPitch = 80f;

        [Space(5)]
        [SerializeField, Tooltip("Invert vertical camera input when true.")]
        private bool invertY = false;

        #endregion

        #region === Runtime Fields ===

        private float currentPitch; // Tracks current pitch (vertical camera rotation).

        #endregion

        #region === Properties ===

        /// <summary>
        /// Gets or sets the input handler for camera look.
        /// </summary>
        public LookInputHandler LookInputHandler
        {
            get => lookInputHandler;
            set => lookInputHandler = value;
        }

        /// <summary>
        /// Gets or sets the player's Transform for horizontal rotation.
        /// </summary>
        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        /// <summary>
        /// Gets or sets the camera pivot Transform for pitch rotation.
        /// </summary>
        public Transform CameraPivot
        {
            get => cameraPivot;
            set => cameraPivot = value;
        }

        /// <summary>
        /// Gets or sets the global camera sensitivity multiplier.
        /// Use SetSensitivity() for clamped control.
        /// </summary>
        public float CameraSensitivity
        {
            get => cameraSensitivity;
            set => cameraSensitivity = value;
        }

        /// <summary>
        /// Gets or sets separate angle multipliers for yaw (X) and pitch (Y).
        /// </summary>
        public Vector2 AngleSensitivity
        {
            get => angleSensitivity;
            set => angleSensitivity = value;
        }

        /// <summary>
        /// Gets or sets the minimum pitch angle (look down limit).
        /// </summary>
        public float MinPitch
        {
            get => minPitch;
            set => minPitch = value;
        }

        /// <summary>
        /// Gets or sets the maximum pitch angle (look up limit).
        /// </summary>
        public float MaxPitch
        {
            get => maxPitch;
            set => maxPitch = value;
        }

        /// <summary>
        /// Gets or sets whether vertical input is inverted.
        /// Use SetInvertY() to change at runtime safely.
        /// </summary>
        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        #endregion

        #region === Unity Methods ===

        /// <summary>
        /// Validates required references.
        /// </summary>
        private void Awake()
        {
            if (!lookInputHandler) Debug.LogWarning("LookInputHandler not assigned.", this);
            if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
            if (!cameraPivot) Debug.LogWarning("Camera Pivot not assigned.", this);
            if (minPitch > maxPitch) Debug.LogError("Minimum pitch cannot be greater than maximum pitch.", this);
        }

        /// <summary>
        /// Handles camera movement each frame.
        /// </summary>
        private void Update() => HandleMouseLook();

        #endregion

        #region === Camera Control Methods ===

        /// <summary>
        /// Applies look input to rotate player and camera.
        /// </summary>
        private void HandleMouseLook()
        {
            if (!lookInputHandler || !playerTransform || !cameraPivot) return;

            Vector2 lookInput = lookInputHandler.LookDirection * cameraSensitivity;

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
        public void SetSensitivity(float sensitivity) => cameraSensitivity = Mathf.Clamp(sensitivity, 0.05f, 1f);

        /// <summary>
        /// Enables or disables Y-axis inversion for camera pitch.
        /// </summary>
        /// <param name="enabled">True to invert Y-axis, false otherwise.</param>
        public void SetInvertY(bool enabled) => invertY = enabled;

        #endregion
    }
}