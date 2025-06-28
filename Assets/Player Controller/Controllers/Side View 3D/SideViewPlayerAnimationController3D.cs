using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles animation states for a side-view 3D player using boolean animator parameters.
/// Ensures only one animation state is active at any given time.
/// </summary>
[AddComponentMenu("Player Controller/3D/Animation/Player Animation Controller (Side View)")]
public class SideViewPlayerAnimationController3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField, HighlightEmptyReference] private SideViewPlayerController3D playerController; // Reference to the player controller.
    [SerializeField, HighlightEmptyReference] private Animator animator; // Animator component controlling animations.

    [Header("Animator Parameters")]
    [SerializeField] private string stopped = "Idle"; // Animator bool for idle.
    [SerializeField] private string walking = "Walking"; // Animator bool for walking.
    [SerializeField] private string jumping = "Jump"; // Animator bool for jumping.
    [SerializeField] private string crouching = "Crouch Idle"; // Animator bool for idle while crouching.
    [SerializeField] private string crawling = "Crouch Walking"; // Animator bool for crawling.

    [Header("Debug (Read Only)")]
    [SerializeField, ReadOnly] private AnimationState currentState; // Current animation state (read-only in Inspector).

    private readonly Dictionary<AnimationState, int> stateHashes = new(); // Cached animator parameter hashes.

    /// <summary>
    /// Enum representing all possible player animation states.
    /// Only one should be active at a time.
    /// </summary>
    private enum AnimationState
    {
        Stopped,    // Idle on ground and not crouching.
        Walking,    // Moving on ground and not crouching.
        Jumping,    // In air and not crouching.
        Crouching,  // Crouching while idle.
        Crawling    // Crouching while moving.
    }

    /// <summary>
    /// Initializes references and caches parameter hashes.
    /// Sets initial animation state to Stopped.
    /// </summary>
    private void Awake()
    {
        if (!playerController) Debug.LogWarning("Player Controller not assigned.", this);
        if (!animator) Debug.LogWarning("Animator not assigned.", this);

        // Cache animator parameter hashes using provided names.
        stateHashes[AnimationState.Stopped] = Animator.StringToHash(stopped);
        stateHashes[AnimationState.Walking] = Animator.StringToHash(walking);
        stateHashes[AnimationState.Jumping] = Animator.StringToHash(jumping);
        stateHashes[AnimationState.Crouching] = Animator.StringToHash(crouching);
        stateHashes[AnimationState.Crawling] = Animator.StringToHash(crawling);

        currentState = AnimationState.Stopped;

        // Initialize animator by enabling only the initial state.
        foreach (var kvp in stateHashes)
        {
            animator.SetBool(kvp.Value, kvp.Key == currentState);
        }
    }

    /// <summary>
    /// Updates animation state based on player controller every frame.
    /// Only applies changes when state has changed.
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
    /// Determines the correct animation state based on movement, crouch, and grounded status.
    /// </summary>
    /// <returns>AnimationState to be applied.</returns>
    private AnimationState DetermineState()
    {
        // Player is idle on ground and not crouching.
        if (!playerController.IsMove && playerController.IsGrounded && !playerController.IsCrouching)
        {
            return AnimationState.Stopped;
        }

        // Player is moving on ground and not crouching.
        if (playerController.IsGrounded && playerController.IsMove && !playerController.IsCrouching)
        {
            return AnimationState.Walking;
        }

        // Player is in the air and not crouching.
        if (!playerController.IsGrounded && !playerController.IsCrouching)
        {
            return AnimationState.Jumping;
        }

        // Player is crouching while idle.
        if (playerController.IsGrounded && playerController.IsCrouching && !playerController.IsMove)
        {
            return AnimationState.Crouching;
        }

        // Player is crouching and moving.
        if (playerController.IsGrounded && playerController.IsCrouching && playerController.IsMove)
        {
            return AnimationState.Crawling;
        }

        return currentState; // Fallback to current state if no condition matches.
    }
}