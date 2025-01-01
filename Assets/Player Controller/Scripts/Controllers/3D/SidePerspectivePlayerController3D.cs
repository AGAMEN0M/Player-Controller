using System.Collections.Generic;
using CustomKeyboard;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("Player/Controllers/3D/Side Perspective")]
public class SidePerspectivePlayerController3D : MonoBehaviour
{
    [System.Serializable]
    public class MovementSettings
    {
        [HighlightEmptyReference] public Transform playerTransform; // Reference to the player's transform for positioning.
        [HighlightEmptyReference] public Rigidbody playerRigidbody; // Reference to the player's Rigidbody component for physics interactions.
        [Space(10)]
        public float movementSpeed = 1f; // The default speed at which the player moves.
        public float rotationSpeed = 10f; // The speed at which the player rotates to face movement direction.
        [Space(10)]
        public Vector3 detectObstaclesCenter = new(0, 0.8f, 0.45f); // Center point for obstacle detection box.
        public Vector3 detectObstaclesSize = new(0.25f, 1, 0.2f); // Size of the obstacle detection box.
        [TagDropdown] public List<string> ignoreCollisionWithTags = new(); // List of tags for which collisions should be ignored.
        [ReadOnly] public bool isObstacle = false; // Flag indicating whether an obstacle is detected in the player's path.
    }

    [System.Serializable]
    public class RunSettings
    {
        public float runSpeed = 5f; // Speed at which the player runs.
        [ReadOnly] public bool isRunning = false; // Flag indicating whether the player is currently running.
    }

    [System.Serializable]
    public class StaminaConfig
    {
        public float currentStamina; // Current stamina value of the player.
        [Space(5)]
        public float maxStamina = 50f; // Maximum stamina the player can have.
        public float minimumAmountVigor = 12.5f; // Minimum stamina required for vigorous activities like running.
        [Space(5)]
        public float staminaDepletionRate = 15f; // Rate at which stamina decreases while running.
        public float staminaRecoveryRate = 3f; // Rate at which stamina recovers when not running.
        [ReadOnly] public bool hasStamina = true; // Flag indicating whether the player has any stamina left.
    }

    [System.Serializable]
    public class CrouchSettings
    {
        public float crouchingSpeed = 1f; // Speed at which the player moves while crouching.
        [ConditionalHide("canRunning")] public float crouchedRunSpeed = 1.5f; // Running speed when crouched.
        [Space(5)]
        public Vector3 spaceDetectionCenter = new(0, 0.8f, 0); // Center point for space detection while crouching.
        public Vector3 spaceDetectionSize = new(0.34f, 1.4f, 0.36f); // Size of the detection box to check for available space to crouch.
        [Space(5)]
        public Vector3 newDetectObstaclesCenter = new(0, 0.35f, 0.45f); // Center point for obstacle detection while crouching.
        public Vector3 newDetectObstaclesSize = new(0.25f, 0.4f, 0.2f); // Size of the obstacle detection box while crouching.
        [Space(5)]
        [HighlightEmptyReference] public Collider standingCollider; // Collider used for standing position.
        [HighlightEmptyReference] public Collider crouchCollider; // Collider used for crouching position.
        [Space(5)]
        [TagDropdown] public List<string> ignoreCollisionWithTags = new(); // List of tags for which collisions should be ignored.
        [ReadOnly] public bool isCrouching = false; // Flag indicating whether the player is currently crouching.
    }

    [System.Serializable]
    public class JumpAndGroundSettings
    {
        public float maxJumpHeight = 1.5f; // Maximum height the player can jump.
        public float coyoteTime = 0.15f; // Delay time to set to false.
        [Space(5)]
        public Vector3 groundDetectionCenter = Vector3.zero; // Center point for ground detection.
        public Vector3 groundDetectionSize = new(0.2f, 0.3f, 0.2f); // Size of the box used to check if the player is grounded.
        [TagDropdown] public List<string> ignoreCollisionWithTags = new(); // List of tags for which collisions should be ignored.
        [ReadOnly] public bool isGrounded = true; // Flag indicating whether the player is currently on the ground.
    }

