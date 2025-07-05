/*
 * ---------------------------------------------------------------------------
 * Description: Detects box-shaped collisions using OverlapBox with runtime-configurable 
 *              providers for position, size, rotation, layers, triggers, and filtering.
 *              Supports debug logging and editor gizmo visualization via PhysicsUpdateBroadcaster.
 *              Automatically registers and unregisters from update cycles.
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
    /// Detects box-shaped physics collisions using OverlapBox and configurable runtime providers.
    /// Also supports custom filtering and optional debug visualization via Gizmos.
    /// </summary>
    public class BoxCollisionSensor
    {
        private readonly Func<bool> enableDetectionProvider; // Controls whether collision detection is enabled at runtime.
        private readonly Func<bool> enableDebugLogProvider; // Controls whether collisions are logged in the console when detected.
        private readonly Func<DetectionFilter> filterModeProvider; // Specifies which filtering logic to apply to detected colliders.
        private readonly Func<Transform> referenceParentTransformProvider; // Used in filtering: a reference transform to exclude children from detection.
        private readonly Func<HashSet<string>> ignoredTagsProvider; // Used in filtering: a set of tags to ignore from detection.

        /// <summary>Provides the world-space center position of the box.</summary>
        public Func<Vector3> boxCenterProvider;

        /// <summary>Provides the box size (width, height, depth).</summary>
        public Func<Vector3> boxSizeProvider;

        /// <summary>Provides the rotation of the box in world space.</summary>
        public Func<Quaternion> boxRotationProvider;

        private readonly Func<LayerMask> collisionLayerMaskProvider; // Specifies which layers should be considered for collision detection.
        private readonly Func<QueryTriggerInteraction> triggerInteractionProvider; // Specifies whether trigger colliders should be included.

        /// <summary>Target object for drawing gizmos (e.g., used with editor selection).</summary>
        public Func<GameObject> gizmoTargetObjectProvider;

        /// <summary>Defines how and when the gizmos should be drawn (always, selected, none).</summary>
        public Func<GizmoDisplayMode> gizmoDrawingModeProvider;

        /// <summary>Provides the color to use when drawing gizmos.</summary>
        public Func<Color> gizmosColorProvider;

        /// <summary>Defines how collision results are filtered after detection.</summary>
        public enum DetectionFilter { None, IsNotChildOf, IgnoreByTags, All }

        /// <summary>Specifies when gizmos should be drawn in the editor.</summary>
        public enum GizmoDisplayMode { None, Always, SelectedOnly }

        private readonly Collider[] hitResults = new Collider[16]; // Reusable array for OverlapBox collision hits.

        /// <summary>Indicates whether a collision was detected during the last check.</summary>
        public bool collisionDetected = false;

        /// <summary>
        /// Constructs a new BoxCollisionSensor with configurable runtime data providers and filtering options.
        /// Automatically registers with the PhysicsUpdateBroadcaster.
        /// </summary>
        /// <param name="boxCenterProvider">Provides the world-space center position of the box.</param>
        /// <param name="boxSizeProvider">Provides the box size (width, height, depth).</param>
        /// <param name="boxRotationProvider">Provides the rotation of the box in world space.</param>
        /// <param name="collisionLayerMaskProvider">Specifies which layers should be considered for collision detection.</param>
        /// <param name="triggerInteractionProvider">Specifies whether trigger colliders should be included.</param>
        /// <param name="filterModeProvider">Specifies which filtering logic to apply to detected colliders.</param>
        /// <param name="referenceParentTransformProvider">Used to exclude child transforms from detection.</param>
        /// <param name="ignoredTagsProvider">Tags that should be ignored during collision detection.</param>
        /// <param name="enableDetectionProvider">Controls whether collision detection is enabled at runtime.</param>
        /// <param name="enableDebugLogProvider">Controls whether collisions are logged in the console.</param>
        /// <param name="gizmoTargetObjectProvider">Target object to test for selection when drawing gizmos.</param>
        /// <param name="gizmoDrawingModeProvider">Defines how and when gizmos should be drawn.</param>
        /// <param name="gizmosColorProvider">Defines the color used for gizmo drawing.</param>
        public BoxCollisionSensor(
            Func<Vector3> boxCenterProvider,
            Func<Vector3> boxSizeProvider,
            Func<Quaternion> boxRotationProvider = default,
            Func<LayerMask> collisionLayerMaskProvider = default,
            Func<QueryTriggerInteraction> triggerInteractionProvider = default,
            Func<DetectionFilter> filterModeProvider = default,
            Func<Transform> referenceParentTransformProvider = default,
            Func<HashSet<string>> ignoredTagsProvider = default,
            Func<bool> enableDetectionProvider = default,
            Func<bool> enableDebugLogProvider = default,
            Func<GameObject> gizmoTargetObjectProvider = default,
            Func<GizmoDisplayMode> gizmoDrawingModeProvider = default,
            Func<Color> gizmosColorProvider = default)
        {
            // Core geometry and physics config.
            this.boxCenterProvider = boxCenterProvider;
            this.boxSizeProvider = boxSizeProvider;
            this.boxRotationProvider = boxRotationProvider ?? (() => Quaternion.identity);
            this.collisionLayerMaskProvider = collisionLayerMaskProvider ?? (() => -1);
            this.triggerInteractionProvider = triggerInteractionProvider ?? (() => QueryTriggerInteraction.Ignore);

            // Detection and filtering.
            this.filterModeProvider = filterModeProvider ?? (() => DetectionFilter.None);
            this.referenceParentTransformProvider = referenceParentTransformProvider ?? (() => null);
            this.ignoredTagsProvider = ignoredTagsProvider ?? (() => null);

            // Runtime logic switches.
            this.enableDetectionProvider = enableDetectionProvider ?? (() => true);
            this.enableDebugLogProvider = enableDebugLogProvider ?? (() => false);

            // Gizmo visualization config.
            this.gizmoTargetObjectProvider = gizmoTargetObjectProvider ?? (() => null);
            this.gizmoDrawingModeProvider = gizmoDrawingModeProvider ?? (() => GizmoDisplayMode.None);
            this.gizmosColorProvider = gizmosColorProvider ?? (() => Color.red);

            // Subscribe to the update broadcaster for fixed timestep detection.
            PhysicsUpdateBroadcaster.OnFixedUpdate += CheckForCollisions;
            PhysicsUpdateBroadcaster.AddSensor(this);
        }

        /// <summary>
        /// Unsubscribes and disposes the sensor, removing it from the update cycle and clearing the reference.
        /// </summary>
        /// <param name="detector">The sensor reference to nullify after disposal.</param>
        public static void Dispose(ref BoxCollisionSensor detector)
        {
            if (detector != null)
            {
                PhysicsUpdateBroadcaster.OnFixedUpdate -= detector.CheckForCollisions;
                PhysicsUpdateBroadcaster.RemoveSensor(detector);
                detector = null;
            }
        }

        // Performs collision detection using OverlapBox and updates the state accordingly.
        private void CheckForCollisions()
        {
            // Skip if detection is disabled.
            if (enableDetectionProvider?.Invoke() != true) return;

            collisionDetected = false;

            // Get box parameters and query options.
            var center = boxCenterProvider?.Invoke() ?? Vector3.zero;
            var size = boxSizeProvider?.Invoke() ?? Vector3.one;
            var rotation = boxRotationProvider?.Invoke() ?? Quaternion.identity;
            var mask = collisionLayerMaskProvider?.Invoke() ?? -1;
            var trigger = triggerInteractionProvider?.Invoke() ?? QueryTriggerInteraction.Ignore;

            // Perform collision query using non-alloc version.
            int hitCount = Physics.OverlapBoxNonAlloc(center, size / 2f, hitResults, rotation, mask, trigger);

            // Check each result to see if it passes the filtering logic.
            for (int i = 0; i < hitCount; i++)
            {
                var collider = hitResults[i];
                if (PassesDetectionFilter(collider))
                {
                    // Optionally log for debugging.
                    if (enableDebugLogProvider?.Invoke() == true)
                    {
                        Debug.Log($"Collision with: {collider.name}", collider);
                    }

                    // Set detection flag and stop processing.
                    collisionDetected = true;
                    break;
                }
            }
        }

        // Applies custom filtering logic to decide whether a collider should be considered.
        private bool PassesDetectionFilter(Collider collider)
        {
            var filter = filterModeProvider?.Invoke() ?? DetectionFilter.All;
            var parent = referenceParentTransformProvider?.Invoke();
            var ignored = ignoredTagsProvider?.Invoke();

            // Evaluate based on selected filter mode.
            return filter switch
            {
                DetectionFilter.None => true, // No filtering, always accept.
                DetectionFilter.IsNotChildOf => parent == null || !collider.transform.IsChildOf(parent),
                DetectionFilter.IgnoreByTags => ignored == null || !ignored.Contains(collider.tag),
                DetectionFilter.All => (parent == null || !collider.transform.IsChildOf(parent)) && (ignored == null || !ignored.Contains(collider.tag)),
                _ => true, // Fallback case, default to accepting.
            };
        }
    }
}