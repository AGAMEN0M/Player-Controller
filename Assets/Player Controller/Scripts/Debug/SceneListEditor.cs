/*
 * ---------------------------------------------------------------------------
 * Description: Holds a list of SceneAssets for editor-only scene switching.
 * 
 * Author: Lucas Gomes Cecchini
 * Pseudonym: AGAMENOM
 * ---------------------------------------------------------------------------
*/

using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SceneList", menuName = "Player Controller/Debug/Scene List (Editor)", order = 0)]
public class SceneListEditor : ScriptableObject
{
#if UNITY_EDITOR
    [Tooltip("List of scene assets to display in the dropdown.")]
    public List<SceneAsset> scenes = new();
#endif
}