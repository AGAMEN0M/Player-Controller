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
    #region === Enum ===

    /// <summary>
    /// Enumeration of all valid animation states.
    /// Only one should be active at any given time.
    /// </summary>
    private enum AnimationState
    {
        /// <summary>Idle on ground and not crouching.</summary>
        Stopped,
        /// <summary>Walking on ground and not crouching.</summary>
        Walking,
        /// <summary>Running on ground and not crouching.</summary>
        Running,
        /// <summary>In air and not crouching.</summary>
        Jumping,
        /// <summary>Idle while crouching.</summary>
        Crouching,
        /// <summary>Walking while crouching.</summary>
        Crawling,
        /// <summary>Running while crouching.</summary>
        CrawlingRunning
    }

    #endregion

    #region === Inspector Fields ===

    [Header("References")]
    [SerializeField, ValidateReference, Tooltip("Reference to the player controller script that provides movement and state data.")]
    private MonoBehaviour playerController; // Reference to the player controller component.

    [SerializeField, ValidateReference, Tooltip("Reference to the Animator component controlling the player's animations.")]
    private Animator animator; // Animator component used to control animation states.

    [Header("Animator Parameters")]
    [SerializeField, Tooltip("Animator parameter name for the idle state.")]
    private string stopped = "Idle"; // Boolean parameter for idle state.

    [SerializeField, Tooltip("Animator parameter name for the walking state.")]
    private string walking = "Walking"; // Boolean parameter for walking state.

    [SerializeField, Tooltip("Animator parameter name for the running state.")]
    private string running = "Running"; // Boolean parameter for running state.

    [SerializeField, Tooltip("Animator parameter name for the jumping state.")]
    private string jumping = "Jump"; // Boolean parameter for jumping state.

    [SerializeField, Tooltip("Animator parameter name for the idle crouching state.")]
    private string crouching = "Crouch Idle"; // Boolean parameter for idle while crouched.

    [SerializeField, Tooltip("Animator parameter name for the crouch walking state.")]
    private string crawling = "Crouch Walking"; // Boolean parameter for walking while crouched.

    [SerializeField, Tooltip("Animator parameter name for the crouch running state.")]
    private string crawlingRunning = "Crouch Running"; // Boolean parameter for running while crouched.

    [Header("Debug (Read Only)")]
    [SerializeField, ReadOnlyInInspector, Tooltip("Currently active animation state, updated at runtime.")]
    private AnimationState currentState; // Current active animation state.

    #endregion

    #region === Private Fields ===

    private readonly Dictionary<AnimationState, int> stateHashes = new(); // Cached hashes of Animator parameters.
    private IPlayerMovementState playerState; // Interface to access player movement and status.

    #endregion

    #region === Properties ===

    /// <summary>
    /// Gets or sets the player controller script that provides movement and state information.
    /// </summary>
    public MonoBehaviour PlayerController
    {
        get => playerController;
        set => playerController = value;
    }

    /// <summary>
    /// Gets or sets the Animator component responsible for controlling player animations.
    /// </summary>
    public Animator Animator
    {
        get => animator;
        set => animator = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the idle state.
    /// </summary>
    public string StoppedTag
    {
        get => stopped;
        set => stopped = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the walking state.
    /// </summary>
    public string WalkingTag
    {
        get => walking;
        set => walking = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the running state.
    /// </summary>
    public string RunningTag
    {
        get => running;
        set => running = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the jumping state.
    /// </summary>
    public string JumpingTag
    {
        get => jumping;
        set => jumping = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the idle crouching state.
    /// </summary>
    public string CrouchingTag
    {
        get => crouching;
        set => crouching = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the crouch walking state.
    /// </summary>
    public string CrawlingTag
    {
        get => crawling;
        set => crawling = value;
    }

    /// <summary>
    /// Gets or sets the Animator parameter name used for the crouch running state.
    /// </summary>
    public string CrawlingRunningTag
    {
        get => crawlingRunning;
        set => crawlingRunning = value;
    }

    #endregion

    #region === Unity Methods ===

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

        var newState = DetermineState(); // Get the current desired state.

        // Transition only if the new state differs from the current.
        if (newState != currentState)
        {
            animator.SetBool(stateHashes[currentState], false); // Deactivate current state.
            animator.SetBool(stateHashes[newState], true); // Activate new state.
            currentState = newState; // Update cached state.
        }
    }

    #endregion

    #region === Helper Methods ===

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

    #endregion
}