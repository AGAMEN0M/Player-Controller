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
        private readonly Func<float> normalSpeed; // Delegate returning the player's normal movement speed.
        private readonly Func<float> fastSpeed; // Delegate returning the player's fast (running) speed.
        private readonly Func<float> maxStamina; // Delegate returning the maximum stamina the player can have.
        private readonly Func<float> minimumAmountVigor; // Delegate returning the minimum stamina required to run.
        private readonly Func<float> staminaDepletionRate; // Delegate returning the rate at which stamina is depleted while running.
        private readonly Func<float> staminaRecoveryRate; // Delegate returning the rate at which stamina regenerates when not running.
        private readonly Func<bool> isMove; // Delegate indicating whether the player is currently moving.
        
        private readonly bool useStamina; // Flag to indicate whether the stamina system is enabled.
        
        private float currentStamina; // Player's current stamina value.
        
        private bool subscribedToUpdate; // Tracks whether the update method is subscribed to the fixed update loop.
        private bool inputActive; // Flag indicating if the run input is currently being held.
        private bool hasStamina; // Indicates if the player currently has enough stamina to run.
        private bool isFast; // Indicates if the player is currently running at fast speed.

        /// <summary>
        /// Final resulting movement speed based on the player's current state.
        /// </summary>
        public float resultingVelocity;

        /// <summary>
        /// Player's stamina percentage (0 to 100), based on current and max stamina.
        /// </summary>
        public float staminaPercentage;

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
        /// <param name="staminaDepletionRate">Function that returns depletion rate. Default is 15.</param>
        /// <param name="staminaRecoveryRate">Function that returns recovery rate. Default is 3.</param>
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
            hasStamina = true;
            resultingVelocity = normalSpeed();

            useStamina = true;
            SubscribeUpdate();
        }

        /// <summary>
        /// Subscribes the internal Update method to the global fixed update loop.
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
        /// Disposes of the controller and unsubscribes it from update loop.
        /// </summary>
        /// <param name="controller">Reference to the controller to dispose.</param>
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

        /// <summary>
        /// Attempts to activate fast movement (running), depending on stamina availability.
        /// </summary>
        public void StartRunning()
        {
            inputActive = true;

            if (useStamina && hasStamina)
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
            inputActive = false;
            isFast = false;
            resultingVelocity = normalSpeed();
        }

        /// <summary>
        /// Updates stamina logic every fixed frame (depletion or recovery),
        /// and recalculates resulting velocity based on state.
        /// </summary>
        private void Update()
        {
            if (!useStamina) return;

            float max = maxStamina();
            float deltaTime = Time.fixedDeltaTime;

            // --- Deplete stamina if running and moving ---
            if (isFast && hasStamina && isMove())
            {
                currentStamina -= staminaDepletionRate() * deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, max);

                // If stamina fully depleted, stop running.
                if (currentStamina <= 0f)
                {
                    hasStamina = false;
                    isFast = false;
                    resultingVelocity = normalSpeed();
                    return;
                }
            }

            // --- Regenerate stamina if not running or not moving ---
            if ((!isFast || !isMove()) && currentStamina < max)
            {
                currentStamina += staminaRecoveryRate() * deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, max);

                // If enough stamina is recovered to run again
                if (!hasStamina && currentStamina > minimumAmountVigor())
                {
                    hasStamina = true;

                    // If run input is still active, resume fast movement.
                    if (inputActive)
                    {
                        isFast = true;
                        resultingVelocity = fastSpeed();
                    }
                }
            }

            // --- Update stamina percentage for UI or logic feedback ---
            staminaPercentage = (currentStamina / max) * 100f;
        }
    }
}