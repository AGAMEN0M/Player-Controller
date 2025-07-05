/*
 * ---------------------------------------------------------------------------
 * Description: Controls footstep and landing sounds for the player, adapting 
 *              audio playback to the player's movement state (walking, running, crouching, 
 *              and landing). Uses reflection to access movement state properties from any 
 *              player controller implementing the expected interface.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

/// <summary>
/// Controls the playback of player footstep sounds, adapting to the player's movement state
/// (walking, running, crouching, and landing).
/// Uses reflection to work with any player controller exposing the properties defined in IPlayerMovementState.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Footstep Sound Controller")]
public class FootstepSoundController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ValidateReference] private MonoBehaviour playerController; // Player controller component. Should implement the required properties.
    [SerializeField, ValidateReference] private AudioSource audioSource; // AudioSource used to play sounds.
    [SerializeField, ValidateReference(false)] private AudioMixerGroup audioMixerGroup; // Optional audio mixer group for volume and effects control.

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.1f; // Base volume for footstep sounds.
    [Space(5)]
    [SerializeField, Range(-3, 3)] private float minimumPitch = 0.8f; // Minimum pitch for the audio.
    [SerializeField, Range(-3, 3)] private float maximumPitch = 1f; // Maximum pitch for the audio.

    [Header("Walking & Running Settings")]
    [SerializeField] private float walkInterval = 0.6f; // Time interval between footsteps when walking.
    [SerializeField] private float runInterval = 0.3f; // Time interval between footsteps when running.
    [SerializeField, ValidateReference] private List<AudioClip> walkClips; // List of footstep sounds for walking/running.

    [Header("Crouching Settings")]
    [SerializeField] private float crouchWalkInterval = 1.0f; // Interval between footsteps when crouch-walking.
    [SerializeField] private float crouchRunInterval = 0.5f; // Interval between footsteps when crouch-running.
    [SerializeField, ValidateReference] private List<AudioClip> crouchClips; // List of footstep sounds for crouching.

    [Header("Landing Sound")]
    [SerializeField] private bool playLandingSound = false; // Toggle landing sound playback on/off.
    [SerializeField, ValidateReference] private List<AudioClip> landingClips; // Possible landing sounds.

    private IPlayerMovementState playerState; // Interface representing the player's movement state.
    private float currentInterval; // Current interval between footstep sounds based on movement state.
    private float lastFootstepTime; // The time when the last footstep sound was played.
    private bool wasGroundedLastFrame = true; // Tracks whether the player was grounded last frame to detect changes.

    /// <summary>
    /// Enum representing possible movement states for controlling footstep sounds.
    /// </summary>
    public enum MovementState
    {
        None,
        Walking,
        Running,
        CrouchWalking,
        CrouchRunning
    }

    #region Unity Lifecycle Methods

    /// <summary>
    /// Sets up references and audio configuration for footstep playback.
    /// </summary>
    private void Awake()
    {
        // Check required references to prevent runtime errors.
        if (!playerController) Debug.LogError("Player Controller not assigned.", this);
        if (!audioSource) Debug.LogError("Audio Source not assigned.", this);

        // Create an adapter that uses reflection to access properties from playerController.
        playerState = new PlayerMovementStateAdapter(playerController);
        if (playerState == null) Debug.LogWarning("playerController does not implement IPlayerMovementState.", this);

        // Configure the AudioSource to not play on awake and set volume.
        audioSource.playOnAwake = false;
        audioSource.volume = footstepVolume;

        // Assign audio mixer group if specified.
        if (audioMixerGroup != null) audioSource.outputAudioMixerGroup = audioMixerGroup;

        // Initialize lastFootstepTime so the first step plays immediately.
        lastFootstepTime = -Mathf.Max(walkInterval, crouchWalkInterval);
    }

    /// <summary>
    /// Performs physics-based update to manage footstep and landing sounds.
    /// </summary>
    private void FixedUpdate()
    {
        if (playerState == null) return; // Do nothing if player state is missing.

        HandleLandingSound(); // Check for landing and play landing sound if appropriate.

        var state = GetMovementState(); // Get current movement state.
        UpdateFootsteps(state); // Update footstep sound timing and playback.
    }

    #endregion

    #region Footstep Sound Logic

    /// <summary>
    /// Detects changes in the grounded state and plays landing sound if enabled.
    /// </summary>
    private void HandleLandingSound()
    {
        bool isGroundedNow = playerState.IsGrounded;

        // Detect a change in grounded state (landed or left ground).
        if (wasGroundedLastFrame != isGroundedNow)
        {
            if (playLandingSound && landingClips?.Count > 0)
            {
                // Play a random landing sound.
                var clip = landingClips[Random.Range(0, landingClips.Count)];
                PlayFootstepClipWithPitch(clip);
            }
            else if (!playLandingSound)
            {
                // If landing sounds are disabled, play a regular footstep sound instead.
                PlayFootstep(GetMovementState());
            }
        }

        // Update last frame grounded state.
        wasGroundedLastFrame = isGroundedNow;
    }

    /// <summary>
    /// Checks if enough time has passed to play the next footstep sound, then plays it.
    /// Skips footstep sounds if the player is not grounded.
    /// </summary>
    /// <param name="state">Current movement state of the player.</param>
    private void UpdateFootsteps(MovementState state)
    {
        SetCurrentInterval(state); // Update the interval based on the current state.

        // Do not play footstep if not grounded or not moving.
        if (!playerState.IsGrounded || state == MovementState.None) return;

        // Play a footstep sound if the required interval has passed.
        if (Time.fixedTime - lastFootstepTime >= currentInterval)
        {
            PlayFootstep(state);
            lastFootstepTime = Time.fixedTime;
        }
    }

    /// <summary>
    /// Plays a random footstep sound clip appropriate to the movement state.
    /// </summary>
    /// <param name="state">Current movement state.</param>
    private void PlayFootstep(MovementState state)
    {
        var clips = (state == MovementState.CrouchWalking || state == MovementState.CrouchRunning) ? crouchClips : walkClips;

        if (clips == null || clips.Count == 0) return; // No clips available to play.

        var clip = clips[Random.Range(0, clips.Count)];
        PlayFootstepClipWithPitch(clip);
    }

    /// <summary>
    /// Plays the given audio clip using the AudioSource with a randomized pitch
    /// between the configured minimum and maximum values.
    /// This is used to add variation to footstep or landing sounds.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    private void PlayFootstepClipWithPitch(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.pitch = Random.Range(minimumPitch, maximumPitch); // Randomize pitch to avoid repetitive sounds.
            audioSource.PlayOneShot(clip); // Play the clip once through the AudioSource.
        }
    }

    #endregion

    #region State Evaluation

    /// <summary>
    /// Determines the current movement state based on player state flags.
    /// </summary>
    /// <returns>The movement state used for sound playback.</returns>
    private MovementState GetMovementState()
    {
        if (!playerState.IsMoving) return MovementState.None;

        if (playerState.IsCrouching)
        {
            return playerState.IsRunning ? MovementState.CrouchRunning : MovementState.CrouchWalking;
        }

        return playerState.IsRunning ? MovementState.Running : MovementState.Walking;
    }

    /// <summary>
    /// Updates the time interval between footsteps according to the current movement state.
    /// </summary>
    /// <param name="state">Current movement state.</param>
    private void SetCurrentInterval(MovementState state)
    {
        currentInterval = state switch
        {
            MovementState.Walking => walkInterval,
            MovementState.Running => runInterval,
            MovementState.CrouchWalking => crouchWalkInterval,
            MovementState.CrouchRunning => crouchRunInterval,
            _ => float.MaxValue // Prevent footstep sounds if no movement.
        };
    }

    #endregion
}

#region Helpers (Interfaces & Reflection)

/// <summary>
/// Interface defining properties required to retrieve the player's movement state.
/// </summary>
public interface IPlayerMovementState
{
    bool IsGrounded { get; }   // Is the player currently on the ground?
    bool IsMoving { get; }     // Is the player currently moving?
    bool IsCrouching { get; }  // Is the player crouching?
    bool IsRunning { get; }    // Is the player running?
}

/// <summary>
/// Adapter using reflection to access the required properties from any player controller.
/// </summary>
public class PlayerMovementStateAdapter : IPlayerMovementState
{
    private readonly MonoBehaviour target;

    public PlayerMovementStateAdapter(MonoBehaviour target)
    {
        this.target = target;
    }

    // Uses extension method to safely get property values from the target MonoBehaviour.
    public bool IsGrounded => target.TryGetProperty(nameof(IsGrounded), out bool value) && value;
    public bool IsMoving => target.TryGetProperty(nameof(IsMoving), out bool value) && value;
    public bool IsCrouching => target.TryGetProperty(nameof(IsCrouching), out bool value) && value;
    public bool IsRunning => target.TryGetProperty(nameof(IsRunning), out bool value) && value;
}

#endregion