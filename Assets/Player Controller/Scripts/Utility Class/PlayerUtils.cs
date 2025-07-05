/*
 * ---------------------------------------------------------------------------
 * Description: Utility class for player movement and collision handling,
 *              including vector rotation and slope detection/handling.
 *              
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

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
    }
}