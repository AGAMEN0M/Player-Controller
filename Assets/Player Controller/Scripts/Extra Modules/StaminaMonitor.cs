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

using PlayerController.Attributes;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace PlayerController
{
    /// <summary>
    /// Displays and updates the player's stamina UI using reflection.
    /// Reads stamina percentage from any component exposing a public 'StaminaPercent' property,
    /// updating the slider and text display accordingly.
    /// </summary>
    [AddComponentMenu("Tools/Player Controller/Extra Modules/Stamina Monitor")]
    public class StaminaMonitor : MonoBehaviour
    {
        #region === Serialized Fields ===

        [Header("Player Controller Reference")]
        [SerializeField, ValidateReference, Tooltip("MonoBehaviour that exposes a public 'StaminaPercent' property.")]
        private MonoBehaviour playerController;

        [Header("Stamina Threshold Settings")]
        [SerializeField, Tooltip("Maximum stamina value used for normalization.")]
        private float maxStamina = 50f;

        [SerializeField, Tooltip("Minimum stamina required to run, used as a low threshold.")]
        private float minStaminaForRun = 12.5f;

        [Header("UI Slider Reference")]
        [SerializeField, ValidateReference(false), Tooltip("UI Slider that displays current stamina percentage.")]
        private Slider staminaSlider;

        [Space(5)]

        [SerializeField, Tooltip("Color used when stamina is completely empty.")]
        private Color emptyStaminaColor = Color.red;

        [SerializeField, Tooltip("Color used when stamina is low but not empty.")]
        private Color lowStaminaColor = Color.yellow;

        [SerializeField, Tooltip("Color used when stamina is at normal levels.")]
        private Color normalStaminaColor = Color.green;

        [Header("UI Text Reference")]
        [SerializeField, ValidateReference(false), Tooltip("Optional text displaying the stamina percentage.")]
        private TMP_Text staminaText;

        #endregion

        #region === Private Fields ===

        private Image sliderFillImage; // Reference to the fill area image of the slider.
        private float minimumStaminaColorThreshold; // Normalized threshold for color change.
        private IPlayerStaminaInfo playerStamina; // Interface used to access stamina value via reflection.

        #endregion

        #region === Public Properties ===

        /// <summary>
        /// Gets or sets the player controller reference.
        /// </summary>
        public MonoBehaviour PlayerController
        {
            get => playerController;
            set => playerController = value;
        }

        /// <summary>
        /// Gets or sets the maximum stamina value.
        /// </summary>
        public float MaxStamina
        {
            get => maxStamina;
            set => maxStamina = value;
        }

        /// <summary>
        /// Gets or sets the minimum stamina required to run.
        /// </summary>
        public float MinStaminaForRun
        {
            get => minStaminaForRun;
            set => minStaminaForRun = value;
        }

        /// <summary>
        /// Gets or sets the stamina UI slider reference.
        /// </summary>
        public Slider StaminaSlider
        {
            get => staminaSlider;
            set => staminaSlider = value;
        }

        /// <summary>
        /// Gets or sets the color used when stamina is empty.
        /// </summary>
        public Color EmptyStaminaColor
        {
            get => emptyStaminaColor;
            set => emptyStaminaColor = value;
        }

        /// <summary>
        /// Gets or sets the color used when stamina is low.
        /// </summary>
        public Color LowStaminaColor
        {
            get => lowStaminaColor;
            set => lowStaminaColor = value;
        }

        /// <summary>
        /// Gets or sets the color used when stamina is normal.
        /// </summary>
        public Color NormalStaminaColor
        {
            get => normalStaminaColor;
            set => normalStaminaColor = value;
        }

        /// <summary>
        /// Gets or sets the text element that displays stamina percentage.
        /// </summary>
        public TMP_Text StaminaText
        {
            get => staminaText;
            set => staminaText = value;
        }

        #endregion

        #region === Unity Lifecycle Methods ===

        /// <summary>
        /// Initializes stamina thresholds, slider configuration, and reflection adapter.
        /// </summary>
        private void Start()
        {
            // Compute color change threshold as percentage.
            minimumStaminaColorThreshold = (minStaminaForRun / maxStamina) * 100f;

            // Configure slider range and cache fill image reference.
            if (staminaSlider != null)
            {
                staminaSlider.minValue = 0;
                staminaSlider.maxValue = 100;
                sliderFillImage = staminaSlider.fillRect.GetComponent<Image>();
            }

            // Initialize reflection adapter if player controller is assigned.
            if (playerController != null)
            {
                playerStamina = new PlayerStaminaAdapter(playerController);
            }
            else
            {
                Debug.LogError("StaminaMonitor: Player Controller is not assigned.", this);
            }
        }

        /// <summary>
        /// Continuously updates the stamina UI each frame.
        /// </summary>
        private void Update()
        {
            // Skip update if no stamina source is available.
            if (playerStamina == null) return;

            float stamina = playerStamina.StaminaPercent;

            // Update slider value and adjust color based on current stamina.
            if (staminaSlider != null)
            {
                staminaSlider.value = stamina;

                if (stamina <= 0f)
                {
                    sliderFillImage.color = emptyStaminaColor; // Empty.
                }
                else if (sliderFillImage.color != emptyStaminaColor && stamina <= minimumStaminaColorThreshold)
                {
                    sliderFillImage.color = lowStaminaColor; // Low.
                }
                else if (stamina > minimumStaminaColorThreshold)
                {
                    sliderFillImage.color = normalStaminaColor; // Normal.
                }
            }

            // Update text if assigned.
            if (staminaText != null)
            {
                staminaText.text = stamina % 1 == 0 ? $"{stamina:0}%" : $"{stamina:F1}%";
            }
        }

        #endregion
    }

    #region Helpers (Interfaces & Reflection)

    /// <summary>
    /// Defines the stamina information required by the StaminaMonitor.
    /// </summary>
    public interface IPlayerStaminaInfo
    {
        float StaminaPercent { get; } // Current stamina percentage (0–100).
    }

    /// <summary>
    /// Adapter that uses reflection to access 'StaminaPercent' from any MonoBehaviour.
    /// </summary>
    public class PlayerStaminaAdapter : IPlayerStaminaInfo
    {
        private readonly MonoBehaviour target;

        /// <summary>
        /// Creates a reflection-based adapter for a given MonoBehaviour.
        /// </summary>
        /// <param name="target">MonoBehaviour exposing 'StaminaPercent'.</param>
        public PlayerStaminaAdapter(MonoBehaviour target)
        {
            this.target = target;
        }

        /// <summary>
        /// Retrieves the stamina percentage value using reflection.
        /// Returns 0 if not found or invalid.
        /// </summary>
        public float StaminaPercent => target.TryGetProperty(nameof(StaminaPercent), out float value) ? value : 0f;
    }

    #endregion
}