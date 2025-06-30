using UnityEngine;
using System;

namespace PlayerController.PhysicsRuntime
{
    /// <summary>
    /// Controls movement for a Rigidbody-based player character using XZ input direction.
    /// Includes configurable speed and optional obstacle blocking logic.
    /// </summary>
    public class PlayerMoveController
    {
        private readonly Func<Rigidbody> targetRigidbody; // Provides the Rigidbody to apply movement to.
        private readonly Func<float> speed;               // Provides the movement speed multiplier.
        private readonly Func<bool> isObstacle;           // If true, prevents movement from being applied (e.g., hitting a wall).

        /// <summary>
        /// Constructs a movement controller with optional speed and obstacle logic.
        /// </summary>
        /// <param name="targetRigidbody">Function that provides the target Rigidbody.</param>
        /// <param name="speed">Function that returns movement speed.</param>
        /// <param name="isObstacle">Function that returns whether movement should be blocked.</param>
        public PlayerMoveController(Func<Rigidbody> targetRigidbody, Func<float> speed = default, Func<bool> isObstacle = default)
        {
            this.targetRigidbody = targetRigidbody;
            this.speed = speed ?? (() => 3f);
            this.isObstacle = isObstacle ?? (() => false);
        }

        /// <summary>
        /// Disposes the controller by clearing its reference.
        /// </summary>
        /// <param name="controller">Reference to this controller to nullify.</param>
        public static void Dispose(ref PlayerMoveController controller)
        {
            if (controller != null)
            {
                controller = null;
            }
        }

        /// <summary>
        /// Applies movement to the Rigidbody in the XZ plane.
        /// </summary>
        /// <param name="directionXZ">Normalized movement direction (X,Z).</param>
        public void OnMove(Vector2 directionXZ)
        {
            // Block movement if obstacle is active.
            if (isObstacle?.Invoke() == true)
            {
                OnStop();
                return;
            }

            // Retrieve the target Rigidbody.
            var rb = targetRigidbody.Invoke();
            if (rb == null) return;

            float moveSpeed = speed.Invoke(); // Get the movement speed.

            Vector3 currentVelocity = rb.linearVelocity; // Preserve current vertical (Y) velocity.
            Vector2 moveVector = directionXZ.normalized * moveSpeed; // Calculate the desired movement in the XZ plane.

            // Apply movement while maintaining vertical velocity.
            rb.linearVelocity = new Vector3(moveVector.x, currentVelocity.y, moveVector.y);
        }

        /// <summary>
        /// Stops all horizontal movement while preserving vertical velocity.
        /// </summary>
        public void OnStop()
        {
            var rb = targetRigidbody.Invoke();
            if (rb == null) return;

            Vector3 currentVelocity = rb.linearVelocity;

            // Reset only horizontal (X and Z) velocity.
            rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }
    }
}