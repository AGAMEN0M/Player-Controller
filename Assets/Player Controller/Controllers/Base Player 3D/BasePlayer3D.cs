using PlayerController.TransformRuntime;
using PlayerController.PhysicsRuntime;
using PlayerController.InputEvents;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

using static PlayerController.PhysicsRuntime.BoxCollisionSensor;
using static PlayerController.Utils.PlayerUtils;

/// <summary>
/// Handles player input, movement, jumping, rotation, and collision sensors (ground and obstacle).
/// Combines runtime systems like movement controllers and collision sensors into a unified control point.
/// </summary>
[AddComponentMenu("Player Controller/3D/Player Control")]
public class BasePlayer3D : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField, HighlightEmptyReference] private InputActionAsset inputAsset; // Input system asset reference.
    [SerializeField] private string moveInput = "Player/Move"; // Input action path for movement.
    [SerializeField] private string jumpInput = "Player/Jump"; // Input action path for jumping.

    [Header("Player References")]
    [SerializeField, HighlightEmptyReference] private Transform playerTransform; // Target player transform (used for rotation, sensors, etc).
    [SerializeField, HighlightEmptyReference] private Rigidbody targetRigidbody; // Rigidbody used for movement and jumping.

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f; // Horizontal movement speed.
    [SerializeField] private float rotationSpeed = 10f; // Rotation interpolation speed.
    [SerializeField] private float jumpForce = 5f; // Vertical impulse applied during jump.
    [Space(5)]
    [Tooltip("Global input rotation offset in degrees (e.g., 90 = right becomes forward).")]
    [SerializeField] private float worldRotation = 0; // Used to rotate input direction according to world or camera orientation.

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayerMask = -1; // Mask used for all collision detections.

    [Header("Ground Detection Settings")]
    [SerializeField] private Vector3 groundDetectionSize = new(0.2f, 0.3f, 0.2f); // Box size for ground detection.
    [SerializeField, TagDropdown] private List<string> ignoredTags = new() { "Ignore Collision" }; // Tags to ignore during ground detection.

    [Header("Obstacle Detection Settings")]
    [SerializeField] private Vector3 obstacleDetectionCenter = new(0, 0.8f, 0.45f); // Local center of obstacle detection box.
    [SerializeField] private Vector3 obstacleDetectionSize = new(0.25f, 1, 0.2f); // Box size for obstacle detection.
    [SerializeField, TagDropdown] private List<string> obstacleIgnoreTags = new() { "Ignore Collision" }; // Tags to ignore during obstacle detection.

    [Header("Debug Settings")]
    [SerializeField] private bool debugGroundSensor = false; // Toggle ground sensor debug gizmos/logs.
    [SerializeField] private bool debugObstacleSensor = false; // Toggle obstacle sensor debug gizmos/logs.

    // Runtime controllers and systems
    private BoxCollisionSensor groundSensor;
    private BoxCollisionSensor obstacleSensor;
    private PlayerJumpController jumpController;
    private PlayerMoveController moveController;
    private PlayerRotationController rotationController;

    // Input events (jump and movement)
    private OnInputSystemEventConfig<float> jumpEvent;
    private OnInputSystemEventConfig<Vector2> moveEvent;

    private Vector3 playerPosition; // Cached world-space position of the player.
    private Quaternion playerRotation; // Cached world-space rotation of the player.
    private Vector2 moveDirection; // Normalized 2D movement direction (used for rotation and movement).
    private Vector3 obstacleCheckCenter; // World-space position of the obstacle detection box.

    /// <summary>
    /// Validates required references before initialization.
    /// </summary>
    private void Awake()
    {
        if (inputAsset == null) Debug.LogWarning("Input Asset not assigned.", this);
        if (playerTransform == null) Debug.LogWarning("Player Transform not assigned.", this);
        if (targetRigidbody == null) Debug.LogWarning("Rigidbody not assigned.", this);
    }

    /// <summary>
    /// Initializes sensors, input listeners, and movement/rotation controllers.
    /// </summary>
    private void Start()
    {
        // Create ground sensor for detecting contact with the floor.
        groundSensor = new BoxCollisionSensor(
            boxCenterProvider: () => playerPosition,
            boxSizeProvider: () => groundDetectionSize,
            boxRotationProvider: () => playerRotation,
            collisionLayerMaskProvider: () => collisionLayerMask,
            triggerInteractionProvider: () => QueryTriggerInteraction.Ignore,
            filterModeProvider: () => DetectionFilter.All,
            referenceParentTransformProvider: () => playerTransform,
            ignoredTagsProvider: () => new HashSet<string>(ignoredTags),
            enableDetectionProvider: () => true,
            enableDebugLogProvider: () => debugGroundSensor,
            gizmoTargetObjectProvider: () => gameObject,
            gizmoDrawingModeProvider: () => GizmoDisplayMode.SelectedOnly,
            gizmosColorProvider: () => Color.red
        );

        // Create obstacle sensor for detecting walls/objects in front of the player.
        obstacleSensor = new BoxCollisionSensor(
            boxCenterProvider: () => obstacleCheckCenter,
            boxSizeProvider: () => obstacleDetectionSize,
            boxRotationProvider: () => playerRotation,
            collisionLayerMaskProvider: () => collisionLayerMask,
            triggerInteractionProvider: () => QueryTriggerInteraction.Ignore,
            filterModeProvider: () => DetectionFilter.All,
            referenceParentTransformProvider: () => playerTransform,
            ignoredTagsProvider: () => new HashSet<string>(obstacleIgnoreTags),
            enableDetectionProvider: () => true,
            enableDebugLogProvider: () => debugObstacleSensor,
            gizmoTargetObjectProvider: () => gameObject,
            gizmoDrawingModeProvider: () => GizmoDisplayMode.SelectedOnly,
            gizmosColorProvider: () => Color.yellow
        );

        // Create movement-related controllers.
        jumpController = new PlayerJumpController(targetRigidbody: () => targetRigidbody, isGrounded: () => groundSensor.collisionDetected);
        moveController = new PlayerMoveController(targetRigidbody: () => targetRigidbody, speed: () => moveSpeed, isObstacle: () => obstacleSensor.collisionDetected);
        rotationController = new PlayerRotationController(targetTransform: () => playerTransform, direction: () => moveDirection, speed: () => rotationSpeed);

        // Register input events.
        jumpEvent = OnInputSystemEvent<float>.WithAction(inputAsset, jumpInput).OnPressed(_ => jumpController.OnJump(jumpForce));
        moveEvent = OnInputSystemEvent<Vector2>.WithAction(inputAsset, moveInput).OnHold(value =>
        {
            // Apply world rotation offset to input and update controllers.
            moveDirection = ConvertRotation(value, worldRotation);
            moveController.OnMove(moveDirection);
        }).OnReleased(() =>
        { 
            moveController.OnStop();
            moveDirection = Vector2.zero;
        });
    }

    /// <summary>
    /// Cleans up all systems and listeners on destruction.
    /// </summary>
    private void OnDestroy()
    {
        BoxCollisionSensor.Dispose(ref groundSensor);
        BoxCollisionSensor.Dispose(ref obstacleSensor);
        PlayerJumpController.Dispose(ref jumpController);
        PlayerMoveController.Dispose(ref moveController);
        PlayerRotationController.Dispose(ref rotationController);

        jumpEvent?.UnbindAll();
        moveEvent?.UnbindAll();
    }

    /// <summary>
    /// Updates cached transform data used by sensors and controllers.
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        playerPosition = playerTransform.position;
        playerRotation = playerTransform.rotation;
        obstacleCheckCenter = playerTransform.TransformPoint(obstacleDetectionCenter);
    }
}