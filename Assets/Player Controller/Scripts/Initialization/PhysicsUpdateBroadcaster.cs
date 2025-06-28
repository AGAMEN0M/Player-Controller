using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static PlayerController.PhysicsRuntime.BoxCollisionSensor;

namespace PlayerController.PhysicsRuntime
{
    /// <summary>
    /// Broadcasts Unity lifecycle events (Update, FixedUpdate, LateUpdate) via static events.
    /// Also draws gizmos for registered BoxCollisionSensor components based on their settings.
    /// Ensures a single persistent instance across the game.
    /// </summary>
    public class PhysicsUpdateBroadcaster : MonoBehaviour
    {
        /// <summary> Singleton instance of this broadcaster. </summary>
        public static PhysicsUpdateBroadcaster Instance { get; private set; }

        /// <summary> Invoked every Update frame. </summary>
        public static event Action OnUpdate;

        /// <summary> Invoked every FixedUpdate frame. </summary>
        public static event Action OnFixedUpdate;

        /// <summary> Invoked every LateUpdate frame. </summary>
        public static event Action OnLateUpdate;

        // List of all registered sensors for gizmo rendering.
        private static readonly List<BoxCollisionSensor> registeredSensors = new();

        private void Awake()
        {
            // Enforce singleton pattern.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy duplicates.
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes.
        }

        private void Update() => OnUpdate?.Invoke(); // Broadcast Update event.
        private void FixedUpdate() => OnFixedUpdate?.Invoke(); // Broadcast FixedUpdate event.
        private void LateUpdate() => OnLateUpdate?.Invoke(); // Broadcast LateUpdate event.

        // Draw gizmos for all registered sensors, if applicable.
        private void OnDrawGizmos()
        {
            foreach (var sensor in registeredSensors)
            {
                // Determine whether the sensor should draw gizmos.
                var gizmoMode = sensor.gizmoDrawingModeProvider?.Invoke() ?? GizmoDisplayMode.None;
                var gizmoTarget = sensor.gizmoTargetObjectProvider?.Invoke();

                if (gizmoMode == GizmoDisplayMode.Always)
                {
                    DrawSensorGizmo(sensor);
                }

            #if UNITY_EDITOR
                // Only draw if the sensor target is selected in the editor.
                if (gizmoMode == GizmoDisplayMode.SelectedOnly && gizmoTarget != null && Selection.Contains(gizmoTarget))
                {
                    DrawSensorGizmo(sensor);
                }
            #endif
            }
        }

        // Renders a wireframe cube based on the sensor’s position, rotation, and size.
        private void DrawSensorGizmo(BoxCollisionSensor sensor)
        {
            Gizmos.color = sensor.gizmosColorProvider?.Invoke() ?? Color.red;
            Gizmos.matrix = Matrix4x4.TRS(sensor.boxCenterProvider(), sensor.boxRotationProvider(), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, sensor.boxSizeProvider());
        }

        /// <summary>
        /// Registers a sensor to receive gizmo drawing.
        /// </summary>
        /// <param name="sensor">Sensor to register.</param>
        public static void AddSensor(BoxCollisionSensor sensor)
        {
            if (!registeredSensors.Contains(sensor))
            {
                registeredSensors.Add(sensor);
            }
        }

        /// <summary>
        /// Unregisters a sensor from gizmo drawing.
        /// </summary>
        /// <param name="sensor">Sensor to unregister.</param>
        public static void RemoveSensor(BoxCollisionSensor sensor)
        {
            if (registeredSensors.Contains(sensor))
            {
                registeredSensors.Remove(sensor);
            }
        }

        /// <summary>
        /// Automatically ensures a single persistent instance exists across scenes.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (Instance != null)
            {
                return; // Already initialized.
            }

            GameObject gameObject = new("[Physics Update Broadcaster]");
            gameObject.AddComponent<PhysicsUpdateBroadcaster>();
            DontDestroyOnLoad(gameObject);
        }
    }
}