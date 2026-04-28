/*
 * ---------------------------------------------------------------------------
 * Description: Utility class to handle timed action blocking.
 *              Can be used to prevent an action (like crouch, attack, jump)
 *              from being triggered for a short duration.
 * 
 * Usage Example:
 *     ActionBlockTimer crouchBlock = new ActionBlockTimer(0.15f);
 *     ...
 *     crouchBlock.Activate();     // Blocks the action for 0.15s.
 *     ...
 *     crouchBlock.Update();       // Call this every frame.
 *     if (crouchBlock.IsBlocked)  // Check if still blocked.
 *         return;
 *         
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;
using System;

namespace PlayerController.InputEvents
{
    [Serializable]
    public class ActionBlockTimer
    {
        #region === Properties ===

        /// <summary>
        /// Indicates if the action is currently blocked.
        /// </summary>
        public bool IsBlocked { get; private set; }

        #endregion

        #region === Fields ===

        // Remaining time until the block expires.
        private float timer;

        // Duration of the block when activated.
        private readonly float duration;

        #endregion

        #region === Constructors ===

        /// <summary>
        /// Initializes a new instance of the ActionBlockTimer with a given duration.
        /// </summary>
        public ActionBlockTimer(float duration)
        {
            this.duration = duration;
            IsBlocked = false;
            timer = 0f;
        }

        #endregion

        #region === Public Methods ===

        /// <summary>
        /// Activates the block for the specified duration.
        /// </summary>
        public void Activate()
        {
            IsBlocked = true;
            timer = duration;
        }

        /// <summary>
        /// Updates the block timer, should be called once per frame.
        /// </summary>
        public void Update()
        {
            if (!IsBlocked) return;

            // Decrease the timer by deltaTime.
            timer -= Time.deltaTime;

            // When timer reaches zero, unblock the action.
            if (timer <= 0f) IsBlocked = false;
        }

        /// <summary>
        /// Forces the block to end immediately.
        /// </summary>
        public void Reset()
        {
            IsBlocked = false;
            timer = 0f;
        }

        #endregion
    }
}