    [System.Serializable]
    public class SurfaceFrictionSettings
    {
        [HighlightEmptyReference] public PhysicsMaterial physicMaterial; // Physics material to control surface friction.
        [Space(5)]
        public float maxAngle = 40; // Maximum angle for walking on slopes without slipping.
        [ConditionalHide("physicMaterialSettings.compensateSpeed")] public float minAngle = 15; // Minimum angle for speed compensation.
        [Space(5)]
        public float slopeFriction = 1f; // Friction value applied on slopes.
        public float outsideSlopeFriction = 0f; // Friction value applied on steep slopes.
        public float frictionInAir = 0f; // Friction value applied while in the air.
    }

    [System.Serializable]
    public class MovementInputBindings
    {
        [KeyboardTagDropdown] public string forwardInputTag = "Up"; // Input tag for moving the player forward.
        [KeyboardTagDropdown] public string backInputTag = "Down"; // Input tag for moving the player backward.
        [KeyboardTagDropdown] public string rightInputTag = "Right"; // Input tag for moving the player to the right.
        [KeyboardTagDropdown] public string leftInputTag = "Left"; // Input tag for moving the player to the left.
        [ConditionalHide("canRunning")][KeyboardTagDropdown] public string runInputTag = "RunSpeed"; // Input tag for triggering running.
        [ConditionalHide("canCrouching")][KeyboardTagDropdown] public string crouchingInputTag = "Crouching"; // Input tag for triggering crouching.
        [ConditionalHide("canJump")][KeyboardTagDropdown] public string jumpInputTag = "Jump"; // Input tag for triggering jumping.
    }

    [Header("Player Settings")]
    public MovementSettings movementSettings; // Configuration settings for player movement.
    [Space(5)]
    public bool canRunning = true; // Flag indicating if the player can run.
    [ConditionalHide("canRunning")] public RunSettings runningSettings; // Settings for running mechanics.
    [Space(5)]
    [ConditionalHide("canRunning")] public bool useStamina = true; // Flag indicating if the player uses stamina while running.
    [ConditionalHide("canRunning", "useStamina")] public StaminaConfig staminaSettings; // Stamina configuration settings.
    [Space(5)]
    public bool canCrouching = true; // Flag indicating if the player can crouch.
    [ConditionalHide("canCrouching")] public CrouchSettings crouchingSettings; // Settings for crouching mechanics.
    [Space(5)]
    public bool canJump = true; // Flag indicating if the player can jump.
    [ConditionalHide("canJump")] public JumpAndGroundSettings gravitySettings; // Settings for jumping and ground detection.
    [ConditionalHide("canJump")] public SurfaceFrictionSettings physicMaterialSettings; // Settings for surface friction.
    [Space(5)]
    public MovementInputBindings movementBaseControls; // Input bindings for player movement controls.

    private InputData forwardInput; // Input data for forward movement.
    private InputData backInput; // Input data for backward movement.
    private InputData rightInput; // Input data for rightward movement.
    private InputData leftInput; // Input data for leftward movement.
    private InputData runInput; // Input data for running.
    private InputData crouchingInput; // Input data for crouching.
    private InputData jumpInput; // Input data for jumping.

    private MovementHandler movementHandler; // Reference to the MovementHandler for managing player movement input.

    private Vector3 moveDirection; // Direction vector for the player's movement.

    private bool isCrouchToggled = false; // Flag indicating if the crouch button is toggled.
    private bool wasPreviouslyGrounded = true; // Flag indicating if the player was grounded in the previous frame.
    private bool jumpTriggered = false; // Flag indicating if a jump was triggered by input.
    private bool jumpdetected = false;
    private bool isCameraActive = true; // Flag indicating if the camera is currently active.

    private float groundedTimer = 0f; // Timer to control the delay.
    private float timeOnGround = 0f;
    private float currentSpeed; // Current movement speed of the player.
    private float slopeAngle; // Angle of the slope the player is currently on.

