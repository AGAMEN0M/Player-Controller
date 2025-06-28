/*
 * ---------------------------------------------------------------------------
 * Description: Handles movement input from custom key bindings, allowing the 
 * player to move in four directions based on the most recent key press. 
 * Ensures movement input is processed consistently and in a prioritized order.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

namespace CustomKeyboard
{
    public class MovementHandler
    {
        // Tracks the last time each direction input was pressed.
        private float forwardLastPressedTime = -1f;
        private float backLastPressedTime = -1f;
        private float rightLastPressedTime = -1f;
        private float leftLastPressedTime = -1f;

        // Flags to indicate if the input for a specific direction is active.
        private bool forwardInputActive = false;
        private bool backInputActive = false;
        private bool rightInputActive = false;
        private bool leftInputActive = false;

    #pragma warning disable IDE0044 // Field can be made readonly warning is suppressed.
        private InputData forwardInput;
        private InputData backInput;
        private InputData rightInput;
        private InputData leftInput;
    #pragma warning restore IDE0044

        /// <summary>
        /// Initializes a new instance of the MovementHandler class with directional input mappings.
        /// </summary>
        /// <param name="forward">InputData for moving forward.</param>
        /// <param name="back">InputData for moving backward.</param>
        /// <param name="right">InputData for moving right.</param>
        /// <param name="left">InputData for moving left.</param>
        public MovementHandler(InputData forward, InputData back, InputData right, InputData left)
        {
            forwardInput = forward;
            backInput = back;
            rightInput = right;
            leftInput = left;
        }

        /// <summary>
        /// Calculates and returns the current movement direction as a normalized Vector3 based on input.
        /// </summary>
        /// <returns>Normalized movement direction vector.</returns>
        public Vector3 GetMovementInput()
        {
            Vector3 inputDirection = Vector3.zero; // Initialize to zero vector.

            // Handle key down events: updates time and sets active flags.
            if (Input.GetKeyDown(forwardInput.keyboard))
            {
                forwardLastPressedTime = Time.time; // Record the time when forward key is pressed.
                forwardInputActive = true; // Mark forward input as active.
            }
            if (Input.GetKeyDown(backInput.keyboard))
            {
                backLastPressedTime = Time.time;
                backInputActive = true;
            }
            if (Input.GetKeyDown(rightInput.keyboard))
            {
                rightLastPressedTime = Time.time;
                rightInputActive = true;
            }
            if (Input.GetKeyDown(leftInput.keyboard))
            {
                leftLastPressedTime = Time.time;
                leftInputActive = true;
            }

            // Handle key events: resets active flags when keys are released.
            if (!Input.GetKey(forwardInput.keyboard)) forwardInputActive = false;
            if (!Input.GetKey(backInput.keyboard)) backInputActive = false;
            if (!Input.GetKey(rightInput.keyboard)) rightInputActive = false;
            if (!Input.GetKey(leftInput.keyboard)) leftInputActive = false;

            // Prioritize forward or backward movement based on most recent key press.
            if (forwardInputActive && (!backInputActive || forwardLastPressedTime > backLastPressedTime))
            {
                inputDirection -= Vector3.forward; // Move forward.
            }
            else if (backInputActive && (!forwardInputActive || backLastPressedTime > forwardLastPressedTime))
            {
                inputDirection -= Vector3.back; // Move backward.
            }

            // Prioritize right or left movement based on most recent key press.
            if (rightInputActive && (!leftInputActive || rightLastPressedTime > leftLastPressedTime))
            {
                inputDirection -= Vector3.right; // Move right.
            }
            else if (leftInputActive && (!rightInputActive || leftLastPressedTime > rightLastPressedTime))
            {
                inputDirection -= Vector3.left; // Move left.
            }

            return inputDirection.normalized; // Normalize direction for consistent speed.
        }
    }
}