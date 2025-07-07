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
using UnityEngine.InputSystem;
using UnityEngine;

[AddComponentMenu("Player Controller/3D/Extra Modules/Camera Controller (Third Person)")]
public class ThirdPersonCameraController : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Input Settings")]
    [SerializeField, ValidateReference] private InputActionAsset inputActions; // Input action asset used for input binding.
    [SerializeField] private string zoomActionPath = "UI/ScrollWheel"; // Action path for zooming input.

    [Header("References")]
    [SerializeField, ValidateReference] private LookInputHandler lookInputHandler; // Input handler for camera rotation.
    [SerializeField, ValidateReference] private Transform playerTransform; // Root transform of the player.
    [SerializeField, ValidateReference] private Transform playerPivotTransform; // Pivot from which the camera rotates.
    [SerializeField, ValidateReference] private Transform mainCameraPivotTransform; // Transform used to orient the camera.
    [SerializeField, ValidateReference] private Transform collisionCameraPivotTransform; // Camera transform that gets pushed on collision.

    [Header("Camera Settings")]
    [SerializeField, Range(0.05f, 1f)] private float cameraSensitivity = 0.15f; // Sensitivity multiplier for camera rotation.
    [SerializeField] private Vector2 angleSensitivity = new(1f, 1f); // Per-axis sensitivity for yaw and pitch.
    [Space(5)]
    [SerializeField] private float minPitch = -40f; // Minimum vertical angle.
    [SerializeField] private float maxPitch = 90f;  // Maximum vertical angle.
    [Space(5)]
    [SerializeField] private bool invertY = false; // Whether to invert the vertical look input.

    [Header("Distance Settings")]
    [SerializeField, Min(0.1f)] private float minDistance = 1f; // Minimum distance the camera can zoom in.
    [SerializeField, Min(0f)] private float maxDistance = 5f; // Maximum distance the camera can zoom out.
    [Space(5)]
    [SerializeField, Min(0.1f)] private float minCollisionDistance = 0.1f; // Minimum camera distance when colliding.
    [Space(5)]
    [SerializeField] private float zoomSpeed = 20f; // Speed at which the camera zooms in/out.
    [SerializeField] private float collisionDistanceSmoothSpeed = 100f; // Speed of smoothing camera collision movement.
    [Space(5)]
    [SerializeField] private float targetCameraDistance = -2f; // Desired camera distance from the pivot.

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionDetectionLayers = -1; // Layers considered for collision detection.
    [SerializeField] private float collisionDetectionOffset = 0.3f; // Offset to avoid clipping when hitting surfaces.

    #endregion

    #region === Private Fields ===

    private static readonly RaycastHit[] raycastHitsBuffer = new RaycastHit[8]; // Reusable array for non-alloc raycasting.
    private OnInputSystemEventConfig<Vector2> zoomInputEvent; // Zoom input event binding.
    private float currentPitchAngle; // Current vertical angle.
    private float currentYawAngle;   // Current horizontal angle.
    private bool isCameraActive = true; // Whether camera logic is active.
    private float currentCameraDistance; // Current distance between pivot and camera.

    private float NegativeMaxDistance => -minDistance;
    private float NegativeMinDistance => -maxDistance;

    #endregion

    #region === Unity Callbacks ===

    private void Awake()
    {
        // Validate required references.
        if (!inputActions) Debug.LogWarning("inputActions not assigned.", this);
        if (!lookInputHandler) Debug.LogWarning("LookInputHandler not assigned.", this);
        if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
        if (!playerPivotTransform) Debug.LogWarning("Player Pivot Transform not assigned.", this);
        if (!mainCameraPivotTransform) Debug.LogWarning("Main Camera Pivot Transform not assigned.", this);
        if (!collisionCameraPivotTransform) Debug.LogWarning("Collision Camera Pivot Transform not assigned.", this);
    }

    private void Start()
    {
        // Clamp and initialize camera distance.
        targetCameraDistance = Mathf.Clamp(targetCameraDistance, NegativeMinDistance, NegativeMaxDistance);
        currentCameraDistance = targetCameraDistance;

        // Bind zoom input from the input system.
        zoomInputEvent = OnInputSystemEvent<Vector2>.WithAction(inputActions, zoomActionPath, () => isCameraActive)
            .OnHold(value =>
            {
                targetCameraDistance = Mathf.Clamp(targetCameraDistance + value.y * zoomSpeed * Time.deltaTime, NegativeMinDistance, NegativeMaxDistance);
            });

        ProcessCameraCollision(); // Initial collision check to prevent camera starting inside geometry.
    }

    // Clean up input bindings on destroy.
    private void OnDestroy() => zoomInputEvent?.UnbindAll();

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
        Vector2 lookDelta = lookInputHandler.lookDirection * cameraSensitivity;

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

    /// <summary>Set camera sensitivity between 0.05 and 1.</summary>
    public void SetCameraSensitivity(float sensitivity) => cameraSensitivity = Mathf.Clamp(sensitivity, 0.05f, 1f);

    /// <summary>Enable or disable Y-axis inversion for camera look.</summary>
    public void SetInvertYAxis(bool enabled) => invertY = enabled;

    /// <summary>Enable or disable camera logic processing.</summary>
    public void SetCameraActive(bool active) => isCameraActive = active;

    #endregion
}