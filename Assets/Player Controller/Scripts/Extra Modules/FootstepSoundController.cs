using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Player/Extra Modules/Footstep Sound Controller")]
public class FootstepSoundController : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField][HighlightEmptyReference] private AudioMixerGroup audioMixerGroup; // Audio mixer group for controlling the sound output.
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.1f; // Volume level for footstep sounds, ranging from 0 (mute) to 1 (full volume).

    [Header("Movement Settings")]
    [SerializeField] private float walkInterval = 0.5f; // Time interval between footstep sounds when walking.
    [SerializeField] private float runInterval = 0.3f; // Time interval between footstep sounds when running.
    [SerializeField][HighlightEmptyReference] private List<AudioClip> walkFootstepClips; // List of audio clips for footsteps during normal movement.

    [Header("Crouching Settings")]
    [SerializeField] private float crouchWalkInterval = 1f; // Time interval between footstep sounds when walking while crouching.
    [SerializeField] private float crouchRunInterval = 0.5f; // Time interval between footstep sounds when running while crouching.
    [SerializeField][HighlightEmptyReference] private List<AudioClip> crouchFootstepClips; // List of audio clips for footsteps while crouching.

    private AudioSource audioSource; // Reference to the AudioSource component attached to this GameObject.
    private float lastFootstepTime; // Stores the time when the last walking footstep sound was played.
    private float lastRunFootstepTime; // Stores the time when the last running footstep sound was played.

    private void Start()
    {
        // Try to get the AudioSource component attached to the GameObject, log an error if not found.
        if (!TryGetComponent(out audioSource))
        {
            Debug.LogError("AudioSource component is missing.", this);
            return;
        }

        // Set the volume level of the AudioSource.
        audioSource.volume = footstepVolume;

        // If an AudioMixerGroup is assigned, set it to the AudioSource.
        if (audioMixerGroup != null) audioSource.outputAudioMixerGroup = audioMixerGroup;

        // Initialize the footstep timers to negative values to ensure an immediate sound on start.
        lastFootstepTime = -walkInterval; // Ensures the first walk footstep sound plays instantly.
        lastRunFootstepTime = -runInterval; // Ensures the first run footstep sound plays instantly.
    }

    // Call this method every frame to update footstep sounds based on player's movement.
    public void UpdateFootstepSounds(bool isCrouching, bool isRunning)
    {
        // Set the current time interval between sounds based on whether the player is crouching or running.
        float currentInterval = isCrouching
            ? (isRunning ? crouchWalkInterval : crouchRunInterval) // Use crouch intervals if crouching.
            : (!isRunning ? runInterval : walkInterval); // Use normal walking/running intervals if not crouching.

        // Choose which timestamp to check: last run or last walk footstep time.
        float lastPlayTime = !isRunning ? lastRunFootstepTime : lastFootstepTime;

        // If enough time has passed since the last footstep, play the next footstep sound.
        if (Time.time - lastPlayTime >= currentInterval)
        {
            PlayFootstepSound(isCrouching); // Play a sound from the appropriate footstep clip list.

            // Update the appropriate timestamp based on movement type.
            (isRunning ? ref lastFootstepTime : ref lastRunFootstepTime) = Time.time;
        }
    }

    // Plays a random footstep sound from the correct clip list based on whether the player is crouching.
    public void PlayFootstepSound(bool isCrouching)
    {
        // Select the appropriate list of footstep clips based on crouching state.
        List<AudioClip> clips = isCrouching ? crouchFootstepClips : walkFootstepClips;

        // If the clip list is not empty, play a random sound from the list.
        if (clips.Count > 0)
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Count)]; // Select a random clip from the list.
            if (randomClip != null) audioSource.PlayOneShot(randomClip); // Play the selected clip if it's valid.
        }
    }
}