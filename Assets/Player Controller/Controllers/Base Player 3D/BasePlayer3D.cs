/*
 * ---------------------------------------------------------------------------
 * Description: Modular 3D player controller system for Unity that handles 
 *              input, movement, rotation, crouching, jumping, running, stamina management, 
 *              and collision detection using sensors and a structured runtime architecture.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using PlayerController.TransformRuntime;
using PlayerController.PhysicsRuntime;
using PlayerController.InputEvents;
using PlayerController.CustomData;
using PlayerController.Abilities;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

using static PlayerController.PhysicsRuntime.BoxCollisionSensor;
using static PlayerController.Utils.PlayerUtils;

/// <summary>
/// Core 3D player controller for Unity, handling input, movement, jumping, crouching, running, 
/// collision detection, stamina, and rotation logic. Designed for modular extension using sensors, 
/// input events, and gameplay states.
/// </summary>
[AddComponentMenu("Player Controller/3D/Player Controller (Base)")]
public class BasePlayer3D : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Input Settings")]
    [SerializeField, ValidateReference] private InputActionAsset inputActions; // Reference to the Input Action Asset for player controls.
    [SerializeField] private string moveActionPath = "Player/Move"; // Input action path for movement.
    [SerializeField] private string runActionPath = "Player/Run"; // Input action path for running.
    [SerializeField] private string jumpActionPath = "Player/Jump"; // Input action path for jumping.
    [SerializeField] private string crouchActionPath = "Player/Crouch"; // Input action path for crouching.

    [Header("References")]
    [SerializeField, ValidateReference] private Transform playerTransform; // Transform of the player object.
    [SerializeField, ValidateReference] private Rigidbody playerRigidbody; // Rigidbody used for physics movement.
    [SerializeField, ValidateReference] private Collider standingCollider; // Collider used when player is standing.
    [SerializeField, ValidateReference] private Collider crouchingCollider; // Collider used when player is crouching.

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1f; // Walking speed.
    [SerializeField] private float runSpeed = 5f; // Running speed.
    [Space(5)]
    [SerializeField] private float crouchWalkSpeed = 1f; // Walking speed while crouching.
    [SerializeField] private float crouchRunSpeed = 1.5f; // Running speed while crouching.
    [Space(5)]
    [SerializeField] private float rotationSmoothSpeed = 10f; // Smooth factor for player rotation.
    [Space(5)]
    [SerializeField] private float jumpForce = 5f; // Force applied on jump.
    [SerializeField] private int maxJumpCount = 2; // Maximum number of jumps allowed (for double jump, etc.).
    [SerializeField] private float coyoteTimeDuration = 0.15f; // Time window to still allow jump after leaving ground.
    [Space(5)]
    [SerializeField, Tooltip("Global input rotation offset in degrees.")]
    private float globalInputRotationOffset = 0f; // Global angle offset applied to input direction.

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 50f; // Maximum stamina value.
    [SerializeField] private float minStaminaForRun = 12.5f; // Minimum stamina required to start running.
    [Space(5)]
    [SerializeField] private float staminaDepletionRate = 15f; // Stamina drain rate when running.
    [SerializeField] private float staminaRecoveryRate = 3f; // Stamina recovery rate when not running.

    [Header("Player State Settings")]
    [SerializeField] private PlayerControlMode controlMode = PlayerControlMode.Automatic; // Player control mode.
    [SerializeField] private PlayerAbility currentAbility = PlayerAbility.CanJump; // Current player ability.
    [SerializeField] private PlayerMovement movementMode = PlayerMovement.canRun; // Movement mode (walk or run enabled).

    [Header("Collision Layers")]
    [SerializeField] private LayerMask groundLayers = -1; // Layers considered as ground.
    [SerializeField] private LayerMask obstacleLayers = -1; // Layers considered as obstacles.
    [SerializeField] private LayerMask ceilingLayers = -1; // Layers considered as ceiling for crouch checks.

    [Header("Ground Sensor Settings")]
    [SerializeField] private Vector3 groundSensorBoxSize = new(0.2f, 0.3f, 0.2f); // Size of ground detection box.
    [SerializeField, TagsDropdown] private List<string> groundIgnoredTags = new() { "Ignore Collision" }; // Tags ignored by ground sensor.

    [Header("Obstacle Sensor Settings")]
    [SerializeField] private Vector3 obstacleSensorCenter = new(0f, 0.8f, 0.45f); // Center offset for obstacle sensor.
    [SerializeField] private Vector3 obstacleSensorSize = new(0.25f, 1.5f, 0.2f); // Size of obstacle detection box.
    [Space(5)]
    [SerializeField] private Vector3 crouchObstacleCenter = new(0f, 0.35f, 0.45f); // Obstacle sensor center when crouching.
    [SerializeField] private Vector3 crouchObstacleSize = new(0.25f, 0.4f, 0.2f); // Obstacle sensor size when crouching.
    [SerializeField, TagsDropdown] private List<string> obstacleIgnoredTags = new() { "Ignore Collision" }; // Tags ignored by obstacle sensor.

    [Header("Ceiling Sensor Settings")]
    [SerializeField] private Vector3 ceilingSensorCenter = new(0f, 0.8f, 0f); // Center offset for ceiling sensor.
    [SerializeField] private Vector3 ceilingSensorSize = new(0.34f, 1.4f, 0.36f); // Size of ceiling detection box.
    [SerializeField, TagsDropdown] private List<string> ceilingIgnoredTags = new() { "Ignore Collision" }; // Tags ignored by ceiling sensor.

    [Header("Physics Material Settings")]
    [SerializeField, ValidateReference] private PhysicsMaterial highFrictionMaterial; // Physics material for high friction surfaces.
    [SerializeField, ValidateReference] private PhysicsMaterial lowFrictionMaterial; // Physics material for low friction surfaces.
    [Space(5)]
    [SerializeField, Range(0f, 90f)] private float maxSlopeAngle = 45f; // Maximum walkable slope angle in degrees.

    [Header("Debug Settings")]
    [SerializeField] private bool debugGroundSensor = false; // Enable debug visualization for ground sensor.
    [SerializeField] private bool debugObstacleSensor = false; // Enable debug visualization for obstacle sensor.
    [SerializeField] private bool debugCeilingSensor = false; // Enable debug visualization for ceiling sensor.
    [SerializeField] private bool debugSlopeAngle = false; // Enable debug visualization for slope angle.

    #endregion

    #region === Runtime Fields ===

    // Collision sensors for ground, obstacles, and ceiling.
    private BoxCollisionSensor groundSensor; // Sensor for detecting ground contact.
    private BoxCollisionSensor obstacleSensor; // Sensor for detecting front-facing obstacles.
    private BoxCollisionSensor ceilingSensor; // Sensor for detecting overhead ceilings (used to prevent standing up in crouch).

    // Sensor to check if slope angle is valid for walking.
    private CheckValidAngleSensors slopeAngleSensors; // Sensor to check if the current slope is walkable.

    // Controllers managing different player mechanics.
    private PlayerJumpController jumpController; // Handles jumping logic and force application.
    private PlayerMoveController moveController; // Handles horizontal movement and obstacle checks.
    private PlayerRotationController rotationController; // Handles player rotation based on input direction.
    private PlayerSpeedControl staminaController; // Manages running speed and stamina depletion/recovery.

    // Input event configurations for jump, move, crouch, and run.
    private OnInputSystemEventConfig<float> jumpInputEvent; // Input event for jump action.
    private OnInputSystemEventConfig<Vector2> moveInputEvent; // Input event for movement.
    private OnInputSystemEventConfig<float> crouchInputEvent; // Input event for crouching.
    private OnInputSystemEventConfig<float> runInputEvent; // Input event for running.

    private Collider[] collidersForAngleCheck; // Cached colliders used for slope validation.
    private PlayerAbility lastNonCrouchAbility; // Last ability before switching to crouch mode.

    // Cached player position and rotation for sensors.
    private Vector3 cachedPlayerPosition; // Current player position used by sensors.
    private Quaternion cachedPlayerRotation; // Current player rotation used by sensors.

    // Current input and sensor state.
    private Vector2 currentMoveInput; // Cached input vector for movement.
    private Vector3 currentObstacleCenter; // Center position for obstacle detection.
    private Vector3 currentObstacleSize; // Size of obstacle box used in detection.
    private Vector3 currentCeilingCenter; // Center position for ceiling detection.

    // Movement and state flags.
    private float currentSpeed; // Final computed movement speed based on state.
    private bool isGrounded; // Whether the player is touching the ground.
    private bool isCrouching; // Whether the player is in crouch mode.
    private bool isRunning; // Whether the player is currently running.
    private bool isPlayable; // Whether the player can receive and respond to input.
    private bool canRun; // Whether running is allowed based on state and mode.

    #endregion

    #region === Public Properties ===

    /// <summary>Returns whether the player is currently grounded.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Returns whether the player is currently crouching.</summary>
    public bool IsCrouching => isCrouching;

    /// <summary>Returns true if the player is moving (non-zero input) and no obstacle is blocking movement.</summary>
    public bool IsMoving => currentMoveInput.sqrMagnitude > 0.01f && !obstacleSensor.collisionDetected && isPlayable;

    /// <summary>Returns true if the player is running, has stamina, and is allowed to run.</summary>
    public bool IsRunning => isRunning && staminaController.hasStamina && canRun;

    /// <summary>Current stamina value normalized as a percentage [0..100].</summary>
    public float StaminaPercent => staminaController.staminaPercentage;

    /// <summary>Returns whether the player is currently playable (can receive input).</summary>
    public bool IsPlayable => isPlayable;

    #endregion

    #region === Enums ===

    /// <summary>
    /// Defines how the player control behaves.
    /// </summary>
    public enum PlayerControlMode
    {
        Automatic, // Automatic control mode.
        Manual     // Manual control mode.
    }

    /// <summary>
    /// Defines available abilities the player can have.
    /// </summary>
    public enum PlayerAbility
    {
        None,      // No abilities.
        CanCrouch, // Ability to crouch.
        CanJump    // Ability to jump.
    }

    /// <summary>
    /// Defines player movement modes.
    /// </summary>
    public enum PlayerMovement
    {
        justWalk, // Player can only walk.
        canRun    // Player can run.
    }

    #endregion

    #region === Unity Methods ===

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Validates required serialized references.
    /// </summary>
    private void Awake() => ValidateSerializedReferences();

    /// <summary>
    /// Called before the first frame update.
    /// Initializes player components and state.
    /// </summary>
    private void Start() => InitializeComponents();

    /// <summary>
    /// Called once per frame.
    /// Updates player state and sensors.
    /// </summary>
    private void Update()
    {
        CacheTransformData();
        UpdateSensorPositions();
        UpdateGroundedState();
        UpdateAbilityAndCrouchLogic();
        UpdateSpeedAndColliders();
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Disposes all allocated resources.
    /// </summary>
    private void OnDestroy() => DisposeAll();

    #endregion

    #region === Initialization ===

    /// <summary>
    /// Validates if all required serialized references are assigned.
    /// Logs warnings if any references are missing.
    /// </summary>
    private void ValidateSerializedReferences()
    {
        if (!inputActions) Debug.LogWarning("InputAsset not assigned.", this);
        if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
        if (!playerRigidbody) Debug.LogWarning("Rigidbody not assigned.", this);
        if (!standingCollider) Debug.LogWarning("Collider standing unassigned.", this);
        if (!crouchingCollider) Debug.LogWarning("Unassigned crouch collider.", this);
        if (!highFrictionMaterial || !lowFrictionMaterial)
        {
            Debug.LogWarning("Physics Materials not assigned.", this);
        }
    }

    /// <summary>
    /// Initializes runtime fields, sensors, controllers and input bindings.
    /// </summary>
    private void InitializeComponents()
    {
        collidersForAngleCheck = new[] { standingCollider, crouchingCollider };
        lastNonCrouchAbility = currentAbility;
        isGrounded = true;
        isCrouching = false;
        currentSpeed = walkSpeed;
        isPlayable = true;
        canRun = isPlayable && movementMode == PlayerMovement.canRun;

        SetupSensors();       // Instantiates and configures collision sensors.
        SetupControllers();   // Instantiates movement, rotation, jump and stamina controllers.
        SetupInputBindings(); // Configures input events and maps them to actions.
    }

    #endregion

    #region === Setup Methods ===

    /// <summary>
    /// Creates and configures collision sensors for ground, obstacles and ceiling.
    /// Also sets up slope angle sensor.
    /// </summary>
    private void SetupSensors()
    {
        // Sensor to detect the ground (collision with the ground).
        groundSensor = new BoxCollisionSensor(
            () => cachedPlayerPosition,
            () => groundSensorBoxSize,
            () => cachedPlayerRotation,
            () => groundLayers,
            () => QueryTriggerInteraction.Ignore,
            () => DetectionFilter.All,
            () => playerTransform,
            () => new HashSet<string>(groundIgnoredTags),
            () => currentAbility == PlayerAbility.CanJump,
            () => debugGroundSensor,
            () => gameObject,
            () => GizmoDisplayMode.SelectedOnly,
            () => Color.red);

        // Sensor to detect obstacles in front of the player.
        obstacleSensor = new BoxCollisionSensor(
            () => currentObstacleCenter,
            () => currentObstacleSize,
            () => cachedPlayerRotation,
            () => obstacleLayers,
            () => QueryTriggerInteraction.Ignore,
            () => DetectionFilter.All,
            () => playerTransform,
            () => new HashSet<string>(obstacleIgnoredTags),
            () => true,
            () => debugObstacleSensor,
            () => gameObject,
            () => GizmoDisplayMode.SelectedOnly,
            () => Color.yellow);

        // Sensor to detect ceiling (to prevent player from getting up).
        ceilingSensor = new BoxCollisionSensor(
            () => currentCeilingCenter,
            () => ceilingSensorSize,
            () => cachedPlayerRotation,
            () => ceilingLayers,
            () => QueryTriggerInteraction.Ignore,
            () => DetectionFilter.All,
            () => playerTransform,
            () => new HashSet<string>(ceilingIgnoredTags),
            () => currentAbility == PlayerAbility.CanCrouch,
            () => debugCeilingSensor,
            () => gameObject,
            () => GizmoDisplayMode.SelectedOnly,
            () => Color.blue);

        // Sensor to validate if the ground angle is suitable for walking.
        slopeAngleSensors = new CheckValidAngleSensors(
            () => playerTransform,
            () => highFrictionMaterial,
            () => lowFrictionMaterial,
            () => collidersForAngleCheck,
            () => maxSlopeAngle,
            () => 0.5f,
            () => debugSlopeAngle);
    }

    /// <summary>
    /// Initializes player controllers for jump, movement, rotation and stamina.
    /// </summary>
    private void SetupControllers()
    {
        // Jump controller with ground check and double jump.
        jumpController = new PlayerJumpController(() => playerRigidbody, () => isGrounded, () => maxJumpCount, () => coyoteTimeDuration);

        // Input-based motion controller and frontal collision.
        moveController = new PlayerMoveController(() => playerRigidbody, () => currentSpeed, () => obstacleSensor.collisionDetected);

        // Rotation controller based on input direction.
        rotationController = new PlayerRotationController(() => playerTransform, () => currentMoveInput, () => rotationSmoothSpeed);

        // Stamina based speed controller.
        staminaController = new PlayerSpeedControl(() => walkSpeed, () => runSpeed, () => IsMoving, () => maxStamina, () => minStaminaForRun, () => staminaDepletionRate, () => staminaRecoveryRate);
    }

    /// <summary>
    /// Sets up input bindings and event handlers for jump, move, crouch and run actions.
    /// </summary>
    private void SetupInputBindings()
    {
        // Jump event.
        jumpInputEvent = OnInputSystemEvent<float>.WithAction(inputActions, jumpActionPath, () => isPlayable)
            .OnPressed(_ =>
            {
                if (currentAbility == PlayerAbility.CanJump)
                {
                    jumpController.OnJump(jumpForce);
                }
            });

        // Movement event.
        moveInputEvent = OnInputSystemEvent<Vector2>.WithAction(inputActions, moveActionPath, () => isPlayable)
            .OnHold(value =>
            {
                currentMoveInput = ConvertRotation(value, globalInputRotationOffset);
                moveController.OnMove(currentMoveInput);
            })
            .OnReleased(() =>
            {
                moveController.OnStop();
                currentMoveInput = Vector2.zero;
            });

        // Crouch event.
        crouchInputEvent = OnInputSystemEvent<float>.WithAction(inputActions, crouchActionPath, () => isPlayable)
            .OnPressed(_ =>
            {
                if (controlMode == PlayerControlMode.Automatic)
                {
                    if (!ceilingSensor.collisionDetected)
                    {
                        isCrouching = !isCrouching;
                        currentAbility = isCrouching ? PlayerAbility.CanCrouch : lastNonCrouchAbility;
                    }
                }
                else
                {
                    isCrouching = false;
                    currentAbility = lastNonCrouchAbility;
                }
            });

        // Racing event.
        runInputEvent = OnInputSystemEvent<float>.WithAction(inputActions, runActionPath, () => canRun)
            .OnPressed(_ =>
            {
                isRunning = true;
                staminaController.StartRunning();
            })
            .OnReleased(() =>
            {
                isRunning = false;
                staminaController.StopRunning();
            });
    }

    #endregion

    #region === Update Methods ===

    /// <summary>
    /// Caches player position and rotation for sensor calculations.
    /// </summary>
    private void CacheTransformData()
    {
        cachedPlayerPosition = playerTransform.position; // Update the current player position for use in collision detections.
        cachedPlayerRotation = playerTransform.rotation; // Update the current player rotation for use in collision detections.
    }

    /// <summary>
    /// Updates sensor positions based on player transform and crouching state.
    /// </summary>
    private void UpdateSensorPositions()
    {
        // Calculate the position of the obstacle sensor based on the position and whether the player is crouched.
        currentObstacleCenter = playerTransform.TransformPoint(isCrouching ? crouchObstacleCenter : obstacleSensorCenter);

        // Adjust the size of the obstacle sensor according to the crouching state.
        currentObstacleSize = isCrouching ? crouchObstacleSize : obstacleSensorSize;

        // Updates the ceiling sensor position based on the player's current position.
        currentCeilingCenter = playerTransform.TransformPoint(ceilingSensorCenter);
    }

    /// <summary>
    /// Updates the grounded state based on sensors and player abilities.
    /// </summary>
    private void UpdateGroundedState()
    {
        // Checks if the player is on the ground:
        // - If the current ability does not allow jumping, considers it as on the ground.
        // - Or if the ground sensor detects collision.
        // Also checks if the surface angle is valid for walking or if the player is crouched.
        isGrounded = (currentAbility != PlayerAbility.CanJump || groundSensor.collisionDetected) && (slopeAngleSensors.isValidAngle || isCrouching);
    }

    /// <summary>
    /// Updates ability and crouch logic according to control mode and sensors.
    /// </summary>
    private void UpdateAbilityAndCrouchLogic()
    {
        // If the control mode is Manual and the player is crouched, cancels the crouch and returns the last non-crouch ability.
        if (controlMode == PlayerControlMode.Manual && isCrouching)
        {
            isCrouching = false;
            currentAbility = lastNonCrouchAbility;
        }

        // If the player has no active abilities, force him to be on the ground and not crouched.
        if (currentAbility == PlayerAbility.None)
        {
            isGrounded = true;
            isCrouching = false;
        }

        // Updates the last valid non-crouch skill.
        if (currentAbility != PlayerAbility.CanCrouch)
        {
            lastNonCrouchAbility = currentAbility;
        }

        // Sets whether the player can run based on gameplay and movement mode.
        canRun = isPlayable && movementMode == PlayerMovement.canRun;
    }

    /// <summary>
    /// Updates current movement speed and enables/disables colliders based on crouching.
    /// </summary>
    private void UpdateSpeedAndColliders()
    {
        // Sets the current speed:
        // If crouched, uses run/crouch or walk/crouch speed.
        // Otherwise, uses the speed calculated by the stamina controller.
        currentSpeed = isCrouching ? (IsRunning ? crouchRunSpeed : crouchWalkSpeed) : staminaController.resultingVelocity;

        // Activate the correct collider based on the crouch state.
        standingCollider.enabled = !isCrouching;
        crouchingCollider.enabled = isCrouching;
    }

    #endregion

    #region === Public API ===

    /// <summary>
    /// Sets the player control mode.
    /// </summary>
    /// <param name="mode">Control mode index (Automatic=0, Manual=1).</param>
    public void SetControlMode(int mode)
    {
        // Checks if the received value is a valid value from the PlayerControlMode enum.
        if (Enum.IsDefined(typeof(PlayerControlMode), mode))
        {
            controlMode = (PlayerControlMode)mode;

            // If you enter manual mode and are crouched, cancel the crouch.
            if (controlMode == PlayerControlMode.Manual && isCrouching)
            {
                isCrouching = false;
                currentAbility = lastNonCrouchAbility;
            }
        }
        else
        {
            Debug.LogWarning($"Invalid value for PlayerControlMode: {mode}", this);
        }
    }

    /// <summary>
    /// Sets the player ability.
    /// </summary>
    /// <param name="ability">Ability index (None=0, CanCrouch=1, CanJump=2).</param>
    public void SetPlayerAbility(int ability)
    {
        // Checks if the passed value is a valid value from the PlayerAbility enum.
        if (Enum.IsDefined(typeof(PlayerAbility), ability))
        {
            var newAbility = (PlayerAbility)ability;

            // If the skill is crouch and the control mode is manual, displays a warning and does not allow crouching.
            if (newAbility == PlayerAbility.CanCrouch && controlMode == PlayerControlMode.Manual)
            {
                Debug.LogWarning("Crouching not allowed in Manual mode.", this);
                return;
            }

            currentAbility = newAbility; // Updates the current skill.
            isCrouching = currentAbility == PlayerAbility.CanCrouch; // Updates crouching state according to the new skill.

            // Updates the last non-crouch skill.
            if (currentAbility != PlayerAbility.CanCrouch)
            {
                lastNonCrouchAbility = currentAbility;
            }
        }
        else
        {
            Debug.LogWarning($"Invalid value for PlayerAbility: {ability}", this);
        }
    }

    /// <summary>
    /// Enables or disables running movement mode.
    /// </summary>
    /// <param name="enabled">True to enable running, false to disable.</param>
    public void SetRunEnabled(bool enabled) => movementMode = enabled ? PlayerMovement.canRun : PlayerMovement.justWalk;

    /// <summary>
    /// Toggles whether the player can receive input and be controlled.
    /// </summary>
    /// <param name="enabled">True to make playable, false otherwise.</param>
    public void TogglePlayable(bool enabled) => isPlayable = enabled;

    /// <summary>
    /// Saves player state data to a JSON string.
    /// </summary>
    /// <returns>Serialized JSON string with player data.</returns>
    public string SavePlayerData()
    {
        // Creates a dictionary with essential player data to save.
        return CustomPlayerData.SaveData(() => new Dictionary<string, object>
        {
            { nameof(controlMode), controlMode },                       // Current control mode.
            { nameof(currentAbility), currentAbility },                 // Current ability.
            { nameof(movementMode), movementMode },                     // Movement mode (walking/running).
            { nameof(lastNonCrouchAbility), lastNonCrouchAbility },     // Last ability other than crouch.
            { nameof(cachedPlayerPosition), playerTransform.position }, // Current player position.
            { nameof(cachedPlayerRotation), playerTransform.rotation }, // Current player rotation.
            { nameof(isGrounded), isGrounded },                         // Grounded state.
            { nameof(isCrouching), isCrouching }                        // Crouched state.
        });
    }

    /// <summary>
    /// Loads player state data from a JSON string.
    /// </summary>
    /// <param name="json">Serialized JSON string with player data.</param>
    public void LoadPlayerData(string json)
    {
        // Deserialize the JSON and apply the saved values ​​to the player's 
        CustomPlayerData.LoadData(json, dict =>
        {
            controlMode = (PlayerControlMode)dict[nameof(controlMode)];               // Reset control mode.
            currentAbility = (PlayerAbility)dict[nameof(currentAbility)];             // Reset current ability.
            movementMode = (PlayerMovement)dict[nameof(movementMode)];                // Reset movement mode.
            lastNonCrouchAbility = (PlayerAbility)dict[nameof(lastNonCrouchAbility)]; // Reset last ability that was not crouch.

            // Update player position and rotation in the scene.
            playerTransform.SetPositionAndRotation((Vector3)dict[nameof(cachedPlayerPosition)], (Quaternion)dict[nameof(cachedPlayerRotation)]);

            isGrounded = Convert.ToBoolean(dict[nameof(isGrounded)]);   // Restore grounded state.
            isCrouching = Convert.ToBoolean(dict[nameof(isCrouching)]); // Restore crouched state.
        });
    }
    #endregion

    #region === Cleanup ===

    /// <summary>
    /// Disposes all sensors, controllers and unbinds input events.
    /// </summary>
    private void DisposeAll()
    {
        // Remove sensors, freeing up resources.
        BoxCollisionSensor.Dispose(ref groundSensor);
        BoxCollisionSensor.Dispose(ref obstacleSensor);
        BoxCollisionSensor.Dispose(ref ceilingSensor);

        // Remove angle sensor.
        CheckValidAngleSensors.Dispose(ref slopeAngleSensors);

        // Remove controllers from the player.
        PlayerJumpController.Dispose(ref jumpController);
        PlayerMoveController.Dispose(ref moveController);
        PlayerRotationController.Dispose(ref rotationController);
        PlayerSpeedControl.Dispose(ref staminaController);

        // Remove all input events.
        jumpInputEvent?.UnbindAll();
        moveInputEvent?.UnbindAll();
        crouchInputEvent?.UnbindAll();
        runInputEvent?.UnbindAll();
    }

    #endregion
}