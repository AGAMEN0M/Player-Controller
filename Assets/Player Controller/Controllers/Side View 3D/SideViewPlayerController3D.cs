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
/// Handles movement, jumping, crouching, and environmental detection using dynamic sensors.
/// </summary>
[AddComponentMenu("Player Controller/3D/Player Controller (Side View)")]
public class SideViewPlayerController3D : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField, HighlightEmptyReference] private InputActionAsset inputAsset; // Input asset containing action maps.
    [SerializeField] private string moveInput = "Player/Move"; // Input path for directional movement.
    [SerializeField] private string jumpInput = "Player/Jump"; // Input path for jump.
    [SerializeField] private string crouchInput = "Player/Crouch"; // Input path for crouch toggle.

    [Header("References")]
    [SerializeField, HighlightEmptyReference] private Transform playerTransform; // Reference to the player's transform.
    [SerializeField, HighlightEmptyReference] private Rigidbody targetRigidbody; // Rigidbody component for physics movement.
    [SerializeField, HighlightEmptyReference] private Collider normalCollider; // Collider used when standing.
    [SerializeField, HighlightEmptyReference] private Collider crouchCollider; // Collider used when crouching.

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f; // Movement speed when standing.
    [SerializeField] private float crouchSpeed = 1f; // Movement speed when crouching.
    [SerializeField] private float rotationSpeed = 10f; // Speed of rotation smoothing.
    [SerializeField] private float jumpForce = 5f; // Force applied when jumping.
    [SerializeField] private float coyoteTime = 0.15f; // Grace period to jump after leaving ground.
    [Space(5)]
    [SerializeField, Tooltip("Global input rotation offset in degrees.")]
    private float worldRotation = 0; // Rotates input direction globally.

    [Header("State Settings")]
    [SerializeField] private PlayerControlMode capabilityMode = PlayerControlMode.Automatic; // Controls automatic or manual state handling.
    [SerializeField] private PlayerAbility activeAbility = PlayerAbility.CanJump; // Currently active player ability.

    [Header("Collision Layers")]
    [SerializeField] private LayerMask groundLayerMask = -1; // Layer mask used to detect ground.
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Layer mask for obstacle detection.
    [SerializeField] private LayerMask ceilingLayerMask = -1; // Layer mask for checking overhead space.

    [Header("Ground Sensor Settings")]
    [SerializeField] private Vector3 groundDetectionSize = new(0.2f, 0.3f, 0.2f); // Box size used for ground detection.
    [SerializeField, TagDropdown] private List<string> ignoredTags = new() { "Ignore Collision" }; // Tags to ignore when detecting ground.

    [Header("Obstacle Sensor Settings")]
    [SerializeField] private Vector3 obstacleDetectionCenter = new(0f, 0.8f, 0.45f); // Center position for detecting obstacles.
    [SerializeField] private Vector3 obstacleDetectionSize = new(0.25f, 1.5f, 0.2f); // Box size for detecting obstacles.
    [Space(5)]
    [SerializeField] private Vector3 obstacleCrouchDetectionCenter = new(0f, 0.35f, 0.45f); // Center position for crouch obstacle detection.
    [SerializeField] private Vector3 obstacleCrouchDetectionSize = new(0.25f, 0.4f, 0.2f); // Box size for crouch obstacle detection.
    [SerializeField, TagDropdown] private List<string> obstacleIgnoreTags = new() { "Ignore Collision" }; // Tags to ignore when detecting obstacles.

    [Header("Ceiling Sensor Settings")]
    [SerializeField] private Vector3 hasSpaceDetectionCenter = new(0f, 0.8f, 0f); // Center for detecting space to stand.
    [SerializeField] private Vector3 hasSpaceDetectionSize = new(0.34f, 1.4f, 0.36f); // Box size to check for overhead clearance.
    [SerializeField, TagDropdown] private List<string> hasSpaceIgnoredTags = new() { "Ignore Collision" }; // Tags to ignore when detecting ceiling space.

    [Header("Physics Material Settings")]
    [SerializeField, HighlightEmptyReference] private PhysicsMaterial highFrictionMaterial; // Material for grounded friction.
    [SerializeField, HighlightEmptyReference] private PhysicsMaterial lowFrictionMaterial; // Material for airborne or low-friction surfaces.
    [Space(5)]
    [SerializeField, Range(0f, 90f)] private float maxStableAngle = 45f; // Maximum slope angle the player can stand on.

    [Header("Debug Settings")]
    [SerializeField] private bool debugGroundSensor = false; // Draw gizmos for ground sensor.
    [SerializeField] private bool debugObstacleSensor = false; // Draw gizmos for obstacle sensor.
    [SerializeField] private bool debugHasSpaceSensor = false; // Draw gizmos for ceiling sensor.
    [SerializeField] private bool debugAngleSensor = false; // Draw debug for angle detection.

    // Sensor and controller instances
    private BoxCollisionSensor groundSensor;
    private BoxCollisionSensor obstacleSensor;
    private BoxCollisionSensor hasSpaceSensor;
    private CheckValidAngleSensors validAngleSensors;
    private PlayerJumpController jumpController;
    private PlayerMoveController moveController;
    private PlayerRotationController rotationController;

    // Input event bindings
    private OnInputSystemEventConfig<float> jumpEvent;
    private OnInputSystemEventConfig<Vector2> moveEvent;
    private OnInputSystemEventConfig<float> crouchEvent;

    // Runtime states
    private Collider[] collisionList; // List of colliders used for angle checking.
    private PlayerAbility previousAbility; // Stores previous ability state when toggling crouch.
    private Vector3 playerPosition; // Cached world position of the player.
    private Quaternion playerRotation; // Cached world rotation of the player.
    private Vector2 moveDirection; // Current movement direction from input.
    private Vector3 obstacleCheckCenter; // Current center position used for obstacle detection box.
    private Vector3 obstacleCheckSize; // Current size of the box used for obstacle detection.
    private Vector3 hasSpaceCheckCenter; // Current center used to check for space above the player.
    private float speed; // Current movement speed depending on state.
    private bool isGrounded; // Whether the player is on the ground.
    private bool isCrouching; // Whether the player is crouching.
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
        Automatic,  // Capabilities handled automatically. (0)
        Manual      // Capabilities must be set manually.  (1)
    }

    /// <summary>
    /// Defines available player abilities for this controller.
    /// </summary>
    public enum PlayerAbility
    {
        None,       // No ability is currently active. (0)
        CanCrouch,  // Enables crouching ability.      (1)
        CanJump     // Enables jumping ability.        (2)
    }

    /// <summary>
    /// Validates serialized references at startup to ensure required components are assigned.
    /// </summary>
    private void Awake()
    {
        // Check if all necessary serialized references are assigned.
        if (!inputAsset) Debug.LogWarning("Input Asset not assigned.", this);
        if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
        if (!targetRigidbody) Debug.LogWarning("Rigidbody not assigned.", this);
        if (!normalCollider) Debug.LogWarning("Normal Collider not assigned.", this);
        if (!crouchCollider) Debug.LogWarning("Crouch Collider not assigned.", this);
        if (!highFrictionMaterial || !lowFrictionMaterial)
        {
            Debug.LogWarning("One or both Physics Materials (High/Low Friction) are not assigned.", this);
        }
    }

    /// <summary>
    /// Initializes runtime values and sets up sensors, controllers, and input bindings at the start of the game.
    /// </summary>
    private void Start()
    {
        // Initialize collider list with standing and crouching colliders.
        collisionList = new Collider[] { normalCollider, crouchCollider };

        // Set the initial player ability and states.
        previousAbility = activeAbility;
        isGrounded = true;    // Assume player starts grounded.
        isCrouching = false;  // Start in standing state.
        speed = moveSpeed;    // Initialize movement speed to standing speed.
        isPlayable = true;    // Enable player input and actions.

        SetupSensors();      // Create and configure all collision sensors.
        SetupControllers();  // Instantiate movement, jump, and rotation controllers.
        SetupInputEvents();  // Bind input actions to corresponding event handlers.
    }

    /// <summary>
    /// Creates and configures all collision sensors used by the player for ground, obstacle, ceiling, and angle detection.
    /// </summary>
    private void SetupSensors()
    {
        // Ground sensor detects if the player is standing on a valid surface.
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

        // Obstacle sensor detects obstacles in front of the player to block movement.
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

        // Ceiling sensor checks if there is enough overhead space for the player to stand up.
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

        // Angle sensors verify if the player is standing on a surface with an acceptable slope angle.
        validAngleSensors = new CheckValidAngleSensors(
            targetTransform: () => playerTransform,
            highFrictionMaterial: () => highFrictionMaterial,
            lowFrictionMaterial: () => lowFrictionMaterial,
            colliders: () => collisionList,
            maxStableAngle: () => maxStableAngle,
            raycastDistance: () => 0.5f,
            debug: () => debugAngleSensor
        );
    }

    /// <summary>
    /// Initializes controllers responsible for player movement, jumping, and rotation.
    /// </summary>
    private void SetupControllers()
    {
        // Initialize jump controller with access to Rigidbody, grounded state, jump count, and coyote time.
        jumpController = new PlayerJumpController(() => targetRigidbody, () => isGrounded, () => 1, () => coyoteTime);

        // Initialize movement controller that handles player translation and checks for obstacle collisions.
        moveController = new PlayerMoveController(() => targetRigidbody, () => speed, () => obstacleSensor.collisionDetected);

        // Initialize rotation controller to smoothly rotate the player toward movement direction.
        rotationController = new PlayerRotationController(() => playerTransform, () => moveDirection, () => rotationSpeed);
    }

    /// <summary>
    /// Binds player input actions to corresponding event handlers and links input logic.
    /// </summary>
    private void SetupInputEvents()
    {
        // Bind jump input action, triggering jump if the player has jumping ability and is playable.
        jumpEvent = OnInputSystemEvent<float>.WithAction(inputAsset, jumpInput, () => isPlayable).OnPressed(_ =>
        {
            if (activeAbility == PlayerAbility.CanJump)
            {
                jumpController.OnJump(jumpForce);
            }
        });

        // Bind movement input action to handle movement while the input is held or released.
        moveEvent = OnInputSystemEvent<Vector2>.WithAction(inputAsset, moveInput, () => isPlayable)
            .OnHold(value =>
            {
                // Apply world rotation offset to input direction and update movement controller.
                moveDirection = ConvertRotation(value, worldRotation);
                moveController.OnMove(moveDirection);
            })
            .OnReleased(() =>
            {
                // Stop movement and reset input direction when released.
                moveController.OnStop();
                moveDirection = Vector2.zero;
            });

        // Bind crouch toggle input action with logic dependent on capability control mode.
        crouchEvent = OnInputSystemEvent<float>.WithAction(inputAsset, crouchInput, () => isPlayable).OnPressed(_ =>
        {
            if (capabilityMode == PlayerControlMode.Automatic)
            {
                // In automatic mode, toggle crouch only if there is no obstruction overhead.
                if (!hasSpaceSensor.collisionDetected)
                {
                    isCrouching = !isCrouching;
                    activeAbility = isCrouching ? PlayerAbility.CanCrouch : previousAbility;
                }
            }
            else
            {
                // In manual mode, crouch state is reset to standing.
                isCrouching = false;
                activeAbility = previousAbility;
            }
        });
    }

    /// <summary>
    /// Cleans up all allocated sensors, controllers, and unbinds input events when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Dispose of all collision sensors to free resources.
        BoxCollisionSensor.Dispose(ref groundSensor);
        BoxCollisionSensor.Dispose(ref obstacleSensor);
        BoxCollisionSensor.Dispose(ref hasSpaceSensor);

        // Dispose of angle validation sensors.
        CheckValidAngleSensors.Dispose(ref validAngleSensors);

        // Dispose of player control components.
        PlayerJumpController.Dispose(ref jumpController);
        PlayerMoveController.Dispose(ref moveController);
        PlayerRotationController.Dispose(ref rotationController);

        // Unbind and clean up all input event subscriptions.
        jumpEvent?.UnbindAll();
        moveEvent?.UnbindAll();
        crouchEvent?.UnbindAll();
    }

    /// <summary>
    /// Main update loop called once per frame; updates runtime state and sensor data.
    /// </summary>
    private void Update()
    {
        UpdateTransformData();      // Refresh cached player transform position and rotation.
        UpdateSensorPositions();    // Recalculate sensor positions based on current crouch state.
        UpdateGroundStatus();       // Determine if the player is grounded according to sensors and state.
        UpdatePlayerAbilityState(); // Update player abilities and crouch state logic.
        UpdateSpeedAndCollider();   // Adjust movement speed and toggle colliders based on crouch state.
    }
    /// <summary>
    /// Updates cached player position and rotation for use by sensors and controllers.
    /// </summary>
    private void UpdateTransformData()
    {
        playerPosition = playerTransform.position;
        playerRotation = playerTransform.rotation;
    }

    /// <summary>
    /// Updates sensor positions and sizes based on whether the player is crouching or standing.
    /// </summary>
    private void UpdateSensorPositions()
    {
        // Set obstacle sensor center and size depending on crouch state.
        obstacleCheckCenter = playerTransform.TransformPoint(isCrouching ? obstacleCrouchDetectionCenter : obstacleDetectionCenter);
        obstacleCheckSize = isCrouching ? obstacleCrouchDetectionSize : obstacleDetectionSize;

        // Set ceiling space sensor center.
        hasSpaceCheckCenter = playerTransform.TransformPoint(hasSpaceDetectionCenter);
    }

    /// <summary>
    /// Updates grounded state based on sensor detection, crouch status, and surface angle validity.
    /// </summary>
    private void UpdateGroundStatus()
    {
        // Player is grounded if not in jump ability mode or ground sensor detects ground, and player is not crouching and is on a valid slope angle.
        isGrounded = (activeAbility != PlayerAbility.CanJump || groundSensor.collisionDetected) && (!isCrouching && validAngleSensors.isValidAngle);
    }
    /// <summary>
    /// Updates player ability and crouch logic based on the current control mode.
    /// Ensures consistency between crouch state and ability flags.
    /// </summary>
    private void UpdatePlayerAbilityState()
    {
        // In manual mode, reset crouch state and restore the previous ability.
        if (capabilityMode == PlayerControlMode.Manual && isCrouching)
        {
            isCrouching = false;
            activeAbility = previousAbility;
        }

        // If no ability is active, ensure player is grounded and not crouching.
        if (activeAbility == PlayerAbility.None)
        {
            isGrounded = true;
            isCrouching = false;
        }

        // Update previous ability unless crouch is currently active.
        if (activeAbility != PlayerAbility.CanCrouch)
        {
            previousAbility = activeAbility;
        }
    }

    /// <summary>
    /// Updates the player's movement speed and toggles the correct collider based on crouch state.
    /// </summary>
    private void UpdateSpeedAndCollider()
    {
        // Switch movement speed based on whether the player is crouching.
        speed = isCrouching ? crouchSpeed : moveSpeed;

        // Enable the appropriate collider for the current stance.
        normalCollider.enabled = !isCrouching;
        crouchCollider.enabled = isCrouching;
    }

    /// <summary>
    /// Sets the player's capability control mode to manual or automatic.
    /// Resets crouch state if switched to manual while crouching.
    /// </summary>
    /// <param name="mode">Integer value representing the <see cref="PlayerControlMode"/> enum.</param>
    public void SetCapabilityMode(int mode)
    {
        if (Enum.IsDefined(typeof(PlayerControlMode), mode))
        {
            capabilityMode = (PlayerControlMode)mode;

            // If switching to manual mode while crouching, reset crouch and restore previous ability.
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
    /// Sets the player's current ability externally (e.g., enable jumping or crouching).
    /// Updates crouch state and tracks the previous ability when necessary.
    /// </summary>
    /// <param name="ability">Integer value representing the <see cref="PlayerAbility"/> enum.</param>
    public void SetPlayerAbility(int ability)
    {
        if (Enum.IsDefined(typeof(PlayerAbility), ability))
        {
            PlayerAbility selectedAbility = (PlayerAbility)ability;

            // Prevent crouching if in manual mode, as crouch toggling is not supported.
            if (selectedAbility == PlayerAbility.CanCrouch && capabilityMode == PlayerControlMode.Manual)
            {
                Debug.LogWarning("Cannot enter crouch state in Manual mode.", this);
                return;
            }

            // Apply the selected ability and update crouch state.
            activeAbility = selectedAbility;
            isCrouching = activeAbility == PlayerAbility.CanCrouch;

            // Track the last non-crouch ability.
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
    /// Enables or disables the player's ability to perform actions such as moving, jumping, or crouching.
    /// </summary>
    /// <param name="toggle">If true, player input and action logic are enabled; if false, they are disabled.</param>
    public void TogglePlayerPlayable(bool toggle) => isPlayable = toggle;

    /// <summary>
    /// Serializes the current player state into a JSON string for saving.
    /// Includes position, rotation, ability states, and control mode.
    /// </summary>
    /// <returns>A JSON string representing the current player state.</returns>
    public string SavePlayerData()
    {
        return CustomPlayerData.SaveData(() => new Dictionary<string, object>
    {
        { nameof(capabilityMode), capabilityMode },             // Save control mode.
        { nameof(activeAbility), activeAbility },               // Save current active ability.
        { nameof(previousAbility), previousAbility },           // Save previous ability for toggling logic.
        { nameof(playerPosition), playerTransform.position },   // Save player position.
        { nameof(playerRotation), playerTransform.rotation },   // Save player rotation.
        { nameof(isGrounded), isGrounded },                     // Save grounded state.
        { nameof(isCrouching), isCrouching }                    // Save crouch state.
    });
    }

    /// <summary>
    /// Restores player state from a previously saved JSON string.
    /// Applies position, rotation, and ability state to the player.
    /// </summary>
    /// <param name="json">A JSON string containing saved player data.</param>
    public void LoadPlayerData(string json)
    {
        CustomPlayerData.LoadData(json, dict =>
        {
            capabilityMode = (PlayerControlMode)dict[nameof(capabilityMode)]; // Load control mode.
            activeAbility = (PlayerAbility)dict[nameof(activeAbility)];       // Load current ability.
            previousAbility = (PlayerAbility)dict[nameof(previousAbility)];   // Load previous ability.

            // Restore transform state.
            playerTransform.SetPositionAndRotation((Vector3)dict[nameof(playerPosition)], (Quaternion)dict[nameof(playerRotation)]);

            isGrounded = Convert.ToBoolean(dict[nameof(isGrounded)]);   // Restore grounded state.
            isCrouching = Convert.ToBoolean(dict[nameof(isCrouching)]); // Restore crouch state.
        });
    }
}