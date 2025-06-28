/*
 * ---------------------------------------------------------------------------
 * Description: Manages keyboard settings in Unity by allowing users to select, 
 *              reset, and save KeyCode settings through a UI interface. It handles
 *              input detection, ensures KeyCode uniqueness among multiple managers, 
 *              and persists settings using KeyboardControlData. It also provides
 *              UI updates based on user selection, supports a delay timer before
 *              accepting new inputs, and handles KeyCode conflicts across different
 *              instances of KeyboardSettingsManager in the scene.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using CustomKeyboard;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

[AddComponentMenu("UI/Custom Keyboard Settings/Keyboard Settings Manager (TMP)")]
public class TMP_KeyboardSettingsManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private bool useImage; // Determines whether to display the selected KeyCode as an image or text.
    [SerializeField] private Button selectButton; // Button to initiate the selection of a new KeyCode.
    [SerializeField] private TMP_Text selectedButtonText; // Text element displaying the currently selected KeyCode.
    [SerializeField] private Image selectImage; // Image element used to display the currently selected KeyCode as an icon (if useImage is true).
    [SerializeField] private Button resetButton; // Button to reset the KeyCode to its default value.
    [Space(5)]
    [Header("Default Settings")]
    [SerializeField] private KeyCode defaultKeyCode = KeyCode.E; // The default KeyCode to use when resetting.
    [Space(5)]
    [Header("Save Settings")]
    [SerializeField][KeyboardTagDropdown] private string keyboardTag = "DefaultTag"; // Unique tag used to associate this KeyCode with the corresponding InputData in KeyboardControlData.

    [HideInInspector] public KeyCode currentKeyCode = KeyCode.None; // The currently selected KeyCode.

    private InputData inputData; // The InputData associated with the current settings, retrieved by the keyboardTag.
    private KeyCode previousKeyCode = KeyCode.None; // Stores the KeyCode before the new selection begins, used for comparison and UI updates.
    private bool isListening = false; // Indicates whether the manager is currently listening for a new keyboard input.
    private List<TMP_KeyboardSettingsManager> otherManagers; // List of other KeyboardSettingsManager instances in the scene, used to avoid KeyCode conflicts.

    private float delayTimer = 0f; // Timer used to delay the start of listening for new keyboard inputs.
    private bool isDelaying = false; // Indicates whether a delay is in progress before listening for new input.

    private void OnSelectButtonClick()
    {
        // Begin listening for a new input after a short delay, if not already in a delay or listening state.
        if (!isDelaying && !isListening)
        {
            delayTimer = Time.realtimeSinceStartup + 0.5f; // Set a 0.5-second delay before accepting new input.
            isDelaying = true; // Mark that the system is now in a delaying state.
        }

        isListening = true; // Begin listening for a new KeyCode.

        if (!useImage)
        {
            selectedButtonText.text = $"> {previousKeyCode} <"; // Update the UI to indicate the current key selection process.
        }
        else
        {
            selectImage.sprite = KeyboardTagHelper.GetKeySprite(previousKeyCode); // Update image to reflect previous KeyCode.
        }
    }

    private void OnResetButtonClick()
    {
        // Reset the KeyCode to the default setting and save the updated settings.
        SetDefaultSettings(); // Set the KeyCode back to its default value.
        SaveSettings(); // Save the new settings.
    }

    private void Start()
    {
        // Initialize the list of other KeyboardSettingsManager instances in the scene.
        otherManagers = new List<TMP_KeyboardSettingsManager>(FindObjectsByType<TMP_KeyboardSettingsManager>(FindObjectsSortMode.None));

        // Ensure that the button click listeners are set up correctly.
        selectButton.onClick.RemoveListener(OnSelectButtonClick); // Remove any existing listeners to avoid duplicates.
        resetButton.onClick.RemoveListener(OnResetButtonClick); // Remove any existing listeners to avoid duplicates.

        selectButton.onClick.AddListener(OnSelectButtonClick); // Add listener for the select button.
        resetButton.onClick.AddListener(OnResetButtonClick); // Add listener for the reset button.

        // Attempt to load the saved settings or fall back to default settings if no saved data is found.
        inputData = KeyboardTagHelper.GetInputFromTag(keyboardTag); // Retrieve the associated InputData by tag.
        if (inputData != null)
        {
            SetSettings(); // Load and apply the saved settings.
        }
        else
        {
            SetDefaultSettings(); // If no saved settings are found, apply the default settings.
        }
    }

    private void SetSettings()
    {
        // Apply the KeyCode from the saved InputData and update the UI.
        currentKeyCode = inputData.keyboard; // Set the current KeyCode to the value stored in InputData.
        previousKeyCode = currentKeyCode; // Synchronize the previous KeyCode with the current KeyCode.
        selectedButtonText.text = previousKeyCode.ToString(); // Update the UI text to reflect the selected KeyCode.
        if (!useImage)
        {
            selectedButtonText.text = previousKeyCode.ToString(); // Update the UI text to reflect the selected KeyCode.
        }
        else
        {
            selectImage.sprite = KeyboardTagHelper.GetKeySprite(previousKeyCode); // Set the image for the current KeyCode.
        }
    }

    private void SetDefaultSettings()
    {
        // Apply the default KeyCode and update the UI.
        currentKeyCode = defaultKeyCode; // Set the current KeyCode to the default value.
        previousKeyCode = defaultKeyCode; // Synchronize the previous KeyCode with the default KeyCode.
        selectedButtonText.text = previousKeyCode.ToString(); // Update the UI text to display the default KeyCode.
        if (!useImage)
        {
            selectedButtonText.text = previousKeyCode.ToString(); // Display the default KeyCode text.
        }
        else
        {
            selectImage.sprite = KeyboardTagHelper.GetKeySprite(previousKeyCode); // Display the default KeyCode image.
        }
    }

    private void Update()
    {
        // Continuously check for new keyboard inputs if currently in listening mode.
        if (isListening)
        {
            ListenForNewInput(); // Process the new input if detected.
        }

        // Enable or disable the reset button based on whether the current KeyCode matches the default.
        resetButton.interactable = currentKeyCode != defaultKeyCode; // Allow reset only if the KeyCode has changed from the default.

        // Manage the delay timer for input detection.
        if (isDelaying)
        {
            // If the delay timer has expired, stop delaying and begin listening.
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime >= delayTimer)
            {
                isDelaying = false; // End the delay and allow input to be detected.
            }
        }

        // Set UI element visibility based on whether we use image for display.
        selectedButtonText.gameObject.SetActive(!useImage); // Show the text UI element if not using an image.
        selectImage.gameObject.SetActive(useImage); // Show the image UI element if using image.
    }

    private void ListenForNewInput()
    {
        // Iterate through all possible KeyCodes to detect a new input.
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode) && !isDelaying) // Check if a key is pressed and if not currently delaying.
            {
                // Verify that the new KeyCode is not already in use by another KeyboardSettingsManager.
                if (!IsKeyCodeUsedByOtherManagers(keyCode))
                {
                    // Apply the new KeyCode, update the UI, and save the new settings.
                    currentKeyCode = keyCode; // Set the current KeyCode to the newly detected key.
                    previousKeyCode = keyCode; // Update the previous KeyCode to match the new selection.

                    if (!useImage)
                    {
                        selectedButtonText.text = keyCode.ToString(); // Update the UI text with the new KeyCode.
                    }
                    else
                    {
                        selectImage.sprite = KeyboardTagHelper.GetKeySprite(previousKeyCode); // Update the image for the new KeyCode.
                    }

                    isListening = false; // Stop listening for further inputs.
                    SaveSettings(); // Save the updated KeyCode settings.
                }
                else
                {
                    Debug.LogWarning("KeyCode is already in use by another KeyboardSettingsManager.");
                }
            }
        }
    }

    private bool IsKeyCodeUsedByOtherManagers(KeyCode keyCode)
    {
        // Check if the provided KeyCode is already used by another manager in the scene.
        foreach (var manager in otherManagers)
        {
            if (manager != this && manager.currentKeyCode == keyCode) // Ensure it’s not the current manager instance.
            {
                return true; // Return true if the KeyCode is found to be in use by another manager.
            }
        }
        return false; // Return false if the KeyCode is not in use by any other manager.
    }

    private void SaveSettings()
    {
        // Save the current settings to the KeyboardControlData.
        if (inputData != null)
        {
            KeyboardTagHelper.SetKey(inputData, currentKeyCode); // Update the KeyCode in the associated InputData.
            KeyboardTagHelper.SaveKeyboardControlData(); // Save the changes to persistent storage.
        }
        else
        {
            Debug.LogError($"No InputData found with tag '{keyboardTag}'.");
        }
    }
}