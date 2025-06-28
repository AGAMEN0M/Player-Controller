using PlayerController.TransformRuntime;
using PlayerController.PhysicsRuntime;
using PlayerController.InputEvents;
using PlayerController.CustomData;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

using static PlayerController.PhysicsRuntime.BoxCollisionSensor;
using static PlayerController.Utils.PlayerUtils;

/// <summary>
/// Player controller for 3D side view platformer-style games.
/// Handles movement, jump, crouch logic, and dynamic sensor detection.
/// </summary>
[AddComponentMenu("Player Controller/3D/Player Controller (Side View)")]
public class SideViewPlayerController3D : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField, HighlightEmptyReference] private InputActionAsset inputAsset; // Asset with input mappings.
    [SerializeField] private string moveInput = "Player/Move"; // Input path for movement.
    [SerializeField] private string jumpInput = "Player/Jump"; // Input path for jump.
    [SerializeField] private string crouchInput = "Player/Crouch"; // Input path for crouch.

    [Header("References")]
    [SerializeField, HighlightEmptyReference] private Transform playerTransform; // Transform reference of the player.
    [SerializeField, HighlightEmptyReference] private Rigidbody targetRigidbody; // Rigidbody for physics operations.
    [SerializeField, HighlightEmptyReference] private Collider normalCollider; // Collider when standing.
    [SerializeField, HighlightEmptyReference] private Collider crouchCollider; // Collider when crouching.

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f; // Speed when moving normally.
    [SerializeField] private float crouchSpeed = 1f; // Speed when crouching.
    [SerializeField] private float rotationSpeed = 10f; // Rotation interpolation speed.
    [SerializeField] private float jumpForce = 5f; // Upward force applied when jumping.
    [SerializeField] private float coyoteTime = 0.15f;
    [Space(5)]
    [SerializeField, Tooltip("Global input rotation offset in degrees.")] private float worldRotation = 0; // Adjusts direction relative to world.

    [Header("State Settings")]
    [SerializeField] private PlayerControlMode capabilityMode = PlayerControlMode.Automatic; // Determines if the ability logic is automatic or external.
    [SerializeField] private PlayerAbility activeAbility = PlayerAbility.CanJump; // Current active player capability.

    [Header("Collision Layers")]
    [SerializeField] private LayerMask groundLayerMask = -1; // LayerMask used by the ground sensor.
    [SerializeField] private LayerMask obstacleLayerMask = -1; // LayerMask used by the obstacle sensor.
    [SerializeField] private LayerMask ceilingLayerMask = -1; // LayerMask used by the ceiling sensor.

    [Header("Ground Sensor Settings")]
    [SerializeField] private Vector3 groundDetectionSize = new(0.2f, 0.3f, 0.2f); // Box size for detecting ground.
    [SerializeField, TagDropdown] private List<string> ignoredTags = new() { "Ignore Collision" }; // Tags to ignore on ground sensor.

    [Header("Obstacle Sensor Settings")]
    [SerializeField] private Vector3 obstacleDetectionCenter = new(0f, 0.8f, 0.45f); // Default center for obstacle detection.
    [SerializeField] private Vector3 obstacleDetectionSize = new(0.25f, 1.5f, 0.2f); // Size of obstacle detection box.
    [Space(5)]
    [SerializeField] private Vector3 obstacleCrouchDetectionCenter = new(0f, 0.35f, 0.45f); // Center when crouching.
    [SerializeField] private Vector3 obstacleCrouchDetectionSize = new(0.25f, 0.4f, 0.2f); // Size when crouching.
    [SerializeField, TagDropdown] private List<string> obstacleIgnoreTags = new() { "Ignore Collision" }; // Tags to ignore for obstacle sensor.

    [Header("Ceiling Sensor Settings")]
    [SerializeField] private Vector3 hasSpaceDetectionCenter = new(0f, 0.8f, 0f); // Center of box to check space overhead.
    [SerializeField] private Vector3 hasSpaceDetectionSize = new(0.34f, 1.4f, 0.36f); // Size of ceiling clearance box.
    [SerializeField, TagDropdown] private List<string> hasSpaceIgnoredTags = new() { "Ignore Collision" }; // Tags to ignore for ceiling sensor.

    [Header("Physics Material Settings")]
    [SerializeField, HighlightEmptyReference] private PhysicsMaterial highFrictionMaterial;
    [SerializeField, HighlightEmptyReference] private PhysicsMaterial lowFrictionMaterial;
    [Space(5)]
    [SerializeField, Range(0f, 90f)] private float maxStableAngle = 45f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugGroundSensor = false; // Toggle ground sensor gizmo.
    [SerializeField] private bool debugObstacleSensor = false; // Toggle obstacle sensor gizmo.
    [SerializeField] private bool debugHasSpaceSensor = false; // Toggle ceiling sensor gizmo.
    [SerializeField] private bool debugAngleSensor = false;

    // Runtime sensor/controller references
    private BoxCollisionSensor groundSensor;
    private BoxCollisionSensor obstacleSensor;
    private BoxCollisionSensor hasSpaceSensor;
    private PlayerJumpController jumpController;
    private PlayerMoveController moveController;
    private PlayerRotationController rotationController;

    // Input event configs
    private OnInputSystemEventConfig<float> jumpEvent;
    private OnInputSystemEventConfig<Vector2> moveEvent;
    private OnInputSystemEventConfig<float> crouchEvent;

    // Internal state tracking
    private Collider[] collisionList;
    private PlayerAbility previousAbility; // Stores previous ability state when toggling crouch.
    private Vector3 playerPosition; // Cached position.
    private Quaternion playerRotation; // Cached rotation.
    private Vector2 moveDirection; // Current movement direction from input.
    private Vector3 obstacleCheckCenter; // Computed obstacle center.
    private Vector3 obstacleCheckSize; // Computed obstacle size.
    private Vector3 hasSpaceCheckCenter; // Computed ceiling check position.
    private float lastSlopeAngle = -1f;
    private float speed; // Current speed.
    private bool isGrounded; // Whether the player is on the ground.
    private bool isCrouching; // Whether the player is crouching.
    private bool isValidAngle;
    private bool isPlayable; // Flag that determines if the player is allowed to receive inputs and perform actions.

    /// <summary>Returns whether the player is currently grounded.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Returns whether the player is crouching.</summary>
    public bool IsCrouching => isCrouching;

    /// <summary>Returns true if the player is currently moving and not blocked by an obstacle.</summary>
    public bool IsMove => moveDirection.sqrMagnitude > 0.01f && !obstacleSensor.collisionDetected && isPlayable;

    /// <summary>Returns true if the player is currently allowed to move or perform actions.</summary>
    public bool IsPlayable => isPlayable;

    /// <summary>
    /// Defines if player capabilities are handled automatically or manually.
    /// </summary>
    public enum PlayerControlMode
    {
        Automatic,  // 0
        Manual      // 1
    }

    /// <summary>
    /// Defines available player abilities for this controller.
    /// </summary>
    public enum PlayerAbility
    {
        None,       // 0
        CanCrouch,  // 1
        CanJump     // 2
    }

    /// <summary>
    /// Validates serialized references.
    /// </summary>
    private void Awake()
    {
        // Check if all necessary serialized references are assigned.
        if (!inputAsset) Debug.LogWarning("Input Asset not assigned.", this);
        if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
        if (!targetRigidbody) Debug.LogWarning("Rigidbody not assigned.", this);
        if (!normalCollider) Debug.LogWarning("Normal Collider not assigned.", this);
        if (!crouchCollider) Debug.LogWarning("Crouch Collider not assigned.", this);
    }

    /// <summary>
    /// Initializes runtime values and sets up sensors, controllers, and input bindings.
    /// </summary>
    private void Start()
    {
        // Set initial ability state and movement speed.
        collisionList = new Collider[] { normalCollider, crouchCollider };
        previousAbility = activeAbility;
        isGrounded = true;
        isCrouching = false;
        speed = moveSpeed;
        isPlayable = true;

        SetupSensors(); // Initialize all sensors.
        SetupControllers(); // Create movement, jump and rotation controllers.
        SetupInputEvents(); // Bind input actions and link logic.
    }

    /// <summary>
    /// Creates and configures all sensors used by the player.
    /// </summary>
    private void SetupSensors()
    {
        // Sensor to detect ground contact.
        groundSensor = new BoxCollisionSensor(
            boxCenterProvider: () => playerPosition,
            boxSizeProvider: () => groundDetectionSize,
            boxRotationProvider: () => playerRotation,
            collisionLayerMaskProvider: () => groundLayerMask,
            triggerInteractionProvider: () => QueryTriggerInteraction.Ignore,
            filterModeProvider: () => DetectionFilter.All,
            referenceParentTransformProvider: () => playerTransform,
            ignoredTagsProvider: () => new HashSet<string>(ignoredTags),
            enableDetectionProvider: () => activeAbility == PlayerAbility.CanJump,
            enableDebugLogProvider: () => debugGroundSensor,
            gizmoTargetObjectProvider: () => gameObject,
            gizmoDrawingModeProvider: () => GizmoDisplayMode.SelectedOnly,
            gizmosColorProvider: () => Color.red
        );

        // Sensor to detect obstacles in front of the player.
        obstacleSensor = new BoxCollisionSensor(
            boxCenterProvider: () => obstacleCheckCenter,
            boxSizeProvider: () => obstacleCheckSize,
            boxRotationProvider: () => playerRotation,
            collisionLayerMaskProvider: () => obstacleLayerMask,
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

        // Sensor to detect if there's enough space above to stand.
        hasSpaceSensor = new BoxCollisionSensor(
            boxCenterProvider: () => hasSpaceCheckCenter,
            boxSizeProvider: () => hasSpaceDetectionSize,
            boxRotationProvider: () => playerRotation,
            collisionLayerMaskProvider: () => ceilingLayerMask,
            triggerInteractionProvider: () => QueryTriggerInteraction.Ignore,
            filterModeProvider: () => DetectionFilter.All,
            referenceParentTransformProvider: () => playerTransform,
            ignoredTagsProvider: () => new HashSet<string>(hasSpaceIgnoredTags),
            enableDetectionProvider: () => activeAbility == PlayerAbility.CanCrouch,
            enableDebugLogProvider: () => debugHasSpaceSensor,
            gizmoTargetObjectProvider: () => gameObject,
            gizmoDrawingModeProvider: () => GizmoDisplayMode.SelectedOnly,
            gizmosColorProvider: () => Color.blue
        );
    }

    /// <summary>
    /// Initializes controllers used for movement, jumping, and rotation.
    /// </summary>
    private void SetupControllers()
    {
        // Create jump logic handler.
        jumpController = new PlayerJumpController(() => targetRigidbody, () => isGrounded, () => 1, () => coyoteTime);

        // Create movement handler with obstacle detection.
        moveController = new PlayerMoveController(() => targetRigidbody, () => speed, () => obstacleSensor.collisionDetected);

        // Create smooth rotation handler.
        rotationController = new PlayerRotationController(() => playerTransform, () => moveDirection, () => rotationSpeed);
    }

    /// <summary>
    /// Binds input actions to events and hooks behavior.
    /// </summary>
    private void SetupInputEvents()
    {
        // Bind jump event if jump is allowed.
        jumpEvent = OnInputSystemEvent<float>.WithAction(inputAsset, jumpInput, () => isPlayable).OnPressed(_ =>
        {
            if (activeAbility == PlayerAbility.CanJump)
            {
                jumpController.OnJump(jumpForce);
            }
        });

        // Bind movement events.
        moveEvent = OnInputSystemEvent<Vector2>.WithAction(inputAsset, moveInput, () => isPlayable)
            .OnHold(value =>
            {
                // Convert movement input using world rotation.
                moveDirection = ConvertRotation(value, worldRotation);
                moveController.OnMove(moveDirection);
            })
            .OnReleased(() =>
            {
                // Stop movement when released.
                moveController.OnStop();
                moveDirection = Vector2.zero;
            });

        // Bind crouch toggle event.
        crouchEvent = OnInputSystemEvent<float>.WithAction(inputAsset, crouchInput, () => isPlayable).OnPressed(_ =>
        {
            if (capabilityMode == PlayerControlMode.Automatic)
            {
                // Toggle crouch only if there's space to stand up.
                if (!hasSpaceSensor.collisionDetected)
                {
                    isCrouching = !isCrouching;
                    activeAbility = isCrouching ? PlayerAbility.CanCrouch : previousAbility;
                }
            }
            else
            {
                // Manual mode resets crouch state.
                isCrouching = false;
                activeAbility = previousAbility;
            }
        });
    }

    /// <summary>
    /// Cleans up allocated sensors, controllers, and unbinds events.
    /// </summary>
    private void OnDestroy()
    {
        // Dispose all sensors and controllers.
        BoxCollisionSensor.Dispose(ref groundSensor);
        BoxCollisionSensor.Dispose(ref obstacleSensor);
        BoxCollisionSensor.Dispose(ref hasSpaceSensor);
        PlayerJumpController.Dispose(ref jumpController);
        PlayerMoveController.Dispose(ref moveController);
        PlayerRotationController.Dispose(ref rotationController);

        // Unbind all input events.
        jumpEvent?.UnbindAll();
        moveEvent?.UnbindAll();
        crouchEvent?.UnbindAll();
    }

    /// <summary>
    /// Main game loop update. Applies runtime state updates.
    /// </summary>
    private void Update()
    {
        UpdateTransformData(); // Refresh transform data for sensors.
        UpdateSensorPositions(); // Recalculate sensor positions based on crouch state.
        UpdateGroundStatus(); // Evaluate current grounded state.
        UpdatePlayerAbilityState(); // Evaluate player abilities and crouch state.
        UpdateSpeedAndCollider(); // Update speed and enable/disable correct collider.
    }

    /// <summary>
    /// Updates cached transform data from the player.
    /// </summary>
    private void UpdateTransformData()
    {
        playerPosition = playerTransform.position;
        playerRotation = playerTransform.rotation;
    }

    /// <summary>
    /// Recomputes sensor positions based on crouch state.
    /// </summary>
    private void UpdateSensorPositions()
    {
        obstacleCheckCenter = playerTransform.TransformPoint(isCrouching ? obstacleCrouchDetectionCenter : obstacleDetectionCenter);
        obstacleCheckSize = isCrouching ? obstacleCrouchDetectionSize : obstacleDetectionSize;
        hasSpaceCheckCenter = playerTransform.TransformPoint(hasSpaceDetectionCenter);
    }

    /// <summary>
    /// Evaluates whether the player is grounded.
    /// </summary>
    private void UpdateGroundStatus() => isGrounded = (activeAbility != PlayerAbility.CanJump || groundSensor.collisionDetected) && isValidAngle;

    /// <summary>
    /// Adjusts player ability and crouch logic depending on control mode.
    /// </summary>
    private void UpdatePlayerAbilityState()
    {
        if (capabilityMode == PlayerControlMode.Manual && isCrouching)
        {
            isCrouching = false;
            activeAbility = previousAbility;
        }

        if (activeAbility == PlayerAbility.None)
        {
            isGrounded = true;
            isCrouching = false;
        }

        if (activeAbility != PlayerAbility.CanCrouch)
        {
            previousAbility = activeAbility;
        }
    }

    /// <summary>
    /// Updates player movement speed and toggles colliders for crouch state.
    /// </summary>
    private void UpdateSpeedAndCollider()
    {
        speed = isCrouching ? crouchSpeed : moveSpeed;
        normalCollider.enabled = !isCrouching;
        crouchCollider.enabled = isCrouching;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!isCrouching)
        {
            isValidAngle = CheckValidAngle(collision, collisionList, highFrictionMaterial, lowFrictionMaterial, ref lastSlopeAngle, maxStableAngle, debugAngleSensor);
        }
    }

    /// <summary>
    /// Sets the capability control mode (manual or automatic).
    /// </summary>
    /// <param name="mode">Integer representing PlayerControlMode enum value.</param>
    public void SetCapabilityMode(int mode)
    {
        if (Enum.IsDefined(typeof(PlayerControlMode), mode))
        {
            capabilityMode = (PlayerControlMode)mode;

            if (capabilityMode == PlayerControlMode.Manual && isCrouching)
            {
                isCrouching = false;
                activeAbility = previousAbility;
            }
        }
        else
        {
            Debug.LogWarning($"Invalid PlayerState value: {mode}", this);
        }
    }

    /// <summary>
    /// Sets the active player ability externally (e.g., jump or crouch).
    /// </summary>
    /// <param name="ability">Integer representing PlayerAbility enum value.</param>
    public void SetPlayerAbility(int ability)
    {
        if (Enum.IsDefined(typeof(PlayerAbility), ability))
        {
            if ((PlayerAbility)ability == PlayerAbility.CanCrouch && capabilityMode == PlayerControlMode.Manual)
            {
                Debug.LogWarning("Cannot enter crouch state in Manual mode.", this);
                return;
            }

            activeAbility = (PlayerAbility)ability;
            isCrouching = activeAbility == PlayerAbility.CanCrouch;

            if (activeAbility != PlayerAbility.CanCrouch)
            {
                previousAbility = activeAbility;
            }
        }
        else
        {
            Debug.LogWarning($"Invalid PlayerCapability value: {ability}", this);
        }
    }

    /// <summary>
    /// Enables or disables the player's ability to act (movement, jump, crouch).
    /// </summary>
    /// <param name="toggle">True to enable input and logic; false to disable.</param>
    public void TogglePlayerPlayable(bool toggle) => isPlayable = toggle;

    /// <summary>
    /// Serializes current player data and returns a JSON string.
    /// </summary>
    /// <returns>Serialized JSON with current player state.</returns>
    public string SavePlayerData()
    {
        return CustomPlayerData.SaveData(() => new Dictionary<string, object>
        {
            { nameof(capabilityMode), capabilityMode }, // Save control mode.
            { nameof(activeAbility), activeAbility }, // Save current ability.
            { nameof(previousAbility), previousAbility }, // Save previous ability.
            { nameof(playerPosition), playerTransform.position }, // Save position.
            { nameof(playerRotation), playerTransform.rotation }, // Save rotation.
            { nameof(isGrounded), isGrounded }, // Save ground state.
            { nameof(isCrouching), isCrouching } // Save crouch state.
        });
    }

    /// <summary>
    /// Restores player data from a JSON string.
    /// </summary>
    /// <param name="json">Serialized JSON with saved player state.</param>
    public void LoadPlayerData(string json)
    {
        CustomPlayerData.LoadData(json, dict =>
        {
            capabilityMode = (PlayerControlMode)dict[nameof(capabilityMode)]; // Load control mode.
            activeAbility = (PlayerAbility)dict[nameof(activeAbility)]; // Load current ability.
            previousAbility = (PlayerAbility)dict[nameof(previousAbility)]; // Load previous ability.

            // Restore position and rotation
            playerTransform.SetPositionAndRotation((Vector3)dict[nameof(playerPosition)], (Quaternion)dict[nameof(playerRotation)]);

            isGrounded = Convert.ToBoolean(dict[nameof(isGrounded)]); // Restore ground state.
            isCrouching = Convert.ToBoolean(dict[nameof(isCrouching)]); // Restore crouch state.
        });
    }
}