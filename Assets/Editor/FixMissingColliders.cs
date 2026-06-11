using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class FixMissingColliders
{
    [MenuItem("Tools/Fix Missing Colliders/Level 1 Platforms")]
    static void FixLevel1Platforms()
    {
        string scenePath = "Assets/Scenes/level_1.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        if (!scene.IsValid())
        {
            Debug.LogError("Could not open level_1 scene.");
            return;
        }

        int added = 0;
        int skipped = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool isFloor = n.StartsWith("Floor_01") || n.StartsWith("P_MOD_Floor");
                bool isLava  = n.ToLowerInvariant() == "lava" || n.ToLowerInvariant().StartsWith("lava (");
                if (!isFloor && !isLava)
                    continue;

                if (!isLava && t.position.y < 1.0f)
                    continue;

                if (t.GetComponent<MeshCollider>() != null)
                {
                    skipped++;
                    continue;
                }

                var mf = t.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var col = t.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
                added++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[FixMissingColliders] Level 1: added {added} MeshColliders. {skipped} already had one.");
        EditorUtility.DisplayDialog("Done", $"Added {added} MeshColliders to elevated Floor_01 objects in level_1.\n{skipped} already had colliders.", "OK");
    }

    [MenuItem("Tools/Fix Missing Colliders/Baco NPC Collider")]
    static void FixBacoCollider()
    {
        string scenePath = "Assets/Scenes/masmorra_spawn.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        if (!scene.IsValid())
        {
            Debug.LogError("Could not open masmorra_spawn scene.");
            return;
        }

        GameObject baco = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("baco") || t.name.ToLower().Contains("deity_of_wine"))
                {
                    baco = t.gameObject;
                    break;
                }
            }
            if (baco != null) break;
        }

        if (baco == null)
        {
            Debug.LogError("[FixMissingColliders] Baco NPC not found in masmorra_spawn.");
            EditorUtility.DisplayDialog("Not Found", "Baco NPC not found in masmorra_spawn scene.", "OK");
            return;
        }

        if (baco.GetComponent<CapsuleCollider>() != null)
        {
            Debug.Log("[FixMissingColliders] Baco already has a CapsuleCollider.");
            EditorUtility.DisplayDialog("Already Done", "Baco already has a CapsuleCollider.", "OK");
            return;
        }

        var col = baco.AddComponent<CapsuleCollider>();
        // Baco scale ~145x; local radius 0.003 ≈ 0.43m world, height 0.012 ≈ 1.74m world
        // Center y = height/2 so feet sit at pivot (adjust in Inspector if needed)
        col.direction = 1; // Y axis
        col.radius = 0.003f;
        col.height = 0.012f;
        col.center = new Vector3(0f, col.height * 0.5f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[FixMissingColliders] Added CapsuleCollider to {baco.name}. You may need to adjust size in Inspector.");
        EditorUtility.DisplayDialog("Done", $"Added CapsuleCollider to '{baco.name}'.\nPlease verify and adjust the collider size in the Inspector.", "OK");
    }
}
