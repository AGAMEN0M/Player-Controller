using UnityEngine;
using System;

using static PlayerController.PhysicsRuntime.PhysicsUpdateBroadcaster;

namespace PlayerController.TransformRuntime
{
    /// <summary>
    /// Controls smooth rotation of a Transform based on directional input in the XZ plane.
    /// Applies a Lerp toward the movement direction each fixed frame.
    /// </summary>
    public class PlayerRotationController
    {
        private readonly Func<Transform> targetTransform; // Provides the Transform to apply rotation to.
        private readonly Func<Vector2> direction;         // Provides the normalized 2D movement direction (X, Y → X, Z).
        private readonly Func<float> speed;               // Provides the rotation speed multiplier.
        private readonly Func<float> magnitude;           // Minimum magnitude required to trigger rotation.

        /// <summary>
        /// Creates a rotation controller that rotates a Transform based on 2D directional input.
        /// </summary>
        /// <param name="targetTransform">Function that returns the Transform to rotate.</param>
        /// <param name="direction">Function that returns the 2D direction vector.</param>
        /// <param name="speed">Optional rotation speed (default: 10f).</param>
        /// <param name="magnitude">Optional min magnitude to trigger rotation (default: 0.01f).</param>
        public PlayerRotationController(Func<Transform> targetTransform, Func<Vector2> direction, Func<float> speed = default, Func<float> magnitude = default)
        {
            this.targetTransform = targetTransform;
            this.direction = direction;
            this.speed = speed ?? (() => 10f);
            this.magnitude = magnitude ?? (() => 0.01f);

            OnFixedUpdate += UpdateRotation;
        }

        /// <summary>
        /// Disposes the controller and unregisters from FixedUpdate.
        /// </summary>
        /// <param name="controller">Reference to this controller to nullify.</param>
        public static void Dispose(ref PlayerRotationController controller)
        {
            if (controller != null)
            {
                OnFixedUpdate -= controller.UpdateRotation;
                controller = null;
            }
        }

        /// <summary>Rotates the target transform toward the input direction on the XZ plane.</summary>
        private void UpdateRotation()
        {
            Vector2 dir2D = direction.Invoke();

            if (dir2D.sqrMagnitude > magnitude.Invoke())
            {
                // Convert Vector2 to Vector3 in XZ plane
                Vector3 direction3D = new(dir2D.x, 0f, dir2D.y);

                // Calculate rotation toward that direction
                Quaternion targetRotation = Quaternion.LookRotation(direction3D, Vector3.up);

                // Apply smoothed rotation
                targetTransform.Invoke().rotation = Quaternion.Lerp(targetTransform.Invoke().rotation, targetRotation, speed.Invoke() * Time.fixedDeltaTime);
            }
        }
    }
}