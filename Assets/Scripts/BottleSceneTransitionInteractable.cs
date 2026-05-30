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
    private const float PromptMaxWidth = 220f;

#if UNITY_EDITOR
    // Drink intro: Stunned do ExplosiveLLC (funciona no player) → simula embriaguez
    private const string DrinkIntroClipAssetPath = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Stunned.FBX";
    // Drink loop: MoCap Central moonshine
    private const string DrinkLoopClipAssetPath  = "Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Moonshine_01_Drink_Loop.FBX";
    // Collapse: animação de cair do MoCap Central Injured
    private const string FallClipAssetPath       = "Assets/MoCapCentral/MC_Sample/Animations/Injured/MCU_am_InjuredBelly_DropToFloor.FBX";
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
        GUIContent content = new GUIContent(fullPrompt);
        float width = Mathf.Min(PromptMaxWidth, Mathf.Max(120f, _promptStyle.CalcSize(content).x));
        float height = _promptStyle.CalcHeight(content, width);
        Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);
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
        SaveManager.SaveActiveSlotFromCurrentScene(targetSceneName);
        RuntimeSceneTransitionOverlay.BeginTransition(targetSceneName, blackScreenDuration);
    }

    private void EnsureSequenceClips()
    {
        // Reset clips antigos incompatíveis
        if (drinkIntroClip != null && drinkIntroClip.name == "MCU_am_Stand_Trans_Moonshine")
            drinkIntroClip = null;
        if (collapseClip != null && (collapseClip.name == "Unarmed-Fall" || collapseClip.name == "Unarmed-Knockdown1"))
            collapseClip = null;

        if (drinkIntroClip == null)
            drinkIntroClip = FindRuntimeClip("Unarmed-Stunned");

        if (drinkLoopClip == null)
            drinkLoopClip = FindRuntimeClip("MCU_am_Moonshine_01_Drink_Loop");

        if (collapseClip == null)
            collapseClip = FindRuntimeClip("MCU_am_InjuredBelly_DropToFloor");

#if UNITY_EDITOR
        if (drinkIntroClip == null)
            drinkIntroClip = FindClipAtPath(DrinkIntroClipAssetPath, "Unarmed-Stunned");

        if (drinkLoopClip == null)
            drinkLoopClip = FindClipAtPath(DrinkLoopClipAssetPath, "MCU_am_Moonshine_01_Drink_Loop");

        if (collapseClip == null)
            collapseClip = FindClipAtPath(FallClipAssetPath, "MCU_am_InjuredBelly_DropToFloor");
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
        _promptStyle.wordWrap = true;
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
