using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Audio;
using UnityEngine;

/// <summary>
/// Controls the playback of player footstep sounds, adapting to the player's movement state
/// (walking, running, crouching, and landing sounds).
/// Uses reflection to work with any player controller exposing the properties defined in IPlayerMovementState.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Footstep Sound Controller")]
public class FootstepSoundController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, HighlightEmptyReference] private MonoBehaviour playerController; // Player controller component, should implement the required properties.
    [SerializeField, HighlightEmptyReference] private AudioSource audioSource; // AudioSource used to play sounds.
    [SerializeField, HighlightEmptyReference] private AudioMixerGroup audioMixerGroup; // Optional audio mixer group for volume and effects control.

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.1f; // Base volume for footstep sounds.
    [Space(5)]
    [SerializeField, Range(-3, 3)] private float minimumPitch = 0.8f; // Minimum pitch for the audio.
    [SerializeField, Range(-3, 3)] private float maximumPitch = 1f; // Maximum pitch for the audio.

    [Header("Walking & Running Settings")]
    [SerializeField] private float walkInterval = 0.5f; // Time interval between footsteps when walking.
    [SerializeField] private float runInterval = 0.3f; // Time interval between footsteps when running.
    [SerializeField, HighlightEmptyReference] private List<AudioClip> walkClips; // List of footstep sounds for walking/running.

    [Header("Crouching Settings")]
    [SerializeField] private float crouchWalkInterval = 1.0f; // Interval between footsteps when crouch-walking.
    [SerializeField] private float crouchRunInterval = 0.5f; // Interval between footsteps when crouch-running.
    [SerializeField, HighlightEmptyReference] private List<AudioClip> crouchClips; // List of footstep sounds for crouching.

    [Header("Landing Sound")]
    [SerializeField] private bool playLandingSound = false; // Toggle landing sound playback on/off.
    [SerializeField, HighlightEmptyReference] private List<AudioClip> landingClips; // Possible landing sounds.

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
    /// Initializes references and settings.
    /// </summary>
    private void Awake()
    {
        // Check required references to prevent runtime errors.
        if (!playerController) Debug.LogWarning("Player Controller not assigned.", this);
        if (!audioSource) Debug.LogWarning("Audio Source not assigned.", this);

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
    /// Physics update to check and play sounds based on player state.
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
                // If landing sounds disabled, play a regular footstep sound instead.
                PlayFootstep(GetMovementState());
            }
        }

        // Update last frame grounded state.
        wasGroundedLastFrame = isGroundedNow;
    }

    /// <summary>
    /// Checks if enough time has passed to play the next footstep sound, then plays it.
    /// Prevents footstep sound if player is not grounded.
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
        var clips = (state == MovementState.CrouchWalking || state == MovementState.CrouchRunning)? crouchClips : walkClips;

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
        if (!playerState.IsMove) return MovementState.None;

        if (playerState.IsCrouching)
        {
            return playerState.IsRun ? MovementState.CrouchRunning : MovementState.CrouchWalking;
        }

        return playerState.IsRun ? MovementState.Running : MovementState.Walking;
    }

    /// <summary>
    /// Sets the current time interval between footsteps based on the movement state.
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
            _ => float.MaxValue  // Prevent footstep sounds if no movement.
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
    bool IsMove { get; }       // Is the player currently moving?
    bool IsCrouching { get; }  // Is the player crouching?
    bool IsRun { get; }        // Is the player running?
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
    public bool IsMove => target.TryGetProperty(nameof(IsMove), out bool value) && value;
    public bool IsCrouching => target.TryGetProperty(nameof(IsCrouching), out bool value) && value;
    public bool IsRun => target.TryGetProperty(nameof(IsRun), out bool value) && value;
}

/// <summary>
/// Extension methods for MonoBehaviour to simplify reflection-based property access.
/// </summary>
public static class MonoBehaviourExtensions
{
    /// <summary>
    /// Tries to get a public property value by name using reflection.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="mono">The MonoBehaviour to inspect.</param>
    /// <param name="propName">Name of the property.</param>
    /// <param name="value">Output value if property exists and is of type T.</param>
    /// <returns>True if the property was found and value retrieved; otherwise false.</returns>
    public static bool TryGetProperty<T>(this MonoBehaviour mono, string propName, out T value)
    {
        var prop = mono.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(T))
        {
            value = (T)prop.GetValue(mono);
            return true;
        }

        value = default;
        return false;
    }
}
#endregion