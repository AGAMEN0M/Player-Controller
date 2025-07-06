/*
 * ---------------------------------------------------------------------------
 * Description: Handles animation transitions for a 3D side-view player using 
 *              boolean parameters in Unity's Animator. Ensures visual consistency 
 *              by activating only one animation state at a time based on the 
 *              player's grounded, movement, and crouch status.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls animation transitions for a 3D side-view player using Animator booleans.
/// Ensures only one animation state is active at a time based on player movement and state.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Player Animation Controller [3D]")]
public class PlayerAnimationController3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ValidateReference] private MonoBehaviour playerController; // Reference to the player controller.
    [SerializeField, ValidateReference] private Animator animator; // Animator component used to control animation states.

    [Header("Animator Parameters")]
    [SerializeField] private string stopped = "Idle"; // Boolean parameter for idle state.
    [SerializeField] private string walking = "Walking"; // Boolean parameter for walking state.
    [SerializeField] private string running = "Running"; // Boolean parameter for running state.
    [SerializeField] private string jumping = "Jump"; // Boolean parameter for jumping state.
    [SerializeField] private string crouching = "Crouch Idle"; // Boolean parameter for idle while crouched.
    [SerializeField] private string crawling = "Crouch Walking"; // Boolean parameter for walking while crouched.
    [SerializeField] private string crawlingRunning = "Crouch Running"; // Boolean parameter for running while crouched.

    [Header("Debug (Read Only)")]
    [SerializeField, ReadOnlyInInspector] private AnimationState currentState; // Current active animation state.

    private readonly Dictionary<AnimationState, int> stateHashes = new(); // Cached hashes of Animator parameters.
    private IPlayerMovementState playerState; // Interface to access player movement and status.

    /// <summary>
    /// Enumeration of all valid animation states.
    /// Only one should be active at any given time.
    /// </summary>
    private enum AnimationState
    {
        Stopped,        // Idle on ground and not crouching.
        Walking,        // Walking on ground and not crouching.
        Running,        // Running on ground and not crouching.
        Jumping,        // In air and not crouching.
        Crouching,      // Idle while crouching.
        Crawling,       // Walking while crouching.
        CrawlingRunning // Running while crouching.
    }

    /// <summary>
    /// Initializes references and caches animator parameter hashes.
    /// Sets the initial animation state to Stopped.
    /// </summary>
    private void Awake()
    {
        if (!playerController) Debug.LogWarning("Player Controller not assigned.", this);
        if (!animator) Debug.LogWarning("Animator not assigned.", this);

        // Create adapter to access movement state via reflection.
        playerState = new PlayerMovementStateAdapter(playerController);
        if (playerState == null) Debug.LogWarning("playerController does not implement IPlayerMovementState.", this);

        // Cache parameter hashes for all animation states.
        stateHashes[AnimationState.Stopped] = Animator.StringToHash(stopped);
        stateHashes[AnimationState.Walking] = Animator.StringToHash(walking);
        stateHashes[AnimationState.Running] = Animator.StringToHash(running);
        stateHashes[AnimationState.Jumping] = Animator.StringToHash(jumping);
        stateHashes[AnimationState.Crouching] = Animator.StringToHash(crouching);
        stateHashes[AnimationState.Crawling] = Animator.StringToHash(crawling);
        stateHashes[AnimationState.CrawlingRunning] = Animator.StringToHash(crawlingRunning);

        currentState = AnimationState.Stopped;

        // Set initial animation state.
        foreach (var kvp in stateHashes)
        {
            animator.SetBool(kvp.Value, kvp.Key == currentState);
        }
    }

    /// <summary>
    /// Updates the animation state based on the current movement state of the player.
    /// Ensures state transition only occurs when the state changes.
    /// </summary>
    private void Update()
    {
        if (!playerController || !animator) return;

        AnimationState newState = DetermineState(); // Get the current desired state.

        // Transition only if the new state differs from the current.
        if (newState != currentState)
        {
            animator.SetBool(stateHashes[currentState], false); // Deactivate current state.
            animator.SetBool(stateHashes[newState], true); // Activate new state.
            currentState = newState; // Update cached state.
        }
    }

    /// <summary>
    /// Determines the correct animation state based on the player's movement and status.
    /// </summary>
    /// <returns>The AnimationState to apply to the Animator.</returns>
    private AnimationState DetermineState()
    {
        // Idle on ground and not crouching.
        if (!playerState.IsMoving && playerState.IsGrounded && !playerState.IsCrouching)
        {
            return AnimationState.Stopped;
        }

        // Moving on ground and not crouching.
        if (playerState.IsGrounded && playerState.IsMoving && !playerState.IsCrouching)
        {
            return playerState.IsRunning ? AnimationState.Running : AnimationState.Walking;
        }

        // In air and not crouching.
        if (!playerState.IsGrounded && !playerState.IsCrouching)
        {
            return AnimationState.Jumping;
        }

        // Crouching while idle.
        if (playerState.IsGrounded && playerState.IsCrouching && !playerState.IsMoving)
        {
            return AnimationState.Crouching;
        }

        // Crouching while moving.
        if (playerState.IsGrounded && playerState.IsCrouching && playerState.IsMoving)
        {
            return playerState.IsRunning ? AnimationState.CrawlingRunning : AnimationState.Crawling;
        }

        // Return current state as fallback.
        return currentState;
    }
}