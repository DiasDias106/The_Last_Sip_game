using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

public static class BossPrefabBuilder
{
    [MenuItem("Tools/Rebuild Boss Prefabs")]
    public static void RebuildAll()
    {
        BuildMinotaur();
        BuildGolem();
        BuildKnight();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BossPrefabBuilder] Done — MinotaurBoss, GolemBoss, KnightBoss rebuilt.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    static void BuildMinotaur()
    {
        const string fbxPath    = "Assets/Bosses/minotaur_boss/minotaur_boss/Base mesh/minotaur1.fbx";
        const string ctrlPath   = "Assets/Bosses/Animators/Minotaur_AC.controller";
        const string prefabDest = "Assets/Bosses/Prefabs/MinotaurBoss.prefab";

        var fbx  = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
        var mem  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/memories/Prefabs/MemoryPickup.prefab");

        if (fbx == null) { Debug.LogError("[BossPrefabBuilder] minotaur1.fbx not found"); return; }

        Avatar avatar = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (a is Avatar av) { avatar = av; break; }

        var root = new GameObject("MinotaurBoss");

        // Visual child — direct instance of the FBX
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;

        // Animator on root
        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;

        // NavMeshAgent
        var nma = root.AddComponent<NavMeshAgent>();
        nma.speed            = 5f;
        nma.angularSpeed     = 200f;
        nma.stoppingDistance = 3f;
        nma.radius           = 0.8f;
        nma.height           = 3f;

        // CapsuleCollider
        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.8f; col.height = 3f;
        col.center = new Vector3(0, 1.5f, 0);

        // Boss script
        var script = root.AddComponent<MinotaurBossController>();
        script.memoryPrefab = mem;
        script.videoIndex   = 1;

        PrefabUtility.SaveAsPrefabAsset(root, prefabDest);
        Object.DestroyImmediate(root);
        Debug.Log("[BossPrefabBuilder] MinotaurBoss rebuilt");
    }

    // ──────────────────────────────────────────────────────────────────────────
    static void BuildGolem()
    {
        const string fbxPath    = "Assets/Bosses/golem_boss/Characters/Humanoid Giant/Models/GiantModel01 - Golem.fbx";
        const string ctrlPath   = "Assets/Bosses/Animators/Golem_AC.controller";
        const string prefabDest = "Assets/Bosses/Prefabs/GolemBoss.prefab";

        var fbx  = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
        var mem  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/memories/Prefabs/MemoryPickup.prefab");

        if (fbx == null) { Debug.LogError("[BossPrefabBuilder] Golem FBX not found"); return; }

        Avatar avatar = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (a is Avatar av) { avatar = av; break; }

        var root = new GameObject("GolemBoss");

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;

        var nma = root.AddComponent<NavMeshAgent>();
        nma.speed            = 2f;
        nma.angularSpeed     = 120f;
        nma.stoppingDistance = 4f;
        nma.radius           = 1.5f;
        nma.height           = 5f;

        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 1.5f; col.height = 5f;
        col.center = new Vector3(0, 2.5f, 0);

        var script = root.AddComponent<GolemBossController>();
        script.memoryPrefab = mem;
        script.videoIndex   = 2;

        PrefabUtility.SaveAsPrefabAsset(root, prefabDest);
        Object.DestroyImmediate(root);
        Debug.Log("[BossPrefabBuilder] GolemBoss rebuilt");
    }

    // ──────────────────────────────────────────────────────────────────────────
    static void BuildKnight()
    {
        const string fbxPath    = "Assets/Bosses/knight_boss/Wraith_Knight.fbx";
        const string ctrlPath   = "Assets/Bosses/Animators/Knight_AC.controller";
        const string prefabDest = "Assets/Bosses/Prefabs/KnightBoss.prefab";

        var fbx  = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
        var mem  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/memories/Prefabs/MemoryPickup.prefab");

        if (fbx == null) { Debug.LogError("[BossPrefabBuilder] Wraith_Knight.fbx not found"); return; }

        var root = new GameObject("KnightBoss");

        // Knight is a static mesh — visual child
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;

        // Animator on root (no avatar — static mesh, animations are positional only)
        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var nma = root.AddComponent<NavMeshAgent>();
        nma.speed            = 6f;
        nma.angularSpeed     = 300f;
        nma.stoppingDistance = 2.5f;
        nma.radius           = 0.5f;
        nma.height           = 2f;

        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.5f; col.height = 2f;
        col.center = new Vector3(0, 1f, 0);

        var script = root.AddComponent<KnightBossController>();
        script.memoryPrefab = mem;
        script.videoIndex   = 3;

        PrefabUtility.SaveAsPrefabAsset(root, prefabDest);
        Object.DestroyImmediate(root);
        Debug.Log("[BossPrefabBuilder] KnightBoss rebuilt");
    }
}
