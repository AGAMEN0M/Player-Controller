/*
 * ---------------------------------------------------------------------------
 * Description: Third-person camera controller with collision handling and
 *              dynamic zoom using Unity's Input System.
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
    [AddComponentMenu("Tools/Player Controller/3D/Extra Modules/Camera Controller (Third Person)")]
    public class ThirdPersonCameraController : MonoBehaviour
    {
        #region === Serialized Fields ===

        [Header("Input Settings")]
        [SerializeField, ValidateReference, Tooltip("Input action for zooming the camera in/out.")]
        private InputActionReference zoomAction;

        [Header("References")]
        [SerializeField, ValidateReference, Tooltip("Input handler responsible for capturing camera rotation.")]
        private LookInputHandler lookInputHandler;

        [SerializeField, ValidateReference, Tooltip("Root transform of the player.")]
        private Transform playerTransform;

        [SerializeField, ValidateReference, Tooltip("Pivot transform from which the camera rotates.")]
        private Transform playerPivotTransform;

        [SerializeField, ValidateReference, Tooltip("Transform used to orient the camera for rotation.")]
        private Transform mainCameraPivotTransform;

        [SerializeField, ValidateReference, Tooltip("Camera transform that adjusts when colliding with objects.")]
        private Transform collisionCameraPivotTransform;

        [Header("Camera Settings")]
        [SerializeField, Range(0.05f, 1f), Tooltip("Global multiplier for camera rotation sensitivity.")]
        private float cameraSensitivity = 0.15f;

        [SerializeField, Tooltip("Separate sensitivity multipliers for yaw (X) and pitch (Y) axes.")]
        private Vector2 angleSensitivity = new(1f, 1f);

        [Space(5)]

        [SerializeField, Tooltip("Minimum vertical angle the camera can rotate to (looking down).")]
        private float minPitch = -40f;

        [SerializeField, Tooltip("Maximum vertical angle the camera can rotate to (looking up).")]
        private float maxPitch = 90f;

        [Space(5)]

        [SerializeField, Tooltip("Invert vertical camera input when true.")]
        private bool invertY = false;

        [Header("Distance Settings")]
        [SerializeField, Min(0.1f), Tooltip("Minimum zoom distance of the camera.")]
        private float minDistance = 1f;

        [SerializeField, Min(0f), Tooltip("Maximum zoom distance of the camera.")]
        private float maxDistance = 5f;

        [Space(5)]

        [SerializeField, Min(0.1f), Tooltip("Minimum camera distance when colliding with objects.")]
        private float minCollisionDistance = 0.1f;

        [Space(5)]

        [SerializeField, Tooltip("Speed at which the camera zooms in/out.")]
        private float zoomSpeed = 20f;

        [SerializeField, Tooltip("Speed of smoothing camera collision adjustments.")]
        private float collisionDistanceSmoothSpeed = 100f;

        [Space(5)]

        [SerializeField, Tooltip("Target camera distance from pivot.")]
        private float targetCameraDistance = -2f;

        [Header("Collision Settings")]
        [SerializeField, Tooltip("Layers used for camera collision detection.")]
        private LayerMask collisionDetectionLayers = -1;

        [SerializeField, Tooltip("Offset applied to avoid camera clipping into surfaces.")]
        private float collisionDetectionOffset = 0.3f;

        #endregion

        #region === Private Fields ===

        private static readonly RaycastHit[] raycastHitsBuffer = new RaycastHit[8]; // Reusable array for non-alloc raycasting.
        private OnInputSystemEventConfig<Vector2> zoomInputEvent; // Zoom input event binding.
        private float currentPitchAngle; // Current vertical angle.
        private float currentYawAngle;   // Current horizontal angle.
        private bool isCameraActive = true; // Whether camera logic is active.
        private float currentCameraDistance; // Current distance between pivot and camera.

        private float NegativeMaxDistance => -minDistance; // Negative equivalent of minimum zoom distance.
        private float NegativeMinDistance => -maxDistance; // Negative equivalent of maximum zoom distance.

        #endregion

        #region === Properties ===

        /// <summary>
        /// Gets or sets the input action used for camera zoom.
        /// </summary>
        public InputActionReference ZoomAction
        {
            get => zoomAction;
            set => zoomAction = value;
        }

        /// <summary>
        /// Gets or sets the input handler for camera rotation.
        /// </summary>
        public LookInputHandler LookInputHandler
        {
            get => lookInputHandler;
            set => lookInputHandler = value;
        }

        /// <summary>
        /// Gets or sets the player's root transform.
        /// </summary>
        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        /// <summary>
        /// Gets or sets the pivot transform used for camera rotation.
        /// </summary>
        public Transform PlayerPivotTransform
        {
            get => playerPivotTransform;
            set => playerPivotTransform = value;
        }

        /// <summary>
        /// Gets or sets the main camera pivot transform.
        /// </summary>
        public Transform MainCameraPivotTransform
        {
            get => mainCameraPivotTransform;
            set => mainCameraPivotTransform = value;
        }

        /// <summary>
        /// Gets or sets the collision-adjusted camera transform.
        /// </summary>
        public Transform CollisionCameraPivotTransform
        {
            get => collisionCameraPivotTransform;
            set => collisionCameraPivotTransform = value;
        }

        /// <summary>
        /// Gets or sets separate sensitivity multipliers for yaw and pitch axes.
        /// </summary>
        public Vector2 AngleSensitivity
        {
            get => angleSensitivity;
            set => angleSensitivity = value;
        }

        /// <summary>Gets or sets minimum vertical rotation angle.
        /// </summary>
        public float MinPitch
        {
            get => minPitch;
            set => minPitch = value;
        }

        /// <summary>
        /// Gets or sets maximum vertical rotation angle.
        /// </summary>
        public float MaxPitch
        {
            get => maxPitch;
            set => maxPitch = value;
        }

        /// <summary>
        /// Gets or sets minimum zoom distance.
        /// </summary>
        public float MinDistance
        {
            get => minDistance;
            set => minDistance = value;
        }

        /// <summary>
        /// Gets or sets maximum zoom distance.
        /// </summary>
        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = value;
        }

        /// <summary>
        /// Gets or sets minimum camera collision distance.
        /// </summary>
        public float MinCollisionDistance
        {
            get => minCollisionDistance;
            set => minCollisionDistance = value;
        }

        /// <summary>
        /// Gets or sets the camera zoom speed.
        /// </summary>
        public float ZoomSpeed
        {
            get => zoomSpeed;
            set => zoomSpeed = value;
        }

        /// <summary>
        /// Gets or sets smoothing speed for camera collision adjustments.
        /// </summary>
        public float CollisionDistanceSmoothSpeed
        {
            get => collisionDistanceSmoothSpeed;
            set => collisionDistanceSmoothSpeed = value;
        }

        /// <summary>
        /// Gets or sets target camera distance from pivot.
        /// </summary>
        public float TargetCameraDistance
        {
            get => targetCameraDistance;
            set => targetCameraDistance = value;
        }

        /// <summary>
        /// Gets or sets collision layers for camera detection.
        /// </summary>
        public LayerMask CollisionDetectionLayers
        {
            get => collisionDetectionLayers;
            set => collisionDetectionLayers = value;
        }

        /// <summary>
        /// Gets or sets collision offset for camera placement.
        /// </summary>
        public float CollisionDetectionOffset
        {
            get => collisionDetectionOffset;
            set => collisionDetectionOffset = value;
        }

        #endregion

        #region === Unity Callbacks ===

        private void Awake()
        {
            // Validate required references.
            if (zoomAction == null) Debug.LogWarning("Zoom Action not assigned.", this);
            if (!lookInputHandler) Debug.LogWarning("LookInputHandler not assigned.", this);
            if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
            if (!playerPivotTransform) Debug.LogWarning("Player Pivot Transform not assigned.", this);
            if (!mainCameraPivotTransform) Debug.LogWarning("Main Camera Pivot Transform not assigned.", this);
            if (!collisionCameraPivotTransform) Debug.LogWarning("Collision Camera Pivot Transform not assigned.", this);
            if (minPitch > maxPitch) Debug.LogError("Minimum pitch cannot be greater than maximum pitch.", this);
            if (minDistance > maxDistance) Debug.LogError("Minimum distance cannot be greater than maximum distance.", this);
        }

        private void Start()
        {
            // Clamp and initialize camera distance.
            targetCameraDistance = Mathf.Clamp(targetCameraDistance, NegativeMinDistance, NegativeMaxDistance);
            currentCameraDistance = targetCameraDistance;

            // Bind zoom input from the input system.
            zoomInputEvent = OnInputSystemEvent<Vector2>.WithAction(zoomAction, this, () => isCameraActive)
                .OnHold(value =>
                {
                    targetCameraDistance = Mathf.Clamp(targetCameraDistance + value.y * zoomSpeed * Time.deltaTime, NegativeMinDistance, NegativeMaxDistance);
                });

            ProcessCameraCollision(); // Initial collision check to prevent camera starting inside geometry.
        }

        // Clean up input bindings on destroy.
        private void OnDestroy() => zoomInputEvent?.Dispose();

        private void Update()
        {
            ProcessLookInput();       // Update rotation.
            ProcessCameraCollision(); // Update camera collision and positioning.
        }

        private void OnDrawGizmosSelected()
        {
            // Visual debug for camera pivots.
            if (mainCameraPivotTransform == null || collisionCameraPivotTransform == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(mainCameraPivotTransform.position, 0.03f);
            Gizmos.DrawSphere(collisionCameraPivotTransform.position, 0.03f);
            Gizmos.DrawLine(mainCameraPivotTransform.position, collisionCameraPivotTransform.position);
        }

        #endregion

        #region === Camera Logic ===

        /// <summary>
        /// Handles camera rotation based on input.
        /// </summary>
        private void ProcessLookInput()
        {
            if (lookInputHandler == null || playerPivotTransform == null || mainCameraPivotTransform == null) return;

            // Move pivot to player position.
            mainCameraPivotTransform.position = playerPivotTransform.position;

            // Apply sensitivity to input.
            Vector2 lookDelta = lookInputHandler.LookDirection * cameraSensitivity;

            // Adjust yaw and pitch with optional Y inversion.
            float yawChange = lookDelta.x * angleSensitivity.x;
            float pitchChange = lookDelta.y * angleSensitivity.y * (invertY ? 1f : -1f);

            currentYawAngle += yawChange;
            currentPitchAngle = Mathf.Clamp(currentPitchAngle + pitchChange, minPitch, maxPitch);

            // Apply rotation.
            mainCameraPivotTransform.eulerAngles = new Vector3(currentPitchAngle, currentYawAngle, 0f);
        }

        /// <summary>
        /// Handles collision detection and smoothly adjusts camera distance to avoid obstacles.
        /// </summary>
        private void ProcessCameraCollision()
        {
            if (collisionCameraPivotTransform == null || playerPivotTransform == null) return;

            Vector3 origin = playerPivotTransform.position;
            Vector3 desiredPosition = origin + mainCameraPivotTransform.rotation * new Vector3(0, 0, targetCameraDistance);

            Vector3 direction = desiredPosition - origin;
            float maxDistance = direction.magnitude;
            if (maxDistance <= 0f) return;
            direction /= maxDistance;

            float adjustedDistance = targetCameraDistance;
            float closestDistance = float.MaxValue;
            bool validHitFound = false;

            // Use non-alloc raycast to avoid GC.
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                raycastHitsBuffer,
                maxDistance + collisionDetectionOffset,
                collisionDetectionLayers,
                QueryTriggerInteraction.Ignore
            );

            // Check for closest valid hit that is not a child of the player.
            for (int i = 0; i < hitCount; i++)
            {
                var hit = raycastHitsBuffer[i];
                if (hit.collider.transform.IsChildOf(playerTransform)) continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    validHitFound = true;
                }
            }

            // Adjust distance if a valid hit was found.
            if (validHitFound)
            {
                float correctedDistance = Mathf.Max(closestDistance - collisionDetectionOffset, minCollisionDistance);
                adjustedDistance = -Mathf.Clamp(correctedDistance, minCollisionDistance, this.maxDistance);
            }

            // Smoothly update camera distance.
            currentCameraDistance = Mathf.Lerp(currentCameraDistance, adjustedDistance, collisionDistanceSmoothSpeed * Time.deltaTime);
            currentCameraDistance = Mathf.Clamp(currentCameraDistance, NegativeMinDistance, -minCollisionDistance);

            // Apply final position to the collision pivot.
            Vector3 localPosition = collisionCameraPivotTransform.localPosition;
            localPosition.z = currentCameraDistance;
            collisionCameraPivotTransform.localPosition = localPosition;
        }

        #endregion

        #region === Public API ===

        /// <summary>
        /// Sets the global camera rotation sensitivity multiplier.
        /// Clamps the value between 0.05 (very slow) and 1.0 (full speed).
        /// </summary>
        /// <param name="sensitivity">New sensitivity value to apply.</param>
        public void SetCameraSensitivity(float sensitivity) => cameraSensitivity = Mathf.Clamp(sensitivity, 0.05f, 1f);

        /// <summary>
        /// Enables or disables vertical axis inversion for the camera.
        /// When enabled, moving the input upwards will rotate the camera downwards and vice versa.
        /// </summary>
        /// <param name="enabled">True to invert Y-axis, false to use normal orientation.</param>
        public void SetInvertYAxis(bool enabled) => invertY = enabled;

        /// <summary>
        /// Enables or disables all camera processing logic.
        /// When disabled, the camera will ignore rotation and collision updates.
        /// </summary>
        /// <param name="active">True to enable camera logic, false to disable.</param>
        public void SetCameraActive(bool active) => isCameraActive = active;

        #endregion
    }
}