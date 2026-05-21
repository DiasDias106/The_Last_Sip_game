using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;

public static class NavMeshBaker
{
    private static readonly string[] ScenesToBake =
    {
        "Assets/Scenes/level_1.unity",
        "Assets/Scenes/level_2.unity",
        "Assets/Scenes/level_3.unity",
        "Assets/Scenes/boss_room.unity",
    };

    [MenuItem("Tools/Bake NavMesh All Levels")]
    public static void BakeAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[NavMeshBaker] Cancelled.");
            return;
        }

        foreach (string scenePath in ScenesToBake)
            BakeScene(scenePath);

        Debug.Log("[NavMeshBaker] Done — NavMesh baked in level_1, level_2, level_3, boss_room.");
    }

    static void BakeScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogWarning($"[NavMeshBaker] Could not open: {scenePath}");
            return;
        }

        int marked = 0;
        foreach (var mf in Object.FindObjectsOfType<MeshFilter>())
        {
            var go = mf.gameObject;
            if (go.GetComponentInParent<EnemyController>() != null) continue;
            if (go.GetComponentInParent<BossController>()  != null) continue;
            if (go.CompareTag("Player")) continue;
            if (go.GetComponentInParent<CharacterController>() != null) continue;

            var flags    = GameObjectUtility.GetStaticEditorFlags(go);
            var newFlags = flags | StaticEditorFlags.NavigationStatic;
            if (newFlags != flags)
            {
                GameObjectUtility.SetStaticEditorFlags(go, newFlags);
                marked++;
            }
        }

        NavMeshBuilder.BuildNavMesh();
        EditorSceneManager.SaveScene(scene);

        var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
        Debug.Log($"[NavMeshBaker] {scene.name}: marked {marked} objects, {tri.indices.Length / 3} triangles.");
    }
}
