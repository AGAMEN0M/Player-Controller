/*
 * ---------------------------------------------------------------------------
 * Description: Comprehensive 3D side-view player controller for Unity.
 *              Manages input, movement, jumping, crouching, running, rotation,
 *              stamina, and collision detection using modular systems and sensor logic.
 *              Designed for extensibility and gameplay flexibility.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using PlayerController.PhysicsRuntime;
using PlayerController.InputEvents;
using PlayerController.CustomData;
using PlayerController.Attributes;
using PlayerController.Abilities;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

#if UNITY_EDITOR
using PlayerController.Editor;
using UnityEditor;
#endif

using static PlayerController.PhysicsRuntime.BoxCollisionSensor;
using static PlayerController.Utils.PlayerUtils;

namespace PlayerController
{
    /// <summary>
    /// Core 3D player controller for Unity. Handles input, movement, jumping, crouching,
    /// running, collision detection, stamina, and camera rotation logic.
    /// Designed for modular extension using sensors, input events, and gameplay states.
    /// </summary>
    [AddComponentMenu("Tools/Player Controller/3D/Player Controller (First Person)")]
    public partial class FirstPersonPlayerController3D : MonoBehaviour
    {
        #region === Enums ===

        /// <summary>
        /// Defines how the player control behaves.
        /// </summary>
        public enum PlayerControlMode
        {
            /// <summary>Automatic control mode.</summary>
            Automatic,

            /// <summary>Manual control mode.</summary>
            Manual
        }

        /// <summary>
        /// Defines available abilities the player can have.
        /// </summary>
        public enum PlayerAbility
        {
            /// <summary>No abilities.</summary>
            None,

            /// <summary>Ability to crouch.</summary>
            CanCrouch,

            /// <summary>Ability to jump.</summary>
            CanJump
        }

        /// <summary>
        /// Defines player movement modes.
        /// </summary>
        public enum PlayerMovement
        {
            /// <summary>Player can only walk.</summary>
            justWalk,

            /// <summary>Player can run.</summary>
            canRun
        }

        #endregion

        #region === Serialized Fields ===

        [Header("Input Settings")]
        [SerializeField, ValidateReference, Tooltip("Input action that controls player movement.")]
        private InputActionReference moveAction;

        [SerializeField, ValidateReference, Tooltip("Input action that triggers running when pressed.")]
        private InputActionReference runAction;

        [SerializeField, ValidateReference, Tooltip("Input action that triggers jump behavior.")]
        private InputActionReference jumpAction;

        [SerializeField, ValidateReference, Tooltip("Input action that triggers crouch behavior.")]
        private InputActionReference crouchAction;

        [Header("References")]
        [SerializeField, ValidateReference, Tooltip("Transform component representing the player in the scene.")]
        private Transform playerTransform;

        [SerializeField, ValidateReference, Tooltip("Rigidbody component responsible for physics-based movement.")]
        private Rigidbody playerRigidbody;

        [SerializeField, ValidateReference, Tooltip("Collider used when the player is standing upright.")]
        private Collider standingCollider;

        [SerializeField, ValidateReference, Tooltip("Collider used when the player is crouching.")]
        private Collider crouchingCollider;

        [Header("Camera Settings")]
        [SerializeField, ValidateReference, Tooltip("Reference to the pivot transform used to position and rotate the camera.")]
        private Transform cameraPivot;

        [SerializeField, Tooltip("Local position of the camera when the player is standing.")]
        private Vector3 standingCameraLocalPos = new(0f, 1.4f, 0f);

        [SerializeField, Tooltip("Local position of the camera when the player is crouching.")]
        private Vector3 crouchingCameraLocalPos = new(0f, 0.3f, 0.4f);

        [SerializeField, Tooltip("Speed at which the camera smoothly interpolates between positions.")]
        private float cameraLerpSpeed = 10f;

        [Header("Movement Settings")]
        [SerializeField, Tooltip("Base speed applied when walking.")]
        private float walkSpeed = 1f;

        [SerializeField, Tooltip("Base speed applied when running.")]
        private float runSpeed = 5f;

        [Space(5)]

        [SerializeField, Tooltip("Walking speed applied while crouching.")]
        private float crouchWalkSpeed = 1f;

        [SerializeField, Tooltip("Running speed applied while crouching.")]
        private float crouchRunSpeed = 1.5f;

        [Space(5)]

        [SerializeField, Tooltip("Vertical force applied to the player when jumping.")]
        private float jumpForce = 5f;

        [SerializeField, Tooltip("Maximum number of consecutive jumps allowed (e.g., double jump).")]
        private int maxJumpCount = 2;

        [SerializeField, Tooltip("Extra time allowed to jump after leaving the ground.")]
        private float coyoteTimeDuration = 0.15f;

        [Header("Stamina Settings")]
        [SerializeField, Tooltip("Maximum stamina value the player can have.")]
        private float maxStamina = 50f;

        [SerializeField, Tooltip("Minimum stamina required before the player can run.")]
        private float minStaminaForRun = 12.5f;

        [Space(5)]

        [SerializeField, Tooltip("Rate at which stamina decreases while running.")]
        private float staminaDepletionRate = 15f;

        [SerializeField, Tooltip("Rate at which stamina regenerates while idle or walking.")]
        private float staminaRecoveryRate = 3f;

        [Header("Player State Settings")]
        [SerializeField, Tooltip("Defines how player control input is handled (automatic or manual).")]
        private PlayerControlMode controlMode = PlayerControlMode.Automatic;

        [SerializeField, Tooltip("Defines the current active ability the player can use.")]
        private PlayerAbility currentAbility = PlayerAbility.CanJump;

        [SerializeField, Tooltip("Defines current movement mode (walking or running capability).")]
        private PlayerMovement movementMode = PlayerMovement.canRun;

        [Header("Collision Layers")]
        [SerializeField, Tooltip("Specifies which layers are treated as ground for movement detection.")]
        private LayerMask groundLayers = -1;

        [SerializeField, Tooltip("Specifies which layers are treated as obstacles for movement blocking.")]
        private LayerMask obstacleLayers = -1;

        [SerializeField, Tooltip("Specifies which layers are treated as ceilings for crouch detection.")]
        private LayerMask ceilingLayers = -1;

        [Header("Ground Sensor Settings")]
        [SerializeField, Tooltip("Defines the size of the ground detection box for grounded checks.")]
        private Vector3 groundSensorBoxSize = new(0.2f, 0.3f, 0.2f);

        [SerializeField, TagDropdown, Tooltip("List of tags ignored by the ground sensor during detection.")]
        private List<string> groundIgnoredTags = new() { "Ignore Collision" };

        [Header("Obstacle Sensor Settings")]
        [SerializeField, Tooltip("Center offset of the obstacle detection box.")]
        private Vector3 obstacleSensorCenter = new(0f, 0.8f, 0.45f);

        [SerializeField, Tooltip("Defines the size of the obstacle detection box.")]
        private Vector3 obstacleSensorSize = new(0.25f, 1.5f, 0.2f);

        [Space(5)]

        [SerializeField, Tooltip("Center offset of the obstacle detection box when crouching.")]
        private Vector3 crouchObstacleCenter = new(0f, 0.35f, 0.45f);

        [SerializeField, Tooltip("Size of the obstacle detection box when crouching.")]
        private Vector3 crouchObstacleSize = new(0.25f, 0.4f, 0.2f);

        [SerializeField, TagDropdown, Tooltip("List of tags ignored by the obstacle sensor during detection.")]
        private List<string> obstacleIgnoredTags = new() { "Ignore Collision" };

        [Header("Ceiling Sensor Settings")]
        [SerializeField, Tooltip("Center offset of the ceiling detection box.")]
        private Vector3 ceilingSensorCenter = new(0f, 0.8f, 0f);

        [SerializeField, Tooltip("Defines the size of the ceiling detection box.")]
        private Vector3 ceilingSensorSize = new(0.34f, 1.4f, 0.36f);

        [SerializeField, TagDropdown, Tooltip("List of tags ignored by the ceiling sensor during detection.")]
        private List<string> ceilingIgnoredTags = new() { "Ignore Collision" };

        [Header("Physics Material Settings")]
        [SerializeField, ValidateReference, Tooltip("Physics material used for high-friction ground surfaces.")]
        private PhysicsMaterial highFrictionMaterial;

        [SerializeField, ValidateReference, Tooltip("Physics material used for low-friction ground surfaces.")]
        private PhysicsMaterial lowFrictionMaterial;

        [Space(5)]

        [SerializeField, Range(0f, 90f), Tooltip("Maximum slope angle in degrees that the player can walk on.")]
        private float maxSlopeAngle = 45f;

        [Header("Debug Settings")]
        [SerializeField, Tooltip("Enables visual debug for ground sensor.")]
        private bool debugGroundSensor = false;

        [SerializeField, Tooltip("Enables visual debug for obstacle sensor.")]
        private bool debugObstacleSensor = false;

        [SerializeField, Tooltip("Enables visual debug for ceiling sensor.")]
        private bool debugCeilingSensor = false;

        [SerializeField, Tooltip("Enables visual debug for current slope angle.")]
        private bool debugSlopeAngle = false;

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

        // Timing and action control for crouch input.
        private readonly ActionBlockTimer crouchBlock = new(0.15f); // Prevents rapid toggling of crouch state; blocks input for 0.15 seconds after a crouch action.
        private bool allowCrouchThisFrame; // Temporary flag to check if crouch action is allowed on the current frame.

        #endregion

        #region === Public Properties ===

        /// <summary>
        /// Returns whether the player is currently grounded.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// Returns whether the player is currently crouching.
        /// </summary>
        public bool IsCrouching => isCrouching;

        /// <summary>
        /// Returns true if the player is moving (non-zero input) and no obstacle is blocking movement.
        /// </summary>
        public bool IsMoving => currentMoveInput.sqrMagnitude > 0.01f && !obstacleSensor.collisionDetected && isPlayable;

        /// <summary>
        /// Returns true if the player is running, has stamina, and is allowed to run.
        /// </summary>
        public bool IsRunning => isRunning && staminaController.hasStamina && canRun;

        /// <summary>
        /// Current stamina value normalized as a percentage [0..100].
        /// </summary>
        public float StaminaPercent => staminaController.staminaPercentage;

        /// <summary>
        /// Returns whether the player is currently playable (can receive input).
        /// </summary>
        public bool IsPlayable => isPlayable;

        #endregion

        #region === Properties ===

        /// <summary>
        /// Gets or sets the input action used for player movement.
        /// </summary>
        public InputActionReference MoveAction
        {
            get => moveAction;
            set => moveAction = value;
        }

        /// <summary>
        /// Gets or sets the input action used for running.
        /// </summary>
        public InputActionReference RunAction
        {
            get => runAction;
            set => runAction = value;
        }

        /// <summary>
        /// Gets or sets the input action used for jumping.
        /// </summary>
        public InputActionReference JumpAction
        {
            get => jumpAction;
            set => jumpAction = value;
        }

        /// <summary>
        /// Gets or sets the input action used for crouching.
        /// </summary>
        public InputActionReference CrouchAction
        {
            get => crouchAction;
            set => crouchAction = value;
        }

        /// <summary>
        /// Gets or sets the player's Transform component.
        /// </summary>
        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        /// <summary>
        /// Gets or sets the player's Rigidbody component.
        /// </summary>
        public Rigidbody PlayerRigidbody
        {
            get => playerRigidbody;
            set => playerRigidbody = value;
        }

        /// <summary>
        /// Gets or sets the collider used when the player is standing.
        /// </summary>
        public Collider StandingCollider
        {
            get => standingCollider;
            set => standingCollider = value;
        }

        /// <summary>
        /// Gets or sets the collider used when the player is crouching.
        /// </summary>
        public Collider CrouchingCollider
        {
            get => crouchingCollider;
            set => crouchingCollider = value;
        }

        /// <summary>
        /// Gets or sets the pivot transform of the player's camera.
        /// </summary>
        public Transform CameraPivot
        {
            get => cameraPivot;
            set => cameraPivot = value;
        }

        /// <summary>
        /// Gets or sets the local camera position when standing.
        /// </summary>
        public Vector3 StandingCameraLocalPos
        {
            get => standingCameraLocalPos;
            set => standingCameraLocalPos = value;
        }

        /// <summary>
        /// Gets or sets the local camera position when crouching.
        /// </summary>
        public Vector3 CrouchingCameraLocalPos
        {
            get => crouchingCameraLocalPos;
            set => crouchingCameraLocalPos = value;
        }

        /// <summary>
        /// Gets or sets the speed of camera position interpolation.
        /// </summary>
        public float CameraLerpSpeed
        {
            get => cameraLerpSpeed;
            set => cameraLerpSpeed = value;
        }

        /// <summary>
        /// Gets or sets the player's walking speed.
        /// </summary>
        public float WalkSpeed
        {
            get => walkSpeed;
            set => walkSpeed = value;
        }

        /// <summary>
        /// Gets or sets the player's running speed.
        /// </summary>
        public float RunSpeed
        {
            get => runSpeed;
            set => runSpeed = value;
        }

        /// <summary>
        /// Gets or sets the player's crouch walking speed.
        /// </summary>
        public float CrouchWalkSpeed
        {
            get => crouchWalkSpeed;
            set => crouchWalkSpeed = value;
        }

        /// <summary>
        /// Gets or sets the player's crouch running speed.
        /// </summary>
        public float CrouchRunSpeed
        {
            get => crouchRunSpeed;
            set => crouchRunSpeed = value;
        }

        /// <summary>
        /// Gets or sets the jump force applied to the player.
        /// </summary>
        public float JumpForce
        {
            get => jumpForce;
            set => jumpForce = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of jumps allowed.
        /// </summary>
        public int MaxJumpCount
        {
            get => maxJumpCount;
            set => maxJumpCount = value;
        }

        /// <summary>
        /// Gets or sets the coyote time duration for jump forgiveness.
        /// </summary>
        public float CoyoteTimeDuration
        {
            get => coyoteTimeDuration;
            set => coyoteTimeDuration = value;
        }

        /// <summary>
        /// Gets or sets the maximum stamina value for the player.
        /// </summary>
        public float MaxStamina
        {
            get => maxStamina;
            set => maxStamina = value;
        }

        /// <summary>
        /// Gets or sets the minimum stamina required to start running.
        /// </summary>
        public float MinStaminaForRun
        {
            get => minStaminaForRun;
            set => minStaminaForRun = value;
        }

        /// <summary>
        /// Gets or sets the stamina depletion rate while running.
        /// </summary>
        public float StaminaDepletionRate
        {
            get => staminaDepletionRate;
            set => staminaDepletionRate = value;
        }

        /// <summary>
        /// Gets or sets the stamina recovery rate when not running.
        /// </summary>
        public float StaminaRecoveryRate
        {
            get => staminaRecoveryRate;
            set => staminaRecoveryRate = value;
        }

        /// <summary>
        /// Gets or sets the ground detection layers.
        /// </summary>
        public LayerMask GroundLayers
        {
            get => groundLayers;
            set => groundLayers = value;
        }

        /// <summary>
        /// Gets or sets the obstacle detection layers.
        /// </summary>
        public LayerMask ObstacleLayers
        {
            get => obstacleLayers;
            set => obstacleLayers = value;
        }

        /// <summary>
        /// Gets or sets the ceiling detection layers.
        /// </summary>
        public LayerMask CeilingLayers
        {
            get => ceilingLayers;
            set => ceilingLayers = value;
        }

        /// <summary>
        /// Gets or sets the physics material used for high friction surfaces.
        /// </summary>
        public PhysicsMaterial HighFrictionMaterial
        {
            get => highFrictionMaterial;
            set => highFrictionMaterial = value;
        }

        /// <summary>
        /// Gets or sets the physics material used for low friction surfaces.
        /// </summary>
        public PhysicsMaterial LowFrictionMaterial
        {
            get => lowFrictionMaterial;
            set => lowFrictionMaterial = value;
        }

        /// <summary>
        /// Gets or sets the maximum slope angle the player can walk on.
        /// </summary>
        public float MaxSlopeAngle
        {
            get => maxSlopeAngle;
            set => maxSlopeAngle = value;
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
        /// Updates player state, sensors and action timers.
        /// </summary>
        private void Update()
        {
            CacheTransformData();
            UpdateSensorPositions();
            UpdateGroundedState();
            UpdateAbilityAndCrouchLogic();
            UpdateSpeedAndColliders();
            UpdateCameraPivotPosition();
            crouchBlock.Update();
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed.
        /// Disposes all allocated resources.
        /// </summary>
        private void OnDestroy() => DisposeAll();

        /// <summary>
        /// Draws gizmos to visualize camera pivot positions for standing and crouching states in the editor.
        /// This method is only called when the object is selected in the hierarchy.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // If player transform is not assigned, skip gizmo drawing.
            if (playerTransform == null) return;

            // Convert local camera positions to world positions based on player transform.
            Vector3 standingWorldPos = playerTransform.TransformPoint(standingCameraLocalPos);
            Vector3 crouchingWorldPos = playerTransform.TransformPoint(crouchingCameraLocalPos);

            // Draw sphere for standing camera position (green).
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(standingWorldPos, 0.03f);

            // Draw sphere for crouching camera position (cyan).
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(crouchingWorldPos, 0.03f);

            // Draw line connecting both positions (magenta).
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(standingWorldPos, crouchingWorldPos);
        }

        #endregion

        #region === Initialization ===

        /// <summary>
        /// Validates if all required serialized references are assigned.
        /// Logs warnings if any references are missing.
        /// </summary>
        private void ValidateSerializedReferences()
        {
            if (moveAction == null) Debug.LogWarning("Move Action not assigned.", this);
            if (runAction == null) Debug.LogWarning("Run Action not assigned.", this);
            if (jumpAction == null) Debug.LogWarning("Jump Action not assigned.", this);
            if (crouchAction == null) Debug.LogWarning("Crouch Action not assigned.", this);
            if (!playerTransform) Debug.LogWarning("Player Transform not assigned.", this);
            if (!playerRigidbody) Debug.LogWarning("Rigidbody not assigned.", this);
            if (!cameraPivot) Debug.LogWarning("cameraPivot not assigned.", this);
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

            // Stamina based speed controller.
            staminaController = new PlayerSpeedControl(() => walkSpeed, () => runSpeed, () => IsMoving, () => maxStamina, () => minStaminaForRun, () => staminaDepletionRate, () => staminaRecoveryRate);
        }

        /// <summary>
        /// Sets up input bindings and event handlers for jump, move, crouch and run actions.
        /// </summary>
        private void SetupInputBindings()
        {
            // Jump event.
            jumpInputEvent = OnInputSystemEvent<float>.WithAction(jumpAction, this, () => isPlayable)
                .OnPressed(_ =>
                {
                    if (currentAbility == PlayerAbility.CanJump)
                    {
                        jumpController.OnJump(jumpForce);
                        crouchBlock.Activate();
                    }
                });

            // Movement event.
            moveInputEvent = OnInputSystemEvent<Vector2>.WithAction(moveAction, this, () => isPlayable)
                .OnHold(value =>
                {
                    currentMoveInput = ConvertRotation(value, -cachedPlayerRotation.eulerAngles.y);
                    moveController.OnMove(currentMoveInput);
                })
                .OnReleased(() =>
                {
                    moveController.OnStop();
                    currentMoveInput = Vector2.zero;
                });

            // Crouch event.
            crouchInputEvent = OnInputSystemEvent<float>.WithAction(crouchAction, this, () => isPlayable)
                .OnPressed(_ =>
                {
                    if (crouchBlock.IsBlocked) return;

                    if (controlMode == PlayerControlMode.Automatic)
                    {
                        if ((isGrounded || allowCrouchThisFrame) && !ceilingSensor.collisionDetected)
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
            runInputEvent = OnInputSystemEvent<float>.WithAction(runAction, this, () => canRun)
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
        /// Updates sensor positions based on movement direction and crouching state.
        /// </summary>
        private void UpdateSensorPositions()
        {
            Vector3 baseCenter = isCrouching ? crouchObstacleCenter : obstacleSensorCenter; // Selects the obstacle sensor's base center offset based on crouch state.
            currentObstacleSize = isCrouching ? crouchObstacleSize : obstacleSensorSize; // Selects the obstacle sensor's size based on crouch state.

            // If the player is moving, position the sensor in the direction of movement.
            if (currentMoveInput.sqrMagnitude > 0.001f)
            {
                Vector3 moveDir = new(currentMoveInput.x, 0f, currentMoveInput.y); // Converts the input vector to a 3D world direction (XZ plane).

                // Places the sensor ahead in the movement direction (z), applies lateral offset (x) using the player's right direction, and applies vertical offset (y) in world space.
                currentObstacleCenter = playerTransform.position + moveDir * baseCenter.z + playerTransform.right * baseCenter.x + Vector3.up * baseCenter.y;
            }
            else
            {
                // If not moving, fallback to using the local forward-based position.
                currentObstacleCenter = playerTransform.TransformPoint(baseCenter);
            }

            // Updates the ceiling sensor position based on the player transform.
            currentCeilingCenter = playerTransform.TransformPoint(ceilingSensorCenter);
        }

        /// <summary>
        /// Updates the grounded state based on sensors, abilities, and slope validation.
        /// </summary>
        private void UpdateGroundedState()
        {
            bool wasGrounded = isGrounded; // Cache previous grounded state to detect transitions.

            // Checks if the player is on the ground:
            // - If the current ability does not allow jumping, considers it as on the ground.
            // - Or if the ground sensor detects collision.
            // Also checks if the surface angle is valid for walking or if the player is crouched.
            isGrounded = (currentAbility != PlayerAbility.CanJump || groundSensor.collisionDetected) && (slopeAngleSensors.isValidAngle || isCrouching);

            // Allow crouch input on this frame only if the player just landed.
            allowCrouchThisFrame = !wasGrounded && isGrounded;
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

        /// <summary>
        /// Updates the camera pivot position based on crouching state.
        /// Smoothly interpolates between standing and crouching positions.
        /// </summary>
        private void UpdateCameraPivotPosition()
        {
            Vector3 targetLocalPosition = isCrouching ? crouchingCameraLocalPos : standingCameraLocalPos;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetLocalPosition, Time.deltaTime * cameraLerpSpeed);
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
            PlayerSpeedControl.Dispose(ref staminaController);

            // Remove all input events.
            jumpInputEvent?.Dispose();
            moveInputEvent?.Dispose();
            crouchInputEvent?.Dispose();
            runInputEvent?.Dispose();
        }

        #endregion
    }

#if UNITY_EDITOR

    #region === Custom Inspector ===

    public partial class FirstPersonPlayerController3D
    {
        [CanEditMultipleObjects]
        [CustomEditor(typeof(FirstPersonPlayerController3D))]
        public class FirstPersonPlayerController3DEditor : CustomInspectorBase<FirstPersonPlayerController3D>
        {
            /// <summary>
            /// Renders the custom inspector layout for the FirstPersonPlayerController3D component.
            /// Organizes all serialized fields into logical, grouped sections for better usability.
            /// </summary>
            protected override void DrawInspector()
            {
                DrawGroup(new GUIContent("Input Settings", "Input Actions used to control movement, running, jumping and crouching."), () =>
                {
                    GUIProperty(nameof(script.moveAction));
                    GUIProperty(nameof(script.runAction));
                    GUIProperty(nameof(script.jumpAction));
                    GUIProperty(nameof(script.crouchAction));
                });

                DrawGroup(new GUIContent("References", "Core references required for the player controller to function properly."), () =>
                {
                    GUIProperty(nameof(script.playerTransform));
                    GUIProperty(nameof(script.playerRigidbody));
                    GUIProperty(nameof(script.standingCollider));
                    GUIProperty(nameof(script.crouchingCollider));
                });

                DrawGroup(new GUIContent("Camera Settings", "Controls camera pivot positioning and smoothing between standing and crouching states."), () =>
                {
                    GUIProperty(nameof(script.cameraPivot));
                    GUIProperty(nameof(script.standingCameraLocalPos));
                    GUIProperty(nameof(script.crouchingCameraLocalPos));
                    GUIProperty(nameof(script.cameraLerpSpeed));
                });

                DrawGroup(new GUIContent("Movement Settings", "Defines movement speeds, rotation behavior and jump parameters."), () =>
                {
                    GUIProperty(nameof(script.walkSpeed));
                    GUIProperty(nameof(script.runSpeed));
                    GUIProperty(nameof(script.crouchWalkSpeed));
                    GUIProperty(nameof(script.crouchRunSpeed));
                    GUIProperty(nameof(script.jumpForce));
                    GUIProperty(nameof(script.maxJumpCount));
                    GUIProperty(nameof(script.coyoteTimeDuration));
                });

                DrawGroup(new GUIContent("Stamina Settings", "Controls stamina consumption, recovery and running limits."), () =>
                {
                    GUIProperty(nameof(script.maxStamina));
                    GUIProperty(nameof(script.minStaminaForRun));
                    GUIProperty(nameof(script.staminaDepletionRate));
                    GUIProperty(nameof(script.staminaRecoveryRate));
                });

                DrawGroup(new GUIContent("Player State Settings", "Defines control mode, ability state and movement mode."), () =>
                {
                    GUIProperty(nameof(script.controlMode));
                    GUIProperty(nameof(script.currentAbility));
                    GUIProperty(nameof(script.movementMode));
                });

                DrawGroup(new GUIContent("Collision Layers", "Layer masks used for ground, obstacle and ceiling detection."), () =>
                {
                    GUIProperty(nameof(script.groundLayers));
                    GUIProperty(nameof(script.obstacleLayers));
                    GUIProperty(nameof(script.ceilingLayers));
                });

                DrawGroup(new GUIContent("Ground Sensor Settings", "Configuration for ground detection box and ignored tags."), () =>
                {
                    GUIProperty(nameof(script.groundSensorBoxSize));
                    GUIProperty(nameof(script.groundIgnoredTags));
                });

                DrawGroup(new GUIContent("Obstacle Sensor Settings", "Configuration for obstacle detection including crouch handling."), () =>
                {
                    GUIProperty(nameof(script.obstacleSensorCenter));
                    GUIProperty(nameof(script.obstacleSensorSize));
                    GUIProperty(nameof(script.crouchObstacleCenter));
                    GUIProperty(nameof(script.crouchObstacleSize));
                    GUIProperty(nameof(script.obstacleIgnoredTags));
                });

                DrawGroup(new GUIContent("Ceiling Sensor Settings", "Configuration used to prevent standing up when blocked above."), () =>
                {
                    GUIProperty(nameof(script.ceilingSensorCenter));
                    GUIProperty(nameof(script.ceilingSensorSize));
                    GUIProperty(nameof(script.ceilingIgnoredTags));
                });

                DrawGroup(new GUIContent("Physics Material Settings", "Physics materials and slope limitation configuration."), () =>
                {
                    GUIProperty(nameof(script.highFrictionMaterial));
                    GUIProperty(nameof(script.lowFrictionMaterial));
                    GUIProperty(nameof(script.maxSlopeAngle));
                });

                DrawGroup(new GUIContent("Debug Settings", "Visual debugging options for sensors and slope validation."), () =>
                {
                    GUIProperty(nameof(script.debugGroundSensor));
                    GUIProperty(nameof(script.debugObstacleSensor));
                    GUIProperty(nameof(script.debugCeilingSensor));
                    GUIProperty(nameof(script.debugSlopeAngle));
                });
            }
        }
    }

    #endregion

#endif
}