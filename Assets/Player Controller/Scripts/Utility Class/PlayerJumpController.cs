/*
 * ---------------------------------------------------------------------------
 * Description: Controls Rigidbody jump logic with multi-jump, coyote time, and cooldown after jump.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;
using System;

using static PlayerController.PhysicsRuntime.PhysicsUpdateBroadcaster;

namespace PlayerController.PhysicsRuntime
{
    /// <summary>
    /// Controls the jump logic for a Rigidbody-based character,
    /// supporting multiple jumps, coyote time (grace period after leaving the ground)
    /// and cooldown to avoid wrong ground detection right after the jump.
    /// </summary>
    public class PlayerJumpController
    {
        private readonly Func<Rigidbody> targetRigidbody; // Function that provides the target Rigidbody to apply jump force.
        private readonly Func<bool> isGrounded;           // Function that indicates whether the character is on the ground.
        private readonly Func<int> maxJumps;              // Function that returns the maximum number of jumps allowed.
        private readonly Func<float> coyoteTime;          // Function that returns the allowed coyote time (in seconds).

        private int jumpCount;              // Number of jumps used since the last contact with the ground.
        private float lastGroundedTime;     // Time (fixedTime) of the last time the character touched the ground.
        private bool wasGroundedLastFrame;  // Grounded state in the previous frame to detect transitions.
        private float jumpCooldownTime;     // Time until which we ignore grounded after jumping (to avoid erroneous collisions).

        private const float groundedIgnoreDurationAfterJump = 0.1f; // Cooldown duration for ignoring grounded after jumping.

        private bool coyoteJumpUsed;        // Indicates whether the coyote's jump has already been consumed.
        private bool isInCoyoteWindow;      // Indicates whether we are still within the coyote time window.

        /// <summary>
        /// Initializes the jump controller with mandatory and optional functions.
        /// </summary>
        /// <param name="targetRigidbody">Function that returns the target Rigidbody.</param>
        /// <param name="isGrounded">Function that returns whether it is on the ground.</param>
        /// <param name="maxJumps">Function that returns the maximum number of jumps allowed (default 1).</param>
        /// <param name="coyoteTime">Function that returns the coyote time (default 0).</param>
        public PlayerJumpController(Func<Rigidbody> targetRigidbody, Func<bool> isGrounded, Func<int> maxJumps = null, Func<float> coyoteTime = null)
        {
            this.targetRigidbody = targetRigidbody ?? throw new ArgumentNullException(nameof(targetRigidbody));
            this.isGrounded = isGrounded ?? throw new ArgumentNullException(nameof(isGrounded));
            this.maxJumps = maxJumps ?? (() => 1);
            this.coyoteTime = coyoteTime ?? (() => 0f);

            OnFixedUpdate += Update;
        }

        /// <summary>
        /// Unregisters the controller from the FixedUpdate event and resets it.
        /// </summary>
        /// <param name="controller">Reference to the controller to be discarded.</param>
        public static void Dispose(ref PlayerJumpController controller)
        {
            if (controller != null)
            {
                OnFixedUpdate -= controller.Update;
                controller = null;
            }
        }

        /// <summary>
        /// Updates the controller state with each FixedUpdate.
        /// Responsible for updating the grounded, controlling the coyote time window and resetting the jump counter.
        /// </summary>
        private void Update()
        {
            bool groundedNow = isGrounded.Invoke();

            // Ignores grounded immediately after jumping to avoid detecting collision with ceiling or floor incorrectly.
            if (Time.fixedTime < jumpCooldownTime)
            {
                groundedNow = false;
            }

            // Detects landing: resets the jump counter and allows the coyote to jump again.
            if (groundedNow && !wasGroundedLastFrame)
            {
                jumpCount = 0;
                coyoteJumpUsed = false;
                lastGroundedTime = Time.fixedTime;
            }

            // Detects ground exit: starts coyote timing and releases coyote jump.
            if (!groundedNow && wasGroundedLastFrame)
            {
                lastGroundedTime = Time.fixedTime;
                coyoteJumpUsed = false;
            }

            wasGroundedLastFrame = groundedNow;

            // Calculates whether we are still within the coyote time window.
            float allowedCoyote = Mathf.Max(0f, coyoteTime.Invoke());
            isInCoyoteWindow = (Time.fixedTime - lastGroundedTime) <= allowedCoyote;

            // If the coyote's timer is up, the character is not on the ground, has not used the coyote jump
            // and has not yet jumped, consumes the jump to prevent infinite jumps off the ground.
            if (!isInCoyoteWindow && !groundedNow && !coyoteJumpUsed && jumpCount == 0)
            {
                jumpCount++;
                coyoteJumpUsed = true;
            }
        }

        /// <summary>
        /// Attempts to perform the jump by applying force to the Rigidbody, respecting the maximum jumps and coyote time rules.
        /// </summary>
        /// <param name="jumpForce">Force applied to the jump (default 5f).</param>
        /// <returns>True if the jump was performed, false otherwise.</returns>
        public bool OnJump(float jumpForce = 5f)
        {
            if (targetRigidbody == null || targetRigidbody.Invoke() == null) return false;

            int maxAllowedJumps = Mathf.Max(1, maxJumps.Invoke());
            bool groundedNow = isGrounded.Invoke();
            bool canJump = false;

            // You can jump if you are on the ground.
            if (groundedNow)
            {
                canJump = true;
            }
            // You can jump if you are in the coyote time window and have not used coyote jump.
            else if (isInCoyoteWindow && !coyoteJumpUsed && jumpCount == 0)
            {
                canJump = true;
            }
            // You can jump if you still have jumps available (e.g. double jump).
            else if (jumpCount < maxAllowedJumps)
            {
                canJump = true;
            }

            if (!canJump) return false;

            var rb = targetRigidbody.Invoke();

            // Resets the vertical speed so the jump is consistent.
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);

            // Applies impulse force for jumping.
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // Ignores grounded for a short period after jumping to avoid incorrect detection.
            jumpCooldownTime = Time.fixedTime + groundedIgnoreDurationAfterJump;

            // Marks the coyote jump as used, if applicable.
            if (isInCoyoteWindow && !coyoteJumpUsed && jumpCount == 0)
            {
                coyoteJumpUsed = true;
            }

            jumpCount++;

            return true;
        }
    }
}