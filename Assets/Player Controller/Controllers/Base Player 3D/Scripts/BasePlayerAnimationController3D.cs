/*
 * ---------------------------------------------------------------------------
 * Description: Controls animation state transitions for a side-view 3D player 
 *              using boolean parameters in Unity's Animator. Syncs animations with player 
 *              actions like walking, running, jumping, crouching, and crawling, ensuring 
 *              exclusive state activation for consistent visual feedback.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles animation states for a side-view 3D player using boolean parameters in the Animator.
/// Ensures only one animation state is active at a time.
/// </summary>
[AddComponentMenu("Player Controller/3D/Animation/Player Animation Controller (Base)")]
public class BasePlayerAnimationController3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ValidateReference] private BasePlayer3D playerController; // Reference to the player controller.
    [SerializeField, ValidateReference] private Animator animator; // Animator component controlling animations.

    [Header("Animator Parameters")]
    [SerializeField] private string stopped = "Idle"; // Animator bool for idle state.
    [SerializeField] private string walking = "Walking"; // Animator bool for walking state.
    [SerializeField] private string running = "Running"; // Animator bool for running state.
    [SerializeField] private string jumping = "Jump"; // Animator bool for jumping state.
    [SerializeField] private string crouching = "Crouch Idle"; // Animator bool for idle while crouching.
    [SerializeField] private string crawling = "Crouch Walking"; // Animator bool for crawling state.
    [SerializeField] private string crawlingRunning = "Crouch Running"; // Animator bool for crouch-running state.

    [Header("Debug (Read Only)")]
    [SerializeField, ReadOnlyInInspector] private AnimationState currentState; // Current animation state (read-only in Inspector).

    private readonly Dictionary<AnimationState, int> stateHashes = new(); // Cached animator parameter hashes.

    /// <summary>
    /// Enum representing all possible player animation states.
    /// Only one should be active at a time.
    /// </summary>
    private enum AnimationState
    {
        Stopped,            // Idle on ground and not crouching.
        Walking,            // Moving on ground and not crouching.
        Running,            // Running on ground and not crouching.
        Jumping,            // In air and not crouching.
        Crouching,          // Crouching while idle.
        Crawling,           // Crouching while walking.
        CrawlingRunning     // Crouching while running.
    }

    /// <summary>
    /// Initializes references and caches animator parameter hashes.
    /// Sets the initial animation state to Stopped.
    /// </summary>
    private void Awake()
    {
        if (!playerController) Debug.LogWarning("Player Controller not assigned.", this);
        if (!animator) Debug.LogWarning("Animator not assigned.", this);

        // Cache animator parameter hashes using provided names.
        stateHashes[AnimationState.Stopped] = Animator.StringToHash(stopped);
        stateHashes[AnimationState.Walking] = Animator.StringToHash(walking);
        stateHashes[AnimationState.Running] = Animator.StringToHash(running);
        stateHashes[AnimationState.Jumping] = Animator.StringToHash(jumping);
        stateHashes[AnimationState.Crouching] = Animator.StringToHash(crouching);
        stateHashes[AnimationState.Crawling] = Animator.StringToHash(crawling);
        stateHashes[AnimationState.CrawlingRunning] = Animator.StringToHash(crawlingRunning);

        currentState = AnimationState.Stopped;

        // Initialize animator by enabling only the initial state.
        foreach (var kvp in stateHashes)
        {
            animator.SetBool(kvp.Value, kvp.Key == currentState);
        }
    }

    /// <summary>
    /// Updates the animation state every frame based on player controller status.
    /// Only changes the animation when the state has changed.
    /// </summary>
    private void Update()
    {
        if (!playerController || !animator) return;

        AnimationState newState = DetermineState(); // Evaluate current animation state.

        // Change animation only if new state is different.
        if (newState != currentState)
        {
            animator.SetBool(stateHashes[currentState], false); // Disable previous animation state.
            animator.SetBool(stateHashes[newState], true); // Enable new animation state.
            currentState = newState; // Cache new current state.
        }
    }

    /// <summary>
    /// Determines the appropriate animation state based on movement, crouching, and grounded status.
    /// </summary>
    /// <returns>The AnimationState that should be applied.</returns>
    private AnimationState DetermineState()
    {
        // Player is idle on ground and not crouching.
        if (!playerController.IsMoving && playerController.IsGrounded && !playerController.IsCrouching)
        {
            return AnimationState.Stopped;
        }

        // Player is moving on ground and not crouching (walking or running).
        if (playerController.IsGrounded && playerController.IsMoving && !playerController.IsCrouching)
        {
            return playerController.IsRunning ? AnimationState.Running : AnimationState.Walking;
        }

        // Player is in the air and not crouching.
        if (!playerController.IsGrounded && !playerController.IsCrouching)
        {
            return AnimationState.Jumping;
        }

        // Player is crouching while idle.
        if (playerController.IsGrounded && playerController.IsCrouching && !playerController.IsMoving)
        {
            return AnimationState.Crouching;
        }

        // Player is crouching and moving (walking or running).
        if (playerController.IsGrounded && playerController.IsCrouching && playerController.IsMoving)
        {
            return playerController.IsRunning ? AnimationState.CrawlingRunning : AnimationState.Crawling;
        }

        return currentState; // Fallback to current state if no condition matches.
    }
}