/*
 * ---------------------------------------------------------------------------
 * Description: Performs downward raycasts to check if the player is grounded on a slope
 *              within a valid angle range and applies corresponding friction materials to colliders.
 *              Integrates with PhysicsUpdateBroadcaster to perform checks on FixedUpdate.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlayerController.PhysicsRuntime
{
    /// <summary>
    /// Performs a downward raycast from a target transform to check if the player is grounded
    /// on a surface with a slope angle within a valid range.
    /// It also applies appropriate friction materials to the player's colliders based on the slope.
    /// </summary>
    public class CheckValidAngleSensors
    {
        #region === Providers ===

        // Delegates for deferred access to runtime values.
        private readonly Func<Transform> targetTransform;
        private readonly Func<float> maxStableAngle;
        private readonly Func<float> raycastDistance;
        private readonly Func<PhysicsMaterial> highFrictionMaterial;
        private readonly Func<PhysicsMaterial> lowFrictionMaterial;
        private readonly Func<Collider[]> colliders;
        private readonly Func<bool> debug;

        #endregion

        #region === Fields ===

        /// <summary>
        /// Stores the result of the last slope check.
        /// </summary>
        public bool isValidAngle = true;

        // Reusable buffer to avoid allocations when raycasting.
        private static readonly RaycastHit[] raycastHits = new RaycastHit[8];

        #endregion

        #region === Constructor & Disposal ===

        /// <summary>
        /// Initializes the ground angle validation system by injecting all necessary references and configuration options.
        /// It registers the CheckRaycast method to be called on each FixedUpdate, allowing the component to monitor ground slope in real time.
        /// </summary>
        /// <param name="targetTransform">Delegate that provides the player's transform used as the raycast origin.</param>
        /// <param name="highFrictionMaterial">Delegate that provides the material used on stable (walkable) surfaces.</param>
        /// <param name="lowFrictionMaterial">Delegate that provides the material used on steep (non-walkable) surfaces.</param>
        /// <param name="colliders">Delegate that provides the colliders to apply the friction material to.</param>
        /// <param name="maxStableAngle">Optional delegate that returns the maximum slope angle (in degrees) considered walkable. Defaults to 45° if null.</param>
        /// <param name="raycastDistance">Optional delegate that returns the max distance of the downward raycast. Defaults to 0.5 if null.</param>
        /// <param name="debug">Optional delegate to enable debug visuals and logs. Defaults to false if null.</param>
        public CheckValidAngleSensors(
            Func<Transform> targetTransform,
            Func<PhysicsMaterial> highFrictionMaterial,
            Func<PhysicsMaterial> lowFrictionMaterial,
            Func<Collider[]> colliders,
            Func<float> maxStableAngle = null,
            Func<float> raycastDistance = null,
            Func<bool> debug = null)
        {
            this.targetTransform = targetTransform;
            this.highFrictionMaterial = highFrictionMaterial;
            this.lowFrictionMaterial = lowFrictionMaterial;
            this.colliders = colliders;
            this.maxStableAngle = maxStableAngle ?? (() => 45f); // Default slope angle threshold
            this.raycastDistance = raycastDistance ?? (() => 0.5f); // Default raycast distance
            this.debug = debug ?? (() => false);

            // Register to fixed update callback for physics timing.
            PhysicsUpdateBroadcaster.OnFixedUpdate += CheckRaycast;
        }

        /// <summary>
        /// Cleans up and unregisters the detector from the update callback.
        /// </summary>
        public static void Dispose(ref CheckValidAngleSensors detector)
        {
            if (detector != null)
            {
                PhysicsUpdateBroadcaster.OnFixedUpdate -= detector.CheckRaycast;
                detector = null;
            }
        }

        #endregion

        #region === Core Logic ===

        /// <summary>
        /// Performs a downward raycast to check for valid ground below the player.
        /// Applies appropriate physics material based on slope angle.
        /// </summary>
        private void CheckRaycast()
        {
            Transform transform = targetTransform();
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // Slight vertical offset to avoid self-collision
            float distance = raycastDistance();
            float maxAngle = maxStableAngle();

            // Non-allocating raycast to improve performance and avoid GC
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, raycastHits, distance);

            // No ground detected
            if (hitCount == 0)
            {
                if (debug())
                {
                    Debug.DrawRay(transform.position, Vector3.down * distance, Color.red, 0.5f);
                    Debug.LogWarning("❌ No ground detected.");
                }

                isValidAngle = false;
                return;
            }

            // Sort raycast results by distance (closest first)
            Array.Sort(raycastHits, 0, hitCount, RaycastHitComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = raycastHits[i];
                if (hit.collider == null) continue;

                // Skip if the hit collider is part of the player
                if (hit.collider.transform.IsChildOf(transform)) continue;

                // Skip if surface is too steep vertically (almost wall)
                if (hit.normal.y < 0.5f) continue;

                // Calculate slope angle from normal
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                bool isStable = slopeAngle <= maxAngle + 0.1f;

                // Choose friction material based on slope
                var targetMaterial = isStable ? highFrictionMaterial() : lowFrictionMaterial();

                // Debug drawing and logging
                if (debug())
                {
                    Debug.DrawRay(hit.point, hit.normal * 0.5f, isStable ? Color.green : Color.yellow, 0.5f);

                    if (isStable)
                    {
                        Debug.Log($"✅ Stable ground: {hit.collider.name} | Angle: {slopeAngle:F2}° | Normal.y: {hit.normal.y:F2}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Unstable slope: {hit.collider.name} | Angle: {slopeAngle:F2}° > {maxAngle:F2}°");
                    }
                }

                // Apply material to all colliders
                foreach (var col in colliders())
                {
                    if (col != null && col.material != targetMaterial)
                    {
                        col.material = targetMaterial;
                    }
                }

                isValidAngle = isStable;
                return; // Exit after processing first valid hit
            }

            // All hits were ignored (e.g. player colliders or invalid surfaces)
            if (debug())
            {
                Debug.DrawRay(transform.position, Vector3.down * distance, Color.red, 0.5f);
                Debug.LogWarning("❌ No valid ground detected (all hits ignored).");
            }

            isValidAngle = false;
        }

        #endregion

        #region === Utilities ===

        /// <summary>
        /// Custom comparer to sort RaycastHit results by distance (nearest first).
        /// </summary>
        private class RaycastHitComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitComparer Instance = new();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        #endregion
    }
}