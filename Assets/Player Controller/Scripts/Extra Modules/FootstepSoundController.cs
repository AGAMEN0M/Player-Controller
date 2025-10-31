/*
 * ---------------------------------------------------------------------------
 * Description: Handles playback of footstep and landing sounds based on the player's 
 *              current movement state. Dynamically adapts timing and clip selection 
 *              using reflection to read movement properties from any controller 
 *              implementing the required interface.
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
    #region === Enumerations ===

    /// <summary>
    /// Enum representing possible movement states for controlling footstep sounds.
    /// </summary>
    public enum MovementState
    {
        /// <summary>Default state when player is not moving.</summary>
        None,

        /// <summary>Player is walking normally.</summary>
        Walking,

        /// <summary>Player is running.</summary>
        Running,

        /// <summary>Player is walking while crouched.</summary>
        CrouchWalking,

        /// <summary>Player is running while crouched.</summary>
        CrouchRunning
    }

    #endregion

    #region === Serialized Fields ===

    [Header("References")]
    [SerializeField, ValidateReference, Tooltip("Reference to the MonoBehaviour component that controls player movement. This object must expose the properties defined in IPlayerMovementState for reflection-based access.")]
    private MonoBehaviour playerController; // Player controller component. Should implement the required properties.

    [SerializeField, ValidateReference, Tooltip("AudioSource component responsible for playing footstep and landing sounds. It will be automatically configured on Awake.")]
    private AudioSource audioSource; // AudioSource used to play sounds.

    [SerializeField, ValidateReference(false), Tooltip("Optional AudioMixerGroup reference for routing the AudioSource output. Used to control volume and apply effects globally through Unity's Audio Mixer.")]
    private AudioMixerGroup audioMixerGroup; // Optional audio mixer group for volume and effects control.

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("Base volume multiplier for all footstep and landing sounds played by this controller.")]
    private float footstepVolume = 0.1f; // Base volume for footstep sounds.

    [Space(5)]

    [SerializeField, Range(-3, 3), Tooltip("Minimum random pitch variation applied when playing a sound. Used to create natural variation between repeated footsteps.")]
    private float minimumPitch = 0.8f; // Minimum pitch for the audio.

    [SerializeField, Range(-3, 3), Tooltip("Maximum random pitch variation applied when playing a sound. Used to create natural variation between repeated footsteps.")]
    private float maximumPitch = 1f; // Maximum pitch for the audio.

    [Header("Walking & Running Settings")]
    [SerializeField, Tooltip("Time interval (in seconds) between footsteps when the player is walking normally.")]
    private float walkInterval = 0.6f; // Time interval between footsteps when walking.

    [SerializeField, Tooltip("Time interval (in seconds) between footsteps when the player is running.")]
    private float runInterval = 0.3f; // Time interval between footsteps when running.

    [SerializeField, ValidateReference, Tooltip("List of AudioClips that will be randomly selected and played when the player walks or runs.")]
    private List<AudioClip> walkClips; // List of footstep sounds for walking/running.

    [Header("Crouching Settings")]
    [SerializeField, Tooltip("Time interval (in seconds) between footsteps when the player is walking while crouched.")]
    private float crouchWalkInterval = 1.0f; // Interval between footsteps when crouch-walking.

    [SerializeField, Tooltip("Time interval (in seconds) between footsteps when the player is running while crouched.")]
    private float crouchRunInterval = 0.5f; // Interval between footsteps when crouch-running.

    [SerializeField, ValidateReference, Tooltip("List of AudioClips used for footsteps when the player is crouching (both walking and running).")]
    private List<AudioClip> crouchClips; // List of footstep sounds for crouching.

    [Header("Landing Sound")]
    [SerializeField, Tooltip("If enabled, a landing sound will play when the player returns to the ground after falling or jumping.")]
    private bool playLandingSound = false; // Toggle landing sound playback on/off.

    [SerializeField, ValidateReference, Tooltip("List of AudioClips that can be used for landing sounds. A random clip will be chosen when landing occurs.")]
    private List<AudioClip> landingClips; // Possible landing sounds.

    #endregion

    #region === Private Fields ===

    private IPlayerMovementState playerState; // Interface representing the player's movement state.
    private float currentInterval; // Current interval between footstep sounds based on movement state.
    private float lastFootstepTime; // The time when the last footstep sound was played.
    private bool wasGroundedLastFrame = true; // Tracks whether the player was grounded last frame to detect changes.

    #endregion

    #region === Properties ===

    /// <summary>
    /// Reference to the player's movement controller implementing IPlayerMovementState.
    /// </summary>
    public MonoBehaviour PlayerController
    {
        get => playerController;
        set => playerController = value;
    }

    /// <summary>
    /// Reference to the AudioSource component responsible for sound playback.
    /// </summary>
    public AudioSource AudioSource
    {
        get => audioSource;
        set => audioSource = value;
    }

    /// <summary>
    /// Optional AudioMixerGroup used for routing this AudioSource's output.
    /// </summary>
    public AudioMixerGroup AudioMixerGroup
    {
        get => audioMixerGroup;
        set => audioMixerGroup = value;
    }

    /// <summary>
    /// Base playback volume for footstep and landing sounds.
    /// </summary>
    public float FootstepVolume
    {
        get => footstepVolume;
        set => footstepVolume = value;
    }

    /// <summary>
    /// Minimum pitch applied when randomizing sound playback.
    /// </summary>
    public float MinimumPitch
    {
        get => minimumPitch;
        set => minimumPitch = value;
    }

    /// <summary>
    /// Maximum pitch applied when randomizing sound playback.
    /// </summary>
    public float MaximumPitch
    {
        get => maximumPitch;
        set => maximumPitch = value;
    }

    /// <summary>
    /// Interval between footsteps when walking.
    /// </summary>
    public float WalkInterval
    {
        get => walkInterval;
        set => walkInterval = value;
    }

    /// <summary>
    /// Interval between footsteps when running.
    /// </summary>
    public float RunInterval
    {
        get => runInterval;
        set => runInterval = value;
    }

    /// <summary>
    /// List of AudioClips for walking and running footsteps.
    /// </summary>
    public List<AudioClip> WalkClips
    {
        get => walkClips;
        set => walkClips = value;
    }

    /// <summary>
    /// Interval between footsteps when walking while crouched.
    /// </summary>
    public float CrouchWalkInterval
    {
        get => crouchWalkInterval;
        set => crouchWalkInterval = value;
    }

    /// <summary>
    /// Interval between footsteps when running while crouched.
    /// </summary>
    public float CrouchRunInterval
    {
        get => crouchRunInterval;
        set => crouchRunInterval = value;
    }

    /// <summary>
    /// List of AudioClips for crouching footsteps.
    /// </summary>
    public List<AudioClip> CrouchClips
    {
        get => crouchClips;
        set => crouchClips = value;
    }

    /// <summary>
    /// Determines whether landing sounds are played when touching the ground.
    /// </summary>
    public bool PlayLandingSound
    {
        get => playLandingSound;
        set => playLandingSound = value;
    }

    /// <summary>
    /// List of AudioClips that can be used for landing sounds.
    /// </summary>
    public List<AudioClip> LandingClips
    {
        get => landingClips;
        set => landingClips = value;
    }

    #endregion

    #region === Unity Lifecycle Methods ===

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

        // Validate pitch configuration to ensure consistency.
        if (minimumPitch > maximumPitch) Debug.LogError("Minimum pitch cannot be greater than maximum pitch.", this);

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

        if (playerState.IsCrouching) return playerState.IsRunning ? MovementState.CrouchRunning : MovementState.CrouchWalking;

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