/*
 * ---------------------------------------------------------------------------
 * Description: Displays and updates the player's stamina UI (slider and optional text)
 *              based on a reflected stamina percentage property from any player controller component.
 *              Changes slider color according to stamina thresholds for empty, low, and normal stamina.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays and updates the player's stamina UI using a generic MonoBehaviour reference.
/// Uses reflection to read stamina percentage from any component that exposes a public 'StaminaPercent' property.
/// </summary>
[AddComponentMenu("Player Controller/Extra Modules/Stamina Monitor")]
public class StaminaMonitor : MonoBehaviour
{
    [Header("Player Controller Reference")]
    [SerializeField, ValidateReference] private MonoBehaviour playerController; // Player controller reference. Must have a public float StaminaPercent property.

    [Header("Stamina Threshold Settings")]
    [SerializeField] private float maxStamina = 50f; // Maximum stamina value used for normalization.
    [SerializeField] private float minStaminaForRun = 12.5f; // Minimum stamina required to run. Used to determine color threshold.

    [Header("UI Slider Reference")]
    [SerializeField, ValidateReference(false)] private Slider staminaSlider; // Reference to the UI Slider showing stamina percentage.
    [Space(5)]
    [SerializeField] private Color emptyStaminaColor = Color.red; // Color when stamina reaches 0%.
    [SerializeField] private Color lowStaminaColor = Color.yellow; // Color when stamina is low but above zero.
    [SerializeField] private Color normalStaminaColor = Color.green; // Color when stamina is at a healthy level.

    [Header("UI Text Reference")]
    [SerializeField, ValidateReference(false)] private TMP_Text staminaText; // Optional text display of stamina percentage.

    private Image sliderFillImage; // Reference to the fill image of the slider.
    private float minimumStaminaColorThreshold; // Threshold below which the slider changes to yellow.
    private IPlayerStaminaInfo playerStamina; // Interface-based adapter to access stamina value via reflection.

    /// <summary>
    /// Initializes stamina thresholds, UI components, and adapter for stamina access.
    /// </summary>
    private void Start()
    {
        // Calculate the percentage threshold below which stamina is considered low.
        minimumStaminaColorThreshold = (minStaminaForRun / maxStamina) * 100f;

        // Setup slider min/max values and get reference to fill image.
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0;
            staminaSlider.maxValue = 100;
            sliderFillImage = staminaSlider.fillRect.GetComponent<Image>();
        }

        // Instantiate the adapter using the reflected stamina property from the target controller.
        if (playerController != null)
        {
            playerStamina = new PlayerStaminaAdapter(playerController);
        }
        else
        {
            Debug.LogError("Player Controller not assigned.", this);
        }
    }

    /// <summary>
    /// Updates the stamina UI slider and text based on the current stamina percentage.
    /// </summary>
    private void Update()
    {
        // Skip if the player controller or stamina adapter is not available.
        if (playerStamina == null) return;

        // Retrieve current stamina percentage from adapter.
        float stamina = playerStamina.StaminaPercent;

        // Update slider value and color.
        if (staminaSlider != null)
        {
            staminaSlider.value = stamina;

            if (stamina <= 0f)
            {
                // Change color to red if stamina is empty.
                sliderFillImage.color = emptyStaminaColor;
            }
            else if (sliderFillImage.color != emptyStaminaColor && stamina <= minimumStaminaColorThreshold)
            {
                // Change color to yellow if stamina is low but not empty.
                sliderFillImage.color = lowStaminaColor;
            }
            else if (stamina > minimumStaminaColorThreshold)
            {
                // Change color to green if stamina is in normal range.
                sliderFillImage.color = normalStaminaColor;
            }
        }

        // Update text label with formatted stamina percentage.
        if (staminaText != null)
        {
            staminaText.text = stamina % 1 == 0 ? $"{stamina:0}%" : $"{stamina:F1}%";
        }
    }
}

#region Helpers (Interfaces & Reflection)

/// <summary>
/// Interface defining required stamina property for use with StaminaMonitor.
/// </summary>
public interface IPlayerStaminaInfo
{
    float StaminaPercent { get; } // Current stamina percentage (0–100).
}

/// <summary>
/// Adapter using reflection to access the stamina property from any MonoBehaviour.
/// </summary>
public class PlayerStaminaAdapter : IPlayerStaminaInfo
{
    private readonly MonoBehaviour target;

    /// <summary>
    /// Constructor that receives the MonoBehaviour reference to extract stamina data.
    /// </summary>
    /// <param name="target">MonoBehaviour that exposes a public StaminaPercent property.</param>
    public PlayerStaminaAdapter(MonoBehaviour target)
    {
        this.target = target;
    }

    /// <summary>
    /// Gets the current stamina percentage using reflection.
    /// Returns 0 if the property is not found or not valid.
    /// </summary>
    public float StaminaPercent => target.TryGetProperty(nameof(StaminaPercent), out float value) ? value : 0f;
}

#endregion