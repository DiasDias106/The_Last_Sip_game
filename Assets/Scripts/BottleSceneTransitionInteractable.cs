using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BottleSceneTransitionInteractable : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "masmorra_spawn";
    [SerializeField] private float interactionDistance = 2.1f;
    [SerializeField] private float blackScreenDuration = 2f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private string promptText = "Apanhar garrafa";
    [SerializeField] private AnimationClip drinkIntroClip;
    [SerializeField] private AnimationClip drinkLoopClip;
    [SerializeField] private AnimationClip collapseClip;

    private GameObject _player;
    private Camera _targetCamera;
    private GUIStyle _promptStyle;
    private bool _isPlayerNear;
    private bool _transitionStarted;

#if UNITY_EDITOR
    private const string DrinkIntroClipAssetPath = "Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Stand_Trans_Moonshine.FBX";
    private const string DrinkLoopClipAssetPath = "Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Moonshine_01_Drink_Loop.FBX";
    private const string FallClipAssetPath = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Knockdown1.FBX";
#endif

    private void Awake()
    {
        EnsureSequenceClips();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSequenceClips();
    }
#endif

    private void Update()
    {
        if (_transitionStarted || Time.timeScale <= 0f)
        {
            return;
        }

        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
            if (_player == null)
            {
                _isPlayerNear = false;
                return;
            }
        }

        float distSq = (transform.position - _player.transform.position).sqrMagnitude;
        _isPlayerNear = distSq <= interactionDistance * interactionDistance;

        if (_isPlayerNear && GetInteractPressed())
        {
            StartTransition();
        }
    }

    private void OnGUI()
    {
        if (!_isPlayerNear || _transitionStarted)
        {
            return;
        }

        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
            if (_targetCamera == null)
            {
                return;
            }
        }

        Vector3 screen = _targetCamera.WorldToScreenPoint(transform.position + promptOffset);
        if (screen.z <= 0f)
        {
            return;
        }

        EnsureStyle();
        string fullPrompt = BuildPromptText();
        Vector2 size = _promptStyle.CalcSize(new GUIContent(fullPrompt));
        Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y * 0.5f, size.x, size.y);
        GUI.Label(rect, fullPrompt, _promptStyle);
    }

    private void StartTransition()
    {
        _transitionStarted = true;
        DisableCollidersOnly();
        FacePlayerToBottle();

        UserImportedPlayerController controller = _player != null ? _player.GetComponent<UserImportedPlayerController>() : null;
        if (controller != null && (drinkIntroClip != null || drinkLoopClip != null))
        {
            controller.PlayBottleDrinkSequence(drinkIntroClip, drinkLoopClip, collapseClip, transform, BeginScreenTransition);
            return;
        }

        DisableVisualsAndColliders();
        BeginScreenTransition();
    }

    private void DisableVisualsAndColliders()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
    }

    private void DisableCollidersOnly()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
    }

    private void FacePlayerToBottle()
    {
        if (_player == null)
        {
            return;
        }

        Vector3 lookDir = transform.position - _player.transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f)
        {
            return;
        }

        _player.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    private void BeginScreenTransition()
    {
        DisableVisualsAndColliders();
        RuntimeSceneTransitionOverlay.BeginTransition(targetSceneName, blackScreenDuration);
    }

    private void EnsureSequenceClips()
    {
        if (collapseClip != null && collapseClip.name == "Unarmed-Fall")
        {
            collapseClip = null;
        }

        if (drinkIntroClip == null)
        {
            drinkIntroClip = FindRuntimeClip("MCU_am_Stand_Trans_Moonshine");
        }

        if (drinkLoopClip == null)
        {
            drinkLoopClip = FindRuntimeClip("MCU_am_Moonshine_01_Drink_Loop");
        }

        if (collapseClip == null)
        {
            collapseClip = FindRuntimeClip("Unarmed-Knockdown1");
        }

#if UNITY_EDITOR
        if (drinkIntroClip == null)
        {
            drinkIntroClip = FindClipAtPath(DrinkIntroClipAssetPath, "MCU_am_Stand_Trans_Moonshine");
        }

        if (drinkLoopClip == null)
        {
            drinkLoopClip = FindClipAtPath(DrinkLoopClipAssetPath, "MCU_am_Moonshine_01_Drink_Loop");
        }

        if (collapseClip == null)
        {
            collapseClip = FindClipAtPath(FallClipAssetPath, "Unarmed-Knockdown1");
        }
#endif
    }

    private static AnimationClip FindRuntimeClip(string clipName)
    {
        AnimationClip[] clips = Resources.FindObjectsOfTypeAll<AnimationClip>();
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }

    private void EnsureStyle()
    {
        if (_promptStyle != null)
        {
            return;
        }

        _promptStyle = new GUIStyle(GUI.skin.label);
        _promptStyle.fontSize = 24;
        _promptStyle.fontStyle = FontStyle.Bold;
        _promptStyle.normal.textColor = Color.white;
        _promptStyle.alignment = TextAnchor.MiddleCenter;
    }

    private string BuildPromptText()
    {
        string cleanPrompt = promptText ?? string.Empty;
        cleanPrompt = cleanPrompt.Replace("F - ", string.Empty).Replace("E - ", string.Empty).Trim();
        return MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact) + " - " + cleanPrompt;
    }

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.WasPressedThisFrame(MovementKeyBindings.GameAction.Interact);
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

#if UNITY_EDITOR
    private static AnimationClip FindClipAtPath(string assetPath, string preferredClipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && clip.name == preferredClipName)
            {
                return clip;
            }
        }

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }
#endif
}
