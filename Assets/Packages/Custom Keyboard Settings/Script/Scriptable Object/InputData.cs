/*
 * ---------------------------------------------------------------------------
 * Description: Stores Tag and KeyCode information.
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

/*[CreateAssetMenu(fileName = "InputData", menuName = "ScriptableObjects/InputData", order = 1)]*/
// Class representing input data for keyboard controls.
// This is a ScriptableObject that holds a tag and a key code for keyboard input.
public class InputData : ScriptableObject
{
    public string keyboardTag; // A tag used to identify the input data.
    public KeyCode keyboard; // The key code associated with the input data.
}