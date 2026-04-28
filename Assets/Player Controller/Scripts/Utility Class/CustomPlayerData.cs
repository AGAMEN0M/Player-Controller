/*
 * ---------------------------------------------------------------------------
 * Description: Helper class for serializing and deserializing custom player data
 *              using JSON, supporting Unity types and enums with type metadata.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlayerController.CustomData
{
    /// <summary>
    /// Helper class for serializing and deserializing custom player data.
    /// </summary>
    public class CustomPlayerData
    {
        #region === Inner Classes ===

        /// <summary>
        /// Represents a serializable key-value pair with type metadata.
        /// </summary>
        [Serializable]
        private class Entry
        {
            [Tooltip("Unique identifier for the stored data entry. Used as the dictionary key during serialization and deserialization.")]
            public string key;

            [Tooltip("Serialized value stored as a string. May contain raw text, JSON (for Unity types), or enum names.")]
            public string value;

            [Tooltip("Assembly-qualified type name of the original value. Used to reconstruct the correct data type when loading.")]
            public string type;
        }

        /// <summary>
        /// Container class for a list of <see cref="Entry"/> objects for serialization.
        /// </summary>
        [Serializable]
        private class EntryList
        {
            [Tooltip("Collection of serialized key-value entries that represent the full dataset.")]
            public List<Entry> entries = new();
        }

        #endregion

        #region === Serialization Methods ===

        /// <summary>
        /// Serializes a dictionary of data into a JSON string.
        /// </summary>
        /// <param name="dataBuilder">Function that returns a dictionary with player data.</param>
        /// <returns>JSON string representing the player data.</returns>
        public static string SaveData(Func<Dictionary<string, object>> dataBuilder)
        {
            var dict = dataBuilder(); // Build the data dictionary.
            var list = new EntryList(); // Container for serializable entries.

            foreach (var kvp in dict)
            {
                if (kvp.Value == null) continue; // Skip null entries.

                var type = kvp.Value.GetType();
                string serializedValue;

                if (type.IsEnum)
                {
                    serializedValue = kvp.Value.ToString(); // Store enum as string.
                }
                else if (type.Namespace == "UnityEngine")
                {
                    serializedValue = JsonUtility.ToJson(kvp.Value); // Serialize Unity types using JsonUtility.
                }
                else
                {
                    serializedValue = kvp.Value.ToString(); // Fallback to string conversion.
                }

                // Add entry to list.
                list.entries.Add(new Entry
                {
                    key = kvp.Key,
                    value = serializedValue,
                    type = type.AssemblyQualifiedName
                });
            }

            return JsonUtility.ToJson(list); // Serialize entry list to JSON.
        }

        /// <summary>
        /// Deserializes a JSON string and applies the values to a dictionary.
        /// </summary>
        /// <param name="json">Serialized JSON string.</param>
        /// <param name="applyData">Action that receives the deserialized dictionary.</param>
        public static void LoadData(string json, Action<Dictionary<string, object>> applyData)
        {
            var list = JsonUtility.FromJson<EntryList>(json); // Deserialize entry list.
            var dict = new Dictionary<string, object>(); // Dictionary to populate.

            // Define known type converters.
            Dictionary<Type, Func<string, object>> converters = new()
            {
                { typeof(float), s => float.Parse(s) },
                { typeof(bool), s => bool.Parse(s) },
                { typeof(int), s => int.Parse(s) },
                { typeof(string), s => s },
                { typeof(Vector2), s => JsonUtility.FromJson<Vector2>(s) },
                { typeof(Vector3), s => JsonUtility.FromJson<Vector3>(s) },
                { typeof(Vector4), s => JsonUtility.FromJson<Vector4>(s) },
                { typeof(Quaternion), s => JsonUtility.FromJson<Quaternion>(s) }
            };

            foreach (var entry in list.entries)
            {
                var type = Type.GetType(entry.type);
                if (type == null)
                {
                    Debug.LogWarning($"Type not found: {entry.type}");
                    continue;
                }

                object value;

                if (type.IsEnum)
                {
                    value = Enum.Parse(type, entry.value); // Parse enum from string.
                }
                else if (converters.TryGetValue(type, out var converter))
                {
                    value = converter(entry.value); // Use custom converter.
                }
                else
                {
                    value = Convert.ChangeType(entry.value, type); // Attempt fallback conversion.
                }

                dict[entry.key] = value;
            }

            applyData(dict); // Apply the final data.
        }

        #endregion
    }
}