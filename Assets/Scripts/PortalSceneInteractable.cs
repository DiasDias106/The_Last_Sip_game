using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PortalSceneInteractable : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "level_1";
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private string promptText = "F - Entrar portal";
    [SerializeField] private bool lockPortalWhenLevelCompleted = true;
    [SerializeField] private string lockedPromptText = "Nivel concluido";
    [SerializeField] private Color completedPortalColor = new Color(0.15f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color completedEmissionColor = new Color(0.1f, 1f, 0.25f, 1f);

    private GameObject _player;
    private Camera _targetCamera;
    private GUIStyle _promptStyle;
    private bool _isPlayerNear;
    private bool _transitionStarted;
    private bool _isLocked;

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
            LoadTargetScene();
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
        string currentPrompt = _isLocked ? lockedPromptText : promptText;
        Vector2 size = _promptStyle.CalcSize(new GUIContent(currentPrompt));
        Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y * 0.5f, size.x, size.y);
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
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            _transitionStarted = false;
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        string candidatePath = targetSceneName.EndsWith(".unity")
            ? targetSceneName
            : $"Assets/Scenes/{targetSceneName}.unity";

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(candidatePath);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

        Debug.LogWarning($"PortalSceneInteractable could not resolve scene '{targetSceneName}'.");
        _transitionStarted = false;
    }

    private void RefreshCompletionState()
    {
        _isLocked = lockPortalWhenLevelCompleted
            && !string.IsNullOrWhiteSpace(targetSceneName)
            && PlayerPrefs.GetInt(LevelCompletionState.GetKey(targetSceneName), 0) == 1;

        if (_isLocked)
        {
            ApplyCompletedVisual();
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

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
