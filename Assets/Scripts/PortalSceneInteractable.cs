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

    private GameObject _player;
    private Camera _targetCamera;
    private GUIStyle _promptStyle;
    private bool _isPlayerNear;
    private bool _transitionStarted;

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

        if (_isPlayerNear && GetInteractPressed())
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
        Vector2 size = _promptStyle.CalcSize(new GUIContent(promptText));
        Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y * 0.5f, size.x, size.y);
        GUI.Label(rect, promptText, _promptStyle);
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
