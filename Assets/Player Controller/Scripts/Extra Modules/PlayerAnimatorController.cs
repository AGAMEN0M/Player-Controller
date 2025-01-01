using CustomKeyboard;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Player/Extra Modules/Animator Controller")]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Animator Settings")]
    [HighlightEmptyReference] public Animator playerAnimator; // Reference to the player animator.
    [SerializeField] private string animationParameterName = "Animation State"; // Name of the parameter for animation state in Animator.
    [Space(5)]
    [SerializeField] private int idleAnimationID = 0; // ID for the idle (stop) animation.
    [SerializeField] private int walkAnimationID = 1; // ID for the walking animation.
    [SerializeField] private int runAnimationID = 2; // ID for the running animation.
    [SerializeField] private int jumpAnimationID = 3; // ID for the jumping animation.
    [SerializeField] private int crouchIdleAnimationID = 4; // ID for the crouch idle (stop) animation.
    [SerializeField] private int crouchWalkAnimationID = 5; // ID for the crouch walking animation.
    [SerializeField] private int crouchRunAnimationID = 6; // ID for the crouch running animation.
    [Space(5)]
    [SerializeField] private List<CustomAnimation> customAnimations; // List of custom animations with their triggering input.

    private void Start()
    {
        // Initialize input data for each custom animation based on input tags.
        foreach (var customAnim in customAnimations)
        {
            customAnim.inputData = KeyboardTagHelper.GetInputFromTag(customAnim.inputTag);
        }
    }

    public void UpdateAnimatorState(bool isAnimationActive, bool isOnGround, bool canPerformJump, bool isRunning, bool isCrouching, bool isMove)
    {
        // Set animator speed based on whether animations are active.
        playerAnimator.speed = isAnimationActive ? 1 : 0;
        if (isAnimationActive)
        {
            // Update animation state based on player’s current state.
            UpdateAnimationState(isOnGround, canPerformJump, isRunning, isCrouching, isMove);
        }
    }

    private void UpdateAnimationState(bool isOnGround, bool canPerformJump, bool isRunning, bool isCrouching, bool isMove)
    {
        int currentAnimationID = 0; // Default to the idle animation ID.

        // Check if the player is on the ground.
        if (isOnGround)
        {
            // Determines the animation based on the movement state and the crouched state.
            if (!isMove)
            {
                // Player is not moving; select idle animation based on crouching state.
                currentAnimationID = isCrouching ? crouchIdleAnimationID : idleAnimationID;
            }
            else
            {
                // Player is moving; select walking or running animation based on running state.
                currentAnimationID = isRunning ? (isCrouching ? crouchRunAnimationID : runAnimationID) : (isCrouching ? crouchWalkAnimationID : walkAnimationID);
            }
        }
        else if (canPerformJump)
        {
            // Player is in the air and can jump; select jump animation.
            currentAnimationID = jumpAnimationID;
        }

        // Check for custom animations based on input tags.
        foreach (CustomAnimation customAnim in customAnimations)
        {
            if (Input.GetKey(customAnim.inputData.keyboard))
            {
                // Override animation ID with custom animation ID if corresponding key is pressed.
                currentAnimationID = customAnim.animationID;
                break;
            }
        }

        // Apply the determined animation state to the Animator.
        playerAnimator.SetInteger(animationParameterName, currentAnimationID);
    }

    [System.Serializable]
    public class CustomAnimation
    {
        public int animationID; // ID of the custom animation.
        [KeyboardTagDropdown] public string inputTag; // Input tag used to trigger the custom animation.
        [HideInInspector] public InputData inputData; // Input data corresponding to the input tag.
    }
}