    private RaycastHit groundRaycastHit = new(); // Data structure for ground hit detection.

    private FootstepSoundController audioController; // Controller for managing player footstep sounds.
    private PlayerAnimatorController animatorController; // Animator controller for handling player animations.
    private CameraStateManager cameraToggleController; // Controller for toggling the camera state.

    private Vector3 velocity; // Current velocity of the player.
    private Vector3 slopeNormal;

    private void InitializeInputBindings()
    {
        // Set up input bindings based on the configuration specified in MovementInputBindings.
        forwardInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.forwardInputTag);
        backInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.backInputTag);
        rightInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.rightInputTag);
        leftInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.leftInputTag);
        runInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.runInputTag);
        crouchingInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.crouchingInputTag);
        jumpInput = KeyboardTagHelper.GetInputFromTag(movementBaseControls.jumpInputTag);
    }

    private void Start()
    {
        InitializeInputBindings(); // Set up the input bindings for movement and actions.
        currentSpeed = movementSettings.movementSpeed; // Set the default movement speed.
        staminaSettings.currentStamina = staminaSettings.maxStamina; // Initialize the player's stamina to the maximum.

        // Initialize movement handler with the configured inputs.
        movementHandler = new MovementHandler(forwardInput, backInput, rightInput, leftInput);

        // Try to get references to important components and log if found.
        if (TryGetComponent(out audioController)) Debug.Log("AudioController found.");
        if (TryGetComponent(out animatorController)) Debug.Log("AnimatorController found.");
        if (TryGetComponent(out cameraToggleController)) Debug.Log("CameraToggleController found.");
    }

    private void Update()
    {
        cameraToggleController.ToggleCameraState(out isCameraActive); // Toggle the camera's activation state.

        bool detectedGrounded = true;

        if (canJump)
        {
            // Detect if the player is grounded.
            Vector3 boxCenter = movementSettings.playerTransform.position + gravitySettings.groundDetectionCenter;
            detectedGrounded = DetectionBox(boxCenter, gravitySettings.groundDetectionSize, Quaternion.identity, gravitySettings.ignoreCollisionWithTags);
        }
        
        // Update the animator based on the player's movement and interaction states.        
        if (animatorController != null)
        {
            bool isMove = (Input.GetKey(forwardInput.keyboard) || Input.GetKey(backInput.keyboard) || Input.GetKey(rightInput.keyboard) || Input.GetKey(leftInput.keyboard)) && !movementSettings.isObstacle;
            bool isRunning = runningSettings.isRunning && staminaSettings.hasStamina;
            bool isCrouching = canCrouching && crouchingSettings.isCrouching;

            animatorController.UpdateAnimatorState(isCameraActive, gravitySettings.isGrounded, canJump, isRunning, isCrouching, isMove);
        }
        if (!isCameraActive) return; // Exit early if the camera is not active.

        // Calculate movement direction based on player input.
        moveDirection = movementHandler.GetMovementInput();

        // Update obstacle detection based on crouching state.
        Vector3 playerBoxCenter = crouchingSettings.isCrouching ? movementSettings.playerTransform.position + movementSettings.playerTransform.rotation * crouchingSettings.newDetectObstaclesCenter : movementSettings.playerTransform.position + movementSettings.playerTransform.rotation * movementSettings.detectObstaclesCenter;
        Vector3 obstacleSize = crouchingSettings.isCrouching ? crouchingSettings.newDetectObstaclesSize : movementSettings.detectObstaclesSize;
        movementSettings.isObstacle = DetectionBox(playerBoxCenter, obstacleSize, movementSettings.playerTransform.rotation, movementSettings.ignoreCollisionWithTags);

        // Play footstep sounds when the player's grounded state changes.
        if (wasPreviouslyGrounded != gravitySettings.isGrounded && canJump)
        {
            wasPreviouslyGrounded = gravitySettings.isGrounded;
            if (audioController != null && detectedGrounded)
            {
                // Play footstep sound based on crouching state.
                audioController.PlayFootstepSound(crouchingSettings.isCrouching);
            }
        }

        // Handle running state based on input and stamina.
        if (canRunning)
        {
            // Start or stop running based on input and stamina.
            if (Input.GetKeyDown(runInput.keyboard) && staminaSettings.hasStamina)
            {
                runningSettings.isRunning = true; // Start running.
            }
            else if (!staminaSettings.hasStamina || Input.GetKeyUp(runInput.keyboard))
            {
                runningSettings.isRunning = false; // Stop running if stamina is depleted or the key is released.
            }
        }

        // Adjust speed based on running and crouching state.
        if (runningSettings.isRunning && staminaSettings.hasStamina)
        {
            currentSpeed = crouchingSettings.isCrouching ? crouchingSettings.crouchedRunSpeed : runningSettings.runSpeed;
        }
        else
        {
            currentSpeed = crouchingSettings.isCrouching ? crouchingSettings.crouchingSpeed : movementSettings.movementSpeed;
        }

        // Handle crouching based on input and available space.
        if (canCrouching)
        {
            // Detect space for crouching.
            Vector3 boxCenter = movementSettings.playerTransform.position + crouchingSettings.spaceDetectionCenter;
            bool isEnoughCrouchingSpace = !DetectionBox(boxCenter, crouchingSettings.spaceDetectionSize, movementSettings.playerTransform.rotation, crouchingSettings.ignoreCollisionWithTags, true);

            // Toggle crouching state when the crouch key is pressed and there's enough space.
            if (Input.GetKeyDown(crouchingInput.keyboard) && isEnoughCrouchingSpace)
            {
                isCrouchToggled = !isCrouchToggled;
            }

            // Update crouching state based on toggling, space, and if player is grounded.
            crouchingSettings.isCrouching = (isCrouchToggled || !isEnoughCrouchingSpace) && gravitySettings.isGrounded;

            // Enable/disable the appropriate collider based on crouching state.
            crouchingSettings.standingCollider.enabled = !crouchingSettings.isCrouching;
            crouchingSettings.crouchCollider.enabled = crouchingSettings.isCrouching;
        }
        else
        {
            isCrouchToggled = false;
        }

        // Handle jumping input and ground detection.
        if (canJump)
        {
            // If the character is on the ground, accumulates the time he is there.
            if (gravitySettings.isGrounded)
            {
                timeOnGround += Time.deltaTime;
            }
            else
            {
                timeOnGround = 0f;
            }

            bool pressingJumpButton = Input.GetKeyDown(jumpInput.keyboard) && !jumpTriggered && !crouchingSettings.isCrouching && timeOnGround >= 0.2f;

            if (pressingJumpButton && gravitySettings.isGrounded)
            {
                // Triggers the jump.
                jumpTriggered = true;
                gravitySettings.isGrounded = false;
                jumpdetected = true;
                groundedTimer = 0f;
            }

            // Update the skip detection status.
            if (jumpdetected)
            {
                jumpdetected = detectedGrounded;
            }

            // If the jump was not detected, check if the character touched the ground.
            if (!jumpdetected)
            {
                if (detectedGrounded)
                {
                    // Character landed.
                    gravitySettings.isGrounded = true;
                    groundedTimer = 0f;
                }
                else
                {
                    groundedTimer += Time.deltaTime; // Character is on air.

                    // Check coyote time.
                    if (groundedTimer >= gravitySettings.coyoteTime)
                    {
                        gravitySettings.isGrounded = false;
                    }
                }
            }
        }
    }

    private bool DetectionBox(Vector3 boxCenter, Vector3 boxSize, Quaternion boxOrientation, List<string> tags, bool logColliders = false)
    {
        // Perform a collision check using Physics.OverlapBox.
        #pragma warning disable UNT0028
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize / 2, boxOrientation);
        #pragma warning restore UNT0028

        foreach (Collider collider in colliders)
        {
            // Detect collision with non-player objects that aren't triggers.
            if (!collider.transform.IsChildOf(movementSettings.playerTransform) && !collider.isTrigger && !ShouldIgnoreCollision(collider, tags))
            {
                if (logColliders) Debug.Log($"Collision detected with: {collider}", collider);
                return true; // Collision detected.
            }
        }

        return false; // No collision detected.
    }

    private bool ShouldIgnoreCollision(Collider collider, List<string> tags)
    {
        // Check if the collision should be ignored based on specified tags.
        if (tags.Count > 0)
        {
            foreach (string tag in tags)
            {
                if (collider.gameObject.CompareTag(tag)) return true; // Ignore collision if tag matches.
            }
        }

        return false; // No matching tag, do not ignore collision.
    }

    private void FixedUpdate()
    {
        if (!isCameraActive) return; // Exit early if the camera is not active.

        // Handle jumping mechanics.
        if (canJump)
        {
            // Apply jump force to the player Rigidbody if the jump is triggered and the player is grounded.
            if (jumpTriggered)
            {
                float jumpForce = Mathf.Sqrt(gravitySettings.maxJumpHeight * -2f * Physics.gravity.y);
                movementSettings.playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                jumpTriggered = false; // Reset jump trigger.
            }

            // Perform ground raycast to check slope angle.
            if (Physics.Raycast(movementSettings.playerTransform.position, Vector3.down, out groundRaycastHit, 0.3f))
            {
                slopeAngle = Vector3.Angle(groundRaycastHit.normal, Vector3.up); // Calculate the slope angle.
                slopeNormal = groundRaycastHit.normal;
                Debug.Log($"Slope Angle: {slopeAngle}�");

                // Adjust friction settings based on the slope angle.
                if (slopeAngle > physicMaterialSettings.maxAngle)
                {
                    // Apply friction for steep slopes.
                    physicMaterialSettings.physicMaterial.dynamicFriction = physicMaterialSettings.outsideSlopeFriction;
                    physicMaterialSettings.physicMaterial.staticFriction = physicMaterialSettings.outsideSlopeFriction;
                }
                else
                {
                    // Apply normal friction settings for shallower slopes or flat surfaces.
                    physicMaterialSettings.physicMaterial.dynamicFriction = gravitySettings.isGrounded ? physicMaterialSettings.slopeFriction : physicMaterialSettings.frictionInAir;
                    physicMaterialSettings.physicMaterial.staticFriction = physicMaterialSettings.slopeFriction;
                }

                // Visualize ground normal for debugging.
                Debug.DrawRay(groundRaycastHit.point, groundRaycastHit.normal, Color.blue);
            }
        }

        // Apply movement velocity to the Rigidbody.
        if (canJump && slopeAngle < physicMaterialSettings.maxAngle && slopeAngle > physicMaterialSettings.minAngle)
        {
            // Ajusta a direção do movimento com base no ângulo da inclinação.
            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal).normalized;
            velocity = currentSpeed * slopeDirection;

            Debug.Log($"Angular Velocity: {slopeDirection} Normal: {slopeNormal}");
        }
        else
        {
            velocity = currentSpeed * moveDirection;
        }

        if (canJump && gravitySettings.isGrounded && jumpTriggered)
        {
            velocity.y = 0;
        }
        else
        {
            velocity.y = movementSettings.playerRigidbody.linearVelocity.y; // Preserve vertical velocity.
        }

        if (movementSettings.isObstacle)
        {
            velocity.x = 0f; velocity.z = 0f; // Stop movement if an obstacle is detected.
        }

        movementSettings.playerRigidbody.linearVelocity = velocity; // Apply calculated velocity to the player's Rigidbody.

        // Rotate the player to face the direction of movement.
        if (moveDirection != Vector3.zero)
        {
            // Calculate target rotation based on movement direction.
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

            // Smoothly rotate player towards the target direction.
            movementSettings.playerTransform.rotation = Quaternion.Lerp(movementSettings.playerTransform.rotation, targetRotation, movementSettings.rotationSpeed * Time.fixedDeltaTime);
        }

        // Check if the player is walking by verifying movement key inputs.
        bool isWalking = Input.GetKey(forwardInput.keyboard) || 
            Input.GetKey(backInput.keyboard) || 
            Input.GetKey(rightInput.keyboard) || 
            Input.GetKey(leftInput.keyboard);

        // Play footstep sound if the player is grounded and moving.
        if (wasPreviouslyGrounded && isWalking && audioController != null && !movementSettings.isObstacle)
        {
            // Update footstep sounds based on crouching and stamina.
            audioController.UpdateFootstepSounds(crouchingSettings.isCrouching, !runningSettings.isRunning || !staminaSettings.hasStamina);
        }

        // Handle running mechanics and stamina depletion if enabled.
        if (canRunning && useStamina)
        {
            // Handle stamina depletion while running.
            if (runningSettings.isRunning && staminaSettings.hasStamina && isWalking)
            {
                DepleteStamina(staminaSettings.staminaDepletionRate * Time.deltaTime); // Reduce stamina if the player is running.
            }

            // Update stamina status based on its current level.
            if (staminaSettings.currentStamina <= 0)
            {
                staminaSettings.hasStamina = false; // Player has no stamina left, stop running.
            }
            else if (staminaSettings.currentStamina > staminaSettings.minimumAmountVigor)
            {
                staminaSettings.hasStamina = true; // Player has sufficient stamina to continue running.
            }

            // Recover stamina if not running or moving.
            if (!(runningSettings.isRunning && isWalking) && staminaSettings.currentStamina < staminaSettings.maxStamina)
            {
                RegenerateStamina(staminaSettings.staminaRecoveryRate * Time.deltaTime); // Recover stamina over time.
            }
        }
    }

    public void DepleteStamina(float amount)
    {
        // Decrease current stamina by a specified amount, ensuring it doesn't drop below 0.
        staminaSettings.currentStamina -= amount;

        // Ensure stamina stays within valid bounds.
        staminaSettings.currentStamina = Mathf.Clamp(staminaSettings.currentStamina, 0f, staminaSettings.maxStamina);
    }

    public void RegenerateStamina(float amount)
    {
        // Increase the player's stamina by the given recovery rate, clamping it to the maximum stamina value.
        staminaSettings.currentStamina += amount;

        // Ensure stamina doesn't exceed the maximum.
        staminaSettings.currentStamina = Mathf.Clamp(staminaSettings.currentStamina, 0f, staminaSettings.maxStamina);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw gizmos to visualize important areas like obstacle detection, crouching space, and ground detection.
        Matrix4x4 originalMatrix = Gizmos.matrix; // Store the original gizmo matrix.
        Gizmos.color = Color.yellow;

        // Draw obstacle detection box based on crouching state.
        Vector3 playerBoxCenter = movementSettings.playerTransform.position + movementSettings.playerTransform.rotation * (crouchingSettings.isCrouching ? crouchingSettings.newDetectObstaclesCenter : movementSettings.detectObstaclesCenter);
        Gizmos.matrix = Matrix4x4.TRS(playerBoxCenter, movementSettings.playerTransform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, crouchingSettings.isCrouching ? crouchingSettings.newDetectObstaclesSize : movementSettings.detectObstaclesSize);

        Gizmos.matrix = originalMatrix;

        // Draw ground detection box if jumping is enabled.
        if (canJump)
        {
            Vector3 boxCenter = movementSettings.playerTransform.position + gravitySettings.groundDetectionCenter;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boxCenter, gravitySettings.groundDetectionSize);
        }

        // Draw crouching space detection box if crouching is enabled.
        if (canCrouching)
        {
            Vector3 boxCenter = movementSettings.playerTransform.position + crouchingSettings.spaceDetectionCenter;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, movementSettings.playerTransform.rotation, Vector3.one);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Vector3.zero, crouchingSettings.spaceDetectionSize);
            Gizmos.matrix = originalMatrix;
        }
    }
}