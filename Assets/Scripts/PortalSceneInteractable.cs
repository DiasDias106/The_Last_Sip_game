using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PortalSceneInteractable : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "level_1";
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private float blackScreenDuration = 0.35f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private string promptText = "Entrar";
    [SerializeField] private bool lockPortalWhenLevelCompleted = true;
    [SerializeField] private bool requireAllPrimaryLevelsCompleted;
    [SerializeField] private bool hideVortexWhenLocked = true;
    [SerializeField] private string lockedPromptText = "Nivel concluido";
    [SerializeField] private Color completedPortalColor = new Color(0.15f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color completedEmissionColor = new Color(0.1f, 1f, 0.25f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private float enterSoundVolume = 1.0f;

    private GameObject _player;
    private Camera _targetCamera;
    private GUIStyle _promptStyle;
    private bool _isPlayerNear;
    private bool _transitionStarted;
    private bool _isLocked;
    private static readonly string[] PrimaryLevelScenes = { "level_1", "level_2", "level_3" };
    private const float PromptMaxWidth = 220f;

    private void Awake()
    {
        RefreshCompletionState();
    }

    private void Update()
    {
        if (_transitionStarted || Time.timeScale <= 0f)
        {
            return;
        }

        if (_player == null)
        {
            _player = FindPlayerObject();
            if (_player == null)
            {
                _isPlayerNear = false;
                return;
            }
        }

        Vector3 portalPos = transform.position;
        Vector3 playerPos = _player.transform.position;
        portalPos.y = 0f;
        playerPos.y = 0f;
        float distSq = (portalPos - playerPos).sqrMagnitude;
        _isPlayerNear = distSq <= interactionDistance * interactionDistance;

        if (_isPlayerNear && !_isLocked && GetInteractPressed())
        {
            _transitionStarted = true;
            PlayEnterSound();
            StartSceneTransition();
        }
    }

    private void PlayEnterSound()
    {
        if (enterSound != null)
        {
            GameObject soundObj = new GameObject("PortalEnterSound");
            soundObj.transform.position = transform.position;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.spatialBlend = 0f; // Som 2D para se ouvir perfeito durante a transição
            source.volume = enterSoundVolume;
            source.clip = enterSound;
            source.Play();
            
            // Garantir que o som não é cortado a meio quando a cena nova acaba de carregar!
            Object.DontDestroyOnLoad(soundObj);
            Object.Destroy(soundObj, enterSound.length + 0.1f);
        }
    }

    private void OnGUI()
    {
        if (!_isPlayerNear || _transitionStarted || _isLocked)
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
        string currentPrompt = _isLocked ? UiText.LocalizeKnownText(lockedPromptText) : BuildPromptText();
        GUIContent content = new GUIContent(currentPrompt);
        float width = Mathf.Min(PromptMaxWidth, Mathf.Max(120f, _promptStyle.CalcSize(content).x));
        float height = _promptStyle.CalcHeight(content, width);
        Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);
        GUI.Label(rect, currentPrompt, _promptStyle);
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

    private void StartSceneTransition()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            _transitionStarted = false;
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            PlayerHealth.PreserveCurrentForSceneTransition(targetSceneName);
            RuntimeSceneTransitionOverlay.TransitionInstantlyToLoading(targetSceneName);
            return;
        }

        string candidatePath = targetSceneName.EndsWith(".unity")
            ? targetSceneName
            : $"Assets/Scenes/{targetSceneName}.unity";

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(candidatePath);
        if (buildIndex >= 0)
        {
            string resolvedSceneName = System.IO.Path.GetFileNameWithoutExtension(candidatePath);
            PlayerHealth.PreserveCurrentForSceneTransition(resolvedSceneName);
            RuntimeSceneTransitionOverlay.TransitionInstantlyToLoading(resolvedSceneName);
            return;
        }

        Debug.LogWarning($"PortalSceneInteractable could not resolve scene '{targetSceneName}'.");
        _transitionStarted = false;
    }

    private void RefreshCompletionState()
    {
        bool gateByAllLevels = requireAllPrimaryLevelsCompleted || IsFinalBossPortal();
        if (gateByAllLevels)
        {
            _isLocked = !ArePrimaryLevelsCompleted();
            ApplyBossPortalLockVisual(_isLocked);
        }
        else
        {
            _isLocked = lockPortalWhenLevelCompleted
                && !string.IsNullOrWhiteSpace(targetSceneName)
                && LevelCompletionState.IsCompleted(targetSceneName);
            if (_isLocked)
            {
                ApplyCompletedVisual();
            }
        }
    }

    private bool IsFinalBossPortal()
    {
        return !string.IsNullOrWhiteSpace(targetSceneName)
               && targetSceneName.Trim().ToLowerInvariant() == "boss_room";
    }

    private static bool ArePrimaryLevelsCompleted()
    {
        for (int i = 0; i < PrimaryLevelScenes.Length; i++)
        {
            if (!LevelCompletionState.IsCompleted(PrimaryLevelScenes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyBossPortalLockVisual(bool locked)
    {
        if (!hideVortexWhenLocked)
        {
            return;
        }

        Transform portalCenter = transform.Find("Portal.obj.cleaner.materialmerger.gles/centro_vortice");
        if (portalCenter == null)
        {
            return;
        }

        Renderer[] renderers = portalCenter.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = !locked;
            }
        }
    }

    private void ApplyCompletedVisual()
    {
        Transform portalCenter = transform.Find("Portal.obj.cleaner.materialmerger.gles/centro_vortice");
        Renderer[] renderers = portalCenter != null
            ? portalCenter.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", completedPortalColor);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", completedPortalColor);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", completedEmissionColor);
                }
            }

            renderer.materials = materials;
        }
    }

    private static GameObject FindPlayerObject()
    {
        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        UserImportedPlayerController importedPlayer = Object.FindFirstObjectByType<UserImportedPlayerController>();
        if (importedPlayer != null)
        {
            return importedPlayer.gameObject;
        }

        CharacterController controller = Object.FindFirstObjectByType<CharacterController>();
        if (controller != null)
        {
            return controller.gameObject;
        }

        return null;
    }

    private string BuildPromptText()
    {
        string cleanPrompt = promptText ?? string.Empty;
        cleanPrompt = cleanPrompt.Replace("F - ", string.Empty).Replace("E - ", string.Empty).Trim();
        cleanPrompt = UiText.LocalizeKnownText(cleanPrompt);
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
}
