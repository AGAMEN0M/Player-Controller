/*
 * ---------------------------------------------------------------------------
 * Description: A generic static dispatcher that listens to any InputAction<T> value (e.g., float, Vector2, bool)
*               and invokes delegates for pressed, hold, and released states dynamically per action.
*               Each callback is tied to an "owner" (usually a MonoBehaviour) so that multiple scripts
*               can safely share the same InputAction without interfering with each other.
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
    /// Static dispatcher that monitors InputAction values and triggers callbacks for pressed, hold, and released states.
    /// Supports ownership so that callbacks from different scripts can be managed independently.
    /// </summary>
    /// <typeparam name="T">The type of value read from the InputAction (e.g., float, Vector2, bool).</typeparam>
    public static class OnInputSystemEvent<T> where T : struct
    {
        #region === Fields ===

        /// <summary>
        /// Holds runtime states for each registered InputAction.
        /// </summary>
        private static readonly Dictionary<InputAction, State> states = new();

        #endregion

        #region === Nested Classes ===

        /// <summary>
        /// Encapsulates runtime state for an InputAction, including last value,
        /// per-owner pressed/hold/released callbacks, and owner-based unbinding.
        /// </summary>
        internal sealed class State
        {
            public InputAction action;
            public T lastValue;

            // Per-owner callback lists that include an enabled predicate for that owner.
            public readonly List<(object owner, Action<T> cb, Func<bool> enabled)> OnHold = new();
            public readonly List<(object owner, Action<T> cb, Func<bool> enabled)> OnPressed = new();
            public readonly List<(object owner, Action cb, Func<bool> enabled)> OnReleased = new();

            // Track per-owner held state so we can simulate releases when owner becomes disabled.
            private readonly Dictionary<object, bool> ownerWasHeld = new();

            /// <summary>
            /// Updates the InputAction state and invokes callbacks depending on transitions.
            /// </summary>
            public void Update()
            {
                if (action == null || !action.enabled) return;

                // Read current input value.
                T value = action.ReadValue<T>();
                bool isHeld = IsNonZero(value);

                // Gather all owners that have any callbacks.
                var owners = new HashSet<object>();
                foreach (var (owner, _, _) in OnHold) owners.Add(owner);
                foreach (var (owner, _, _) in OnPressed) owners.Add(owner);
                foreach (var (owner, _, _) in OnReleased) owners.Add(owner);

                // Process each owner independently, using their own enabled predicate and wasHeld flag.
                foreach (var owner in owners)
                {
                    var enabled = GetEnabledForOwner(owner) ?? (() => true);

                    bool ownerEnabled;
                    try
                    {
                        ownerEnabled = enabled();
                    }
                    catch
                    {
                        // Fail-safe: if predicate throws, consider enabled.
                        ownerEnabled = true;
                    }

                    ownerWasHeld.TryGetValue(owner, out bool wasHeldForOwner);

                    // If the owner is not enabled but was previously held, simulate release for that owner.
                    if (!ownerEnabled)
                    {
                        if (wasHeldForOwner)
                        {
                            foreach (var (o, cb, en) in OnReleased) if (Equals(o, owner)) cb?.Invoke();

                            ownerWasHeld[owner] = false;
                        }
                        // Skip invoking pressed/hold for this owner while disabled.
                        continue;
                    }

                    // If owner is enabled, handle normal transitions based on global isHeld.
                    if (isHeld)
                    {
                        if (!wasHeldForOwner)
                        {
                            foreach (var (o, cb, en) in OnPressed) if (Equals(o, owner)) cb?.Invoke(value);

                            ownerWasHeld[owner] = true;
                        }

                        foreach (var (o, cb, en) in OnHold) if (Equals(o, owner)) cb?.Invoke(value);
                    }
                    else if (wasHeldForOwner)
                    {
                        foreach (var (o, cb, en) in OnReleased) if (Equals(o, owner)) cb?.Invoke();

                        ownerWasHeld[owner] = false;
                    }
                }

                lastValue = value;
            }

            /// <summary>
            /// Tries to find the enabled predicate associated with an owner from any callback list.
            /// </summary>
            private Func<bool> GetEnabledForOwner(object owner)
            {
                foreach (var (o, _, en) in OnHold) if (Equals(o, owner)) return en;
                foreach (var (o, _, en) in OnPressed) if (Equals(o, owner)) return en;
                foreach (var (o, _, en) in OnReleased) if (Equals(o, owner)) return en;
                return null;
            }

            /// <summary>
            /// Evaluates whether a value should be considered "active".
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

            /// <summary>
            /// Removes all callbacks registered by a given owner and clears its held state.
            /// </summary>
            public void UnregisterAll(object owner)
            {
                OnHold.RemoveAll(e => Equals(e.owner, owner));
                OnPressed.RemoveAll(e => Equals(e.owner, owner));
                OnReleased.RemoveAll(e => Equals(e.owner, owner));
                ownerWasHeld.Remove(owner);
            }
        }

        #endregion

        #region === Registration Methods ===

        /// <summary>
        /// Registers a hold callback tied to an owner with an optional enabled predicate for that owner.
        /// </summary>
        public static void RegisterHold(InputAction action, object owner, Action<T> callback, Func<bool> enabled = null)
        {
            var state = GetOrCreateState(action);
            state.OnHold.Add((owner, callback, enabled ?? (() => true)));
        }

        /// <summary>
        /// Registers a pressed callback tied to an owner with an optional enabled predicate for that owner.
        /// </summary>
        public static void RegisterPressed(InputAction action, object owner, Action<T> callback, Func<bool> enabled = null)
        {
            var state = GetOrCreateState(action);
            state.OnPressed.Add((owner, callback, enabled ?? (() => true)));
        }

        /// <summary>
        /// Registers a released callback tied to an owner with an optional enabled predicate for that owner.
        /// </summary>
        public static void RegisterReleased(InputAction action, object owner, Action callback, Func<bool> enabled = null)
        {
            var state = GetOrCreateState(action);
            state.OnReleased.Add((owner, callback, enabled ?? (() => true)));
        }

        /// <summary>
        /// Removes all callbacks for a given action registered by a specific owner.
        /// </summary>
        public static void UnregisterAll(InputAction action, object owner)
        {
            if (states.TryGetValue(action, out var state)) state.UnregisterAll(owner);
        }

        /// <summary>
        /// Completely clears an action state, removing all callbacks from all owners.
        /// </summary>
        public static void Clear(InputAction action)
        {
            states.Remove(action);
        }

        #endregion

        #region === Config Creation ===

        /// <summary>
        /// Creates a fluent configuration wrapper for a specific InputAction and owner with an optional enabled predicate.
        /// </summary>
        public static OnInputSystemEventConfig<T> WithAction(InputAction action, object owner, Func<bool> enabled = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            action.Enable();

            var state = GetOrCreateState(action);
            // Do not store a single global Enabled here anymore.
            // Per-owner enabled predicates will be stored when registering callbacks.

            return new OnInputSystemEventConfig<T>(action, owner, enabled ?? (() => true));
        }

        /// <summary>
        /// Retrieves an existing state or creates a new one for the given InputAction.
        /// Automatically wires it into the InputSystem update cycle.
        /// </summary>
        internal static State GetOrCreateState(InputAction action)
        {
            if (!states.TryGetValue(action, out var state))
            {
                state = new State { action = action };
                states[action] = state;

                // Ensure Update is called every frame after InputSystem update.
                UnityEngine.InputSystem.InputSystem.onAfterUpdate += () =>
                {
                    if (states.ContainsKey(action)) state.Update();
                };
            }
            return state;
        }

        #endregion
    }

    /// <summary>
    /// Fluent configuration for registering input event callbacks on a specific InputAction.
    /// </summary>
    /// <typeparam name="T">The input value type (e.g. float, Vector2, bool).</typeparam>
    public class OnInputSystemEventConfig<T> where T : struct
    {
        #region === Fields ===

        private readonly InputAction action;
        private readonly object owner;
        private readonly Func<bool> enabled;

        #endregion

        #region === Constructor ===

        /// <summary>
        /// Constructor that binds the config to an InputAction, an owner and an enabled predicate.
        /// </summary>
        public OnInputSystemEventConfig(InputAction action, object owner, Func<bool> enabled)
        {
            this.action = action;
            this.owner = owner;
            this.enabled = enabled;
        }

        #endregion

        #region === Fluent API ===

        /// <summary>
        /// Registers a hold callback.
        /// </summary>
        public OnInputSystemEventConfig<T> OnHold(Action<T> callback)
        {
            OnInputSystemEvent<T>.RegisterHold(action, owner, callback, enabled);
            return this;
        }

        /// <summary>
        /// Registers a pressed callback.
        /// </summary>
        public OnInputSystemEventConfig<T> OnPressed(Action<T> callback)
        {
            OnInputSystemEvent<T>.RegisterPressed(action, owner, callback, enabled);
            return this;
        }

        /// <summary>
        /// Registers a released callback.
        /// </summary>
        public OnInputSystemEventConfig<T> OnReleased(Action callback)
        {
            OnInputSystemEvent<T>.RegisterReleased(action, owner, callback, enabled);
            return this;
        }

        /// <summary>
        /// Disposes the configuration by unbinding all callbacks registered by this owner for the given action.
        /// </summary>
        public OnInputSystemEventConfig<T> Dispose()
        {
            OnInputSystemEvent<T>.UnregisterAll(action, owner);
            return this;
        }

        #endregion
    }
}