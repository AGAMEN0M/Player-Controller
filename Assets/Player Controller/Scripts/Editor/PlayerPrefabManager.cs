using UnityEngine;
using UnityEditor;

public static class PlayerPrefabManager
{
    private static void InstantiateAndSetupPrefab(string prefabName, GameObject parentObject)
    {
        string[] prefabGuids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");

        if (prefabGuids.Length == 0)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in the project.");
            return;
        }

        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"Prefab not found: {prefabName}.prefab. Please check if the prefab exists in the project and the name is correct.");
            return;
        }

        Transform parentTransform = parentObject != null ? parentObject.transform : null;
        GameObject instantiatedObject = PrefabUtility.InstantiatePrefab(prefabAsset, parentTransform) as GameObject;

        CompletePrefabSetup(prefabName, instantiatedObject);
    }

    private static void CompletePrefabSetup(string prefabName, GameObject instantiatedObject)
    {
        if (instantiatedObject == null) return;

        Undo.RegisterCreatedObjectUndo(instantiatedObject, $"Create {prefabName}");

        PrefabUtility.UnpackPrefabInstance(instantiatedObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Selection.activeGameObject = instantiatedObject;

        EditorApplication.delayCall += () =>
        {
            if (Selection.activeGameObject == instantiatedObject)
            {
                EditorWindow.focusedWindow.SendEvent(new Event { keyCode = KeyCode.F2, type = EventType.KeyDown });
            }
        };
    }

    [MenuItem("GameObject/Player Controller/3D/Create Side Perspective")]
    public static void CreateSidePerspectivePlayer()
    {
        InstantiateAndSetupPrefab("Player 3D [Side Perspective]", Selection.activeGameObject);
    }
}