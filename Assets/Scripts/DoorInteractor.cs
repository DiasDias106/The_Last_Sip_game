using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoorInteractor : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float maxScanDistance = 3f;

    private DoorInteractable _nearestDoor;
    private GUIStyle _promptStyle;
    private const float PromptMaxWidth = 360f;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        FindNearestDoor();
        if (_nearestDoor != null && GetInteractPressed())
        {
            _nearestDoor.Interact();
        }
    }

    private void FindNearestDoor()
    {
        _nearestDoor = null;
        float bestDistSq = float.MaxValue;
        Vector3 origin = transform.position;
        DoorInteractable[] doors = Object.FindObjectsOfType<DoorInteractable>(true);

        for (int i = 0; i < doors.Length; i++)
        {
            DoorInteractable door = doors[i];
            if (door == null || !door.isActiveAndEnabled)
            {
                continue;
            }

            float maxDoorDistance = Mathf.Min(maxScanDistance, door.InteractionDistance);
            float distSq = (door.transform.position - origin).sqrMagnitude;
            if (distSq > maxDoorDistance * maxDoorDistance)
            {
                continue;
            }

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                _nearestDoor = door;
            }
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f || _nearestDoor == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        Vector3 promptPos = GetDoorCenterWorld(_nearestDoor.gameObject);
        Vector3 screen = targetCamera.WorldToScreenPoint(promptPos);
        if (screen.z <= 0f)
        {
            return;
        }

        EnsureStyle();
        string text = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
        GUIContent content = new GUIContent(text);
        float width = Mathf.Min(PromptMaxWidth, Mathf.Max(180f, _promptStyle.CalcSize(content).x + 10f));
        float height = _promptStyle.CalcHeight(content, width) + 8f;
        float x = Mathf.Max(0f, screen.x - width * 0.5f);
        Rect rect = new Rect(x, Screen.height - screen.y - height * 0.5f, width, height);
        GUI.Label(rect, text, _promptStyle);
    }

    private static Vector3 GetDoorCenterWorld(GameObject door)
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return door.transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.center;
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
