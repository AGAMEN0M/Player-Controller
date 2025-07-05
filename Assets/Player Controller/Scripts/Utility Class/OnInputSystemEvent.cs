/*
 * ---------------------------------------------------------------------------
 * Description: Generic static dispatcher for InputAction<T> events (pressed, hold, released).
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

namespace PlayerController.InputEvents
{
    /// <summary>
    /// A generic static dispatcher that listens to any InputAction<T> value (e.g., float, Vector2, bool)
    /// and invokes delegates for pressed, hold, and released states dynamically per action.
    /// </summary>
    /// <typeparam name="T">The type returned by ReadValue<T>(), such as Vector2, float, or bool.</typeparam>
    public static class OnInputSystemEvent<T> where T : struct
    {
        /// <summary>
        /// Holds internal state and callback bindings per InputAction.
        /// </summary>
        private static readonly Dictionary<InputAction, State> states = new();

        /// <summary>
        /// Encapsulates runtime state for an individual InputAction, including previous value,
        /// whether the input was held, and registered callbacks for different input states.
        /// </summary>
        internal sealed class State
        {
            public InputAction action;
            public T lastValue;
            public bool wasHeld;

            public Action<T> OnHold;    // Called every frame while input is active.
            public Action OnReleased;   // Called when input is released.
            public Action<T> OnPressed; // Called when input becomes active.

            public Func<bool> Enabled = () => true; // Guard clause to enable/disable this action evaluation.

            /// <summary>
            /// Evaluates the input state, and invokes the appropriate callbacks depending on the transition.
            /// </summary>
            public void Update()
            {
                if (action == null || !action.enabled || !Enabled())
                    return;

                T value = action.ReadValue<T>();
                bool isHeld = IsNonZero(value); // Determine whether input is active (pressed/held).

                if (isHeld)
                {
                    if (!wasHeld) OnPressed?.Invoke(value); // Transition: inactive → active
                    OnHold?.Invoke(value); // Continuously called while active.
                }
                else if (wasHeld)
                {
                    OnReleased?.Invoke(); // Transition: active → inactive
                }

                wasHeld = isHeld;
                lastValue = value;
            }

            /// <summary>
            /// Evaluates whether a value should be considered 'active' (non-zero).
            /// </summary>
            private static bool IsNonZero(T value)
            {
                return value switch
                {
                    float f => Mathf.Abs(f) > 0.01f,
                    Vector2 v => v.sqrMagnitude > 0.01f,
                    Vector3 v3 => v3.sqrMagnitude > 0.01f,
                    Quaternion q => q != Quaternion.identity,
                    bool b => b,
                    _ => !EqualityComparer<T>.Default.Equals(value, default)
                };
            }
        }

        #region Registration Methods

        /// <summary>
        /// Registers a callback to be called continuously while the input is active (held).
        /// </summary>
        public static void RegisterHold(InputAction action, Action<T> callback)
        {
            var state = GetOrCreateState(action);
            state.OnHold += callback;
        }

        /// <summary>
        /// Registers a callback to be called once when the input is first activated (pressed).
        /// </summary>
        public static void RegisterPressed(InputAction action, Action<T> callback)
        {
            var state = GetOrCreateState(action);
            state.OnPressed += callback;
        }

        /// <summary>
        /// Registers a callback to be called once when the input is released.
        /// </summary>
        public static void RegisterReleased(InputAction action, Action callback)
        {
            var state = GetOrCreateState(action);
            state.OnReleased += callback;
        }

        /// <summary>
        /// Unregisters a previously registered hold callback.
        /// </summary>
        public static void UnregisterHold(InputAction action, Action<T> callback)
        {
            if (states.TryGetValue(action, out var state)) state.OnHold -= callback;
        }

        /// <summary>
        /// Unregisters a previously registered pressed callback.
        /// </summary>
        public static void UnregisterPressed(InputAction action, Action<T> callback)
        {
            if (states.TryGetValue(action, out var state)) state.OnPressed -= callback;
        }

        /// <summary>
        /// Unregisters a previously registered released callback.
        /// </summary>
        public static void UnregisterReleased(InputAction action, Action callback)
        {
            if (states.TryGetValue(action, out var state)) state.OnReleased -= callback;
        }

        /// <summary>
        /// Clears all registered callbacks and removes the action state entirely.
        /// </summary>
        public static void Clear(InputAction action)
        {
            if (states.TryGetValue(action, out var state))
            {
                state.OnHold = null;
                state.OnPressed = null;
                state.OnReleased = null;
                states.Remove(action);
            }
        }

        #endregion

        /// <summary>
        /// Begins monitoring the specified action path from an InputActionAsset, returning a fluent configuration.
        /// </summary>
        /// <param name="asset">The InputActionAsset containing the action.</param>
        /// <param name="path">The action path (e.g. "Gameplay/Jump").</param>
        /// <param name="enabled">Optional condition to enable/disable this action's processing at runtime.</param>
        public static OnInputSystemEventConfig<T> WithAction(InputActionAsset asset, string path, Func<bool> enabled = null)
        {
            return new OnInputSystemEventConfig<T>(asset, path, enabled ?? (() => true));
        }

        /// <summary>
        /// Retrieves an existing state or creates one for the given action, attaching it to the input system update cycle.
        /// </summary>
        internal static State GetOrCreateState(InputAction action)
        {
            if (!states.TryGetValue(action, out var state))
            {
                state = new State { action = action, Enabled = () => true };
                states[action] = state;

                // Ensure Update is called every frame after InputSystem update.
                InputSystem.onAfterUpdate += () =>
                {
                    if (states.ContainsKey(action)) state.Update();
                };
            }
            return state;
        }
    }

    /// <summary>
    /// Fluent configuration for registering input event callbacks on a specific action.
    /// </summary>
    /// <typeparam name="T">The input value type (e.g. float, Vector2, bool).</typeparam>
    public class OnInputSystemEventConfig<T> where T : struct
    {
        private readonly InputAction action;

        /// <summary>
        /// Binds the input action from the asset and initializes its runtime evaluation.
        /// </summary>
        /// <param name="asset">The InputActionAsset to read from.</param>
        /// <param name="actionPath">The action path (e.g., "Gameplay/Jump").</param>
        /// <param name="enabled">Optional delegate to control runtime activation of callbacks.</param>
        public OnInputSystemEventConfig(InputActionAsset asset, string actionPath, Func<bool> enabled)
        {
            action = asset.FindAction(actionPath, throwIfNotFound: true);
            action.Enable();

            var state = OnInputSystemEvent<T>.GetOrCreateState(action);
            state.Enabled = enabled;
        }

        /// <summary>
        /// Registers a callback to be called while the input is held.
        /// </summary>
        public OnInputSystemEventConfig<T> OnHold(Action<T> callback)
        {
            OnInputSystemEvent<T>.RegisterHold(action, callback);
            return this;
        }

        /// <summary>
        /// Registers a callback to be called when the input is pressed.
        /// </summary>
        public OnInputSystemEventConfig<T> OnPressed(Action<T> callback)
        {
            OnInputSystemEvent<T>.RegisterPressed(action, callback);
            return this;
        }

        /// <summary>
        /// Registers a callback to be called when the input is released.
        /// </summary>
        public OnInputSystemEventConfig<T> OnReleased(Action callback)
        {
            OnInputSystemEvent<T>.RegisterReleased(action, callback);
            return this;
        }

        /// <summary>
        /// Clears all callbacks associated with this action.
        /// </summary>
        public OnInputSystemEventConfig<T> UnbindAll()
        {
            OnInputSystemEvent<T>.Clear(action);
            return this;
        }
    }
}