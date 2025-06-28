using UnityEngine;

namespace PlayerController.Utils
{
    /// <summary>
    /// Utility class for player movement and collision handling.
    /// Provides vector rotation and slope detection/handling methods.
    /// </summary>
    public class PlayerUtils
    {
        /// <summary>
        /// Rotates a 2D vector by a given angle in degrees.
        /// </summary>
        /// <param name="direction">The original direction vector.</param>
        /// <param name="rotation">Rotation angle in degrees (clockwise).</param>
        /// <returns>The rotated vector.</returns>
        public static Vector2 ConvertRotation(Vector2 direction, float rotation)
        {
            // Convert degrees to radians.
            float radians = rotation * Mathf.Deg2Rad;

            // Pre-calculate cosine and sine.
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            // Apply 2D rotation matrix.
            float x = direction.x * cos - direction.y * sin;
            float y = direction.x * sin + direction.y * cos;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Checks if the slope angle from a collision is within a valid range and applies the appropriate physics material.
        /// Useful for switching between high and low friction based on ground angle.
        /// </summary>
        /// <param name="collision">Collision info from OnCollisionStay or OnCollisionEnter.</param>
        /// <param name="colliders">List of colliders whose materials should be adjusted.</param>
        /// <param name="highFrictionMaterial">Material to apply when the slope is walkable.</param>
        /// <param name="lowFrictionMaterial">Material to apply when the slope is too steep.</param>
        /// <param name="lastSlopeAngle">Reference to last slope angle (will be updated).</param>
        /// <param name="maxStableAngle">Maximum slope angle considered stable (walkable), in degrees.</param>
        /// <param name="debug">Enable debug logs and rays.</param>
        /// <returns>True if slope is within a valid range (<= maxStableAngle); otherwise false.</returns>
        public static bool CheckValidAngle(Collision collision, Collider[] colliders, PhysicsMaterial highFrictionMaterial, PhysicsMaterial lowFrictionMaterial, ref float lastSlopeAngle, float maxStableAngle = 45f, bool debug = false)
        {
            // Iterate over all contact points in the collision.
            foreach (ContactPoint contact in collision.contacts)
            {
                // Calculate the slope angle between the surface normal and world up.
                float slopeAngle = Vector3.Angle(contact.normal, Vector3.up);

                if (debug)
                {
                    // Visualize the normal in the scene view and log the angle.
                    Debug.DrawRay(contact.point, contact.normal * 0.5f, Color.purple, 0.5f);
                    Debug.Log($"Slope Angle changed to: {slopeAngle}");
                }

                // If the slope angle hasn't changed since the last check, reuse the result.
                if (Mathf.Approximately(slopeAngle, lastSlopeAngle))
                {
                    return slopeAngle <= maxStableAngle;
                }

                lastSlopeAngle = slopeAngle; // Update last recorded angle.

                // Choose material based on slope.
                var targetMaterial = slopeAngle <= maxStableAngle ? highFrictionMaterial : lowFrictionMaterial;

                // Apply the selected material to all colliders.
                foreach (var col in colliders)
                {
                    if (col.material != targetMaterial) col.material = targetMaterial;
                }

                // Return whether the slope is within walkable limits.
                return slopeAngle <= maxStableAngle;
            }

            return false; // If no contact points were found, default to false.
        }
    }
}