/*
 * ---------------------------------------------------------------------------
 * Description: Manages player's movement speed and stamina system with running and stamina depletion/regeneration.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;
using System;

using static PlayerController.PhysicsRuntime.PhysicsUpdateBroadcaster;

namespace PlayerController.Abilities
{
    /// <summary>
    /// Manages the player's movement speed and stamina system.
    /// Switches between normal and fast movement depending on stamina,
    /// and automatically handles stamina depletion and regeneration.
    /// </summary>
    public class PlayerSpeedControl
    {
        #region === Delegates ===

        // Delegates returning configuration values or state information.
        private readonly Func<float> normalSpeed;            // Returns the player's normal movement speed.
        private readonly Func<float> fastSpeed;              // Returns the player's fast (running) speed.
        private readonly Func<float> maxStamina;             // Returns the maximum stamina the player can have.
        private readonly Func<float> minimumAmountVigor;     // Returns the minimum stamina required to run.
        private readonly Func<float> staminaDepletionRate;   // Returns the rate at which stamina depletes when running.
        private readonly Func<float> staminaRecoveryRate;    // Returns the rate at which stamina regenerates when not running.
        private readonly Func<bool> isMove;                  // Returns whether the player is currently moving.

        #endregion

        #region === Private Fields ===

        private readonly bool useStamina; // Indicates whether the stamina system is enabled.
        private float currentStamina; // Current stamina value of the player.
        private bool subscribedToUpdate; // Tracks subscription to the fixed update event.
        private bool isFast;             // Whether the player is currently running.
        private bool haveEnoughStamina;  // Whether the player currently has enough stamina to run.

        #endregion

        #region === Public Fields ===

        /// <summary>
        /// Final resulting movement speed based on the player's current state.
        /// </summary>
        public float resultingVelocity;

        /// <summary>
        /// Indicates if the player is currently running and has stamina.
        /// </summary>
        public bool hasStamina;

        /// <summary>
        /// Player's stamina percentage (0 to 100) based on current and max stamina.
        /// </summary>
        public float staminaPercentage;

        #endregion

        #region === Constructors ===

        /// <summary>
        /// Constructor for speed control without stamina system.
        /// </summary>
        /// <param name="normalSpeed">Function that returns normal speed.</param>
        /// <param name="fastSpeed">Function that returns fast speed.</param>
        public PlayerSpeedControl(Func<float> normalSpeed, Func<float> fastSpeed)
        {
            this.normalSpeed = normalSpeed;
            this.fastSpeed = fastSpeed;

            resultingVelocity = this.normalSpeed();
            useStamina = false;
        }

        /// <summary>
        /// Constructor for stamina-based speed control.
        /// </summary>
        /// <param name="normalSpeed">Function that returns normal speed.</param>
        /// <param name="fastSpeed">Function that returns fast speed.</param>
        /// <param name="isMove">Function that returns whether the player is moving.</param>
        /// <param name="maxStamina">Function that returns max stamina. Default is 50.</param>
        /// <param name="minimumAmountVigor">Function that returns minimum stamina required to run. Default is 12.5.</param>
        /// <param name="staminaDepletionRate">Function that returns stamina depletion rate. Default is 15.</param>
        /// <param name="staminaRecoveryRate">Function that returns stamina recovery rate. Default is 3.</param>
        public PlayerSpeedControl(
            Func<float> normalSpeed,
            Func<float> fastSpeed,
            Func<bool> isMove,
            Func<float> maxStamina = null,
            Func<float> minimumAmountVigor = null,
            Func<float> staminaDepletionRate = null,
            Func<float> staminaRecoveryRate = null)
        {
            this.normalSpeed = normalSpeed;
            this.fastSpeed = fastSpeed;
            this.isMove = isMove;
            this.maxStamina = maxStamina ?? (() => 50f);
            this.minimumAmountVigor = minimumAmountVigor ?? (() => 12.5f);
            this.staminaDepletionRate = staminaDepletionRate ?? (() => 15f);
            this.staminaRecoveryRate = staminaRecoveryRate ?? (() => 3f);

            currentStamina = this.maxStamina();
            haveEnoughStamina = true;
            resultingVelocity = normalSpeed();

            useStamina = true;
            SubscribeUpdate();
        }

        #endregion

        #region === Subscriptions ===

        /// <summary>
        /// Subscribes the Update method to the fixed update event.
        /// </summary>
        private void SubscribeUpdate()
        {
            if (!subscribedToUpdate)
            {
                OnFixedUpdate += Update;
                subscribedToUpdate = true;
            }
        }

        /// <summary>
        /// Disposes the controller, unsubscribing from the update loop.
        /// </summary>
        /// <param name="controller">Reference to the controller instance to dispose.</param>
        public static void Dispose(ref PlayerSpeedControl controller)
        {
            if (controller == null) return;

            if (controller.subscribedToUpdate)
            {
                OnFixedUpdate -= controller.Update;
                controller.subscribedToUpdate = false;
            }

            controller = null;
        }

        #endregion

        #region === Movement Methods ===

        /// <summary>
        /// Attempts to start running if stamina permits.
        /// </summary>
        public void StartRunning()
        {
            if (useStamina && haveEnoughStamina)
            {
                isFast = true;
                resultingVelocity = fastSpeed();
            }
            else
            {
                isFast = false;
                resultingVelocity = normalSpeed();
            }

            if (!useStamina) resultingVelocity = fastSpeed();
        }

        /// <summary>
        /// Stops running and resets speed to normal.
        /// </summary>
        public void StopRunning()
        {
            isFast = false;
            resultingVelocity = normalSpeed();
        }

        #endregion

        #region === Update Logic ===

        /// <summary>
        /// Updates stamina depletion, regeneration, and resulting velocity each fixed frame.
        /// </summary>
        private void Update()
        {
            if (!useStamina) return;

            float max = maxStamina();
            float deltaTime = Time.fixedDeltaTime;

            // Deplete stamina if running and moving.
            if (isFast && haveEnoughStamina && isMove())
            {
                currentStamina -= staminaDepletionRate() * deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, max);

                // If stamina fully depleted, stop running and reset velocity.
                if (currentStamina <= 0f)
                {
                    haveEnoughStamina = false;
                    hasStamina = false;
                    isFast = false;
                    resultingVelocity = normalSpeed();
                    staminaPercentage = 0f;
                    return;
                }
            }

            // Regenerate stamina if not running or not moving.
            if ((!isFast || !isMove()) && currentStamina < max)
            {
                currentStamina += staminaRecoveryRate() * deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, max);

                // Mark stamina as sufficient again if threshold reached.
                if (!haveEnoughStamina && currentStamina > minimumAmountVigor())
                {
                    haveEnoughStamina = true;
                }
            }

            // Update running state based on velocity and stamina.
            hasStamina = resultingVelocity == fastSpeed() && haveEnoughStamina;

            // Update stamina percentage for UI or logic.
            staminaPercentage = (currentStamina / max) * 100f;
        }

        #endregion
    }
}