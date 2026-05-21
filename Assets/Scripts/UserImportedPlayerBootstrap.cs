using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class UserImportedPlayerBootstrap
{
    private const string SceneName = "level_1";
    private const string CharacterResourcePath = "UserImportedCharacter/Paladin WProp J Nordstrom";
    private static readonly Vector3 FallbackSpawn = new Vector3(-8.48f, 2.2f, 69.553f);
    private static bool _levelCollidersEnsured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "masmorra_spawn")
        {
            SetupTavernShop();
        }
        else if (sceneName == "level_1")
        {
            EnsureLevel1Colliders();
        }

        EnsurePlayerSpawned();
    }

    private static void EnsureLevel1Colliders()
    {
        // level_1 relies on auto-generated colliders for its floor/walls.
        MeshFilter[] meshes = Object.FindObjectsOfType<MeshFilter>();
        foreach (MeshFilter mf in meshes)
        {
            if (mf.gameObject.GetComponent<Collider>() == null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    public static void EnsurePlayerSpawned()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != SceneName && currentScene != "masmorra_spawn")
        {
            return;
        }

        if (Object.FindObjectOfType<UserImportedPlayerController>() != null)
        {
            return;
        }

        EnsureEventSystem();
        HideScenePreviewCharacter();

        GameObject modelPrefab = Resources.Load<GameObject>(CharacterResourcePath);
        if (modelPrefab == null)
        {
            Debug.LogError("Could not load Paladin model from Resources/UserImportedCharacter.");
            return;
        }

        Vector3 requested = GetRequestedSpawnPoint(out bool fromBase, out Quaternion spawnRot);
        Vector3 spawn = fromBase ? requested : ResolveSpawnPoint(requested);
        EnsureSafetyPlatform(spawn);

        GameObject playerRoot = new GameObject("UserImportedPlayer");
        playerRoot.transform.position = spawn;
        playerRoot.transform.rotation = spawnRot;

        CharacterController cc = playerRoot.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.03f;
        cc.minMoveDistance = 0f;

        GameObject visual = Object.Instantiate(modelPrefab, playerRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        UserImportedPlayerController controller = playerRoot.AddComponent<UserImportedPlayerController>();
        controller.AssignVisualRoot(visual.transform);

        playerRoot.AddComponent<DoorInteractor>();
        playerRoot.AddComponent<PlayerHealth>();
        playerRoot.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) playerRoot.layer = playerLayer;
        visual.AddComponent<VisualAnimationEventReceiver>();

        EnsureCharacterMaterialsVisible(visual);

    }

    private static void HideScenePreviewCharacter()
    {
        GameObject preview = GameObject.Find("PaladinOnBase");
        if (preview != null && preview.activeSelf)
        {
            preview.SetActive(false);
        }
    }

    private static void EnsureCharacterMaterialsVisible(GameObject visualRoot)
    {
#if UNITY_EDITOR
        if (ApplyTexturedProjectMaterials(visualRoot))
        {
            return;
        }
#endif

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null)
                {
                    continue;
                }

                mat.shader = urpLit;
                if (!mat.HasProperty("_BaseColor"))
                {
                    continue;
                }

                if (mat.mainTexture == null)
                {
                    // Source pack has no albedo texture maps; use a neutral non-white tint.
                    mat.SetColor("_BaseColor", ResolveFallbackColor(renderer.name));
                }

                Color c = mat.GetColor("_BaseColor");
                if (c.a <= 0f)
                {
                    c.a = 1f;
                    mat.SetColor("_BaseColor", c);
                }
            }
            renderer.materials = mats;
        }
    }

#if UNITY_EDITOR
    private static bool ApplyTexturedProjectMaterials(GameObject visualRoot)
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UserImported/Paladin_Body_Textured.mat");
        Material metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UserImported/Paladin_Metal_Textured.mat");
        Material shield = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UserImported/Paladin_Shield_Textured.mat");
        Material sword = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UserImported/Paladin_Weapon_Textured.mat");
        if (body == null || metal == null || shield == null || sword == null)
        {
            return false;
        }

        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            Material chosen = body;
            string n = renderer.name.ToLowerInvariant();
            if (n.Contains("helmet"))
            {
                chosen = metal;
            }
            else if (n.Contains("shield"))
            {
                chosen = shield;
            }
            else if (n.Contains("sword"))
            {
                chosen = sword;
            }

            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = chosen;
            }
            renderer.materials = mats;
        }

        return true;
    }
#endif

    private static Color ResolveFallbackColor(string rendererName)
    {
        string n = rendererName.ToLowerInvariant();
        if (n.Contains("sword"))
        {
            return new Color(0.72f, 0.74f, 0.78f, 1f);
        }
        if (n.Contains("shield"))
        {
            return new Color(0.41f, 0.44f, 0.50f, 1f);
        }
        if (n.Contains("helmet"))
        {
            return new Color(0.60f, 0.63f, 0.68f, 1f);
        }
        return new Color(0.33f, 0.35f, 0.39f, 1f);
    }

    private static Vector3 ResolveSpawnPoint(Vector3 desired)
    {
        // Find real floor below desired spawn to prevent endless fall.
        Vector3 rayStart = desired + Vector3.up * 25f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 80f))
        {
            return hit.point + Vector3.up * 0.08f;
        }
        return desired;
    }

    private static Vector3 GetRequestedSpawnPoint(out bool fromBase, out Quaternion rot)
    {
        GameObject preview = FindInactiveObjectByName("PaladinOnBase");
        if (preview != null)
        {
            rot = preview.transform.rotation;
        }
        else
        {
            rot = Quaternion.Euler(0, 180, 0); // Default to 180 if requested
        }

        GameObject baseObj = FindInactiveObjectByName("Base");
        if (baseObj != null)
        {
            fromBase = true;
            return baseObj.transform.position + Vector3.up * 0.1f;
        }

        if (preview != null)
        {
            fromBase = true;
            return preview.transform.position + Vector3.up * 0.1f;
        }

        fromBase = false;
        return FallbackSpawn;
    }

    private static GameObject FindInactiveObjectByName(string name)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.isLoaded && go.name == name)
            {
                return go;
            }
        }
        return null;
    }

    private static void EnsureSafetyPlatform(Vector3 around)
    {
        // If the level has no collider below spawn, create a hidden fallback floor.
        if (_levelCollidersEnsured) return;

        if (!Physics.Raycast(around + Vector3.up, Vector3.down, out RaycastHit _, 50f))
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "SafetyPlatform";
            platform.transform.position = around - Vector3.up * 1.5f;
            platform.transform.localScale = new Vector3(50, 1, 50);
            platform.GetComponent<MeshRenderer>().enabled = false;
        }
        _levelCollidersEnsured = true;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();

        System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            eventSystemGo.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        Debug.Log("[UserImportedPlayerBootstrap] Added EventSystem because none was found in the scene.");
    }

    private static void SetupTavernShop()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "masmorra_spawn")
            return;

        GameObject cylinder = GameObject.Find("Cylinder.155");
        if (cylinder != null)
        {
            if (cylinder.GetComponent<ShopInteractable>() == null)
            {
                cylinder.AddComponent<ShopInteractable>();
                Debug.Log("[UserImportedPlayerBootstrap] ShopInteractable added to Cylinder.155");
            }
        }
        else
        {
            Debug.LogWarning("[UserImportedPlayerBootstrap] Cylinder.155 not found in masmorra_spawn!");
        }
    }
}
