using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public class PlayerInteractionDetector : MonoBehaviour
{
    void Start()
    {
        EnsureHudExists();
    }
    private IInteractable _currentClosest;
    [SerializeField] private float interactRange = 2.5f;
    static void EnsureHudExists()
    {
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenuScene" || sceneName == "beco" || sceneName == "beco_noite"
            || sceneName == "beco_dia" || sceneName == "new_game_controls")
            return;
        if (PlayerHUDController.Instance == null)
        {
            var hudGo = new GameObject("PlayerHUD");
            hudGo.AddComponent<PlayerHUDController>();
        }
        if (PlayerInventory.Instance == null)
        {
            var invGo = new GameObject("PlayerInventory");
            invGo.AddComponent<PlayerInventory>();
        }
    }
void Update()
    {
                EnsureHudExists();
IInteractable closest = GetClosestInteractable();
        _currentClosest = closest;
        if (closest != null)
        {
            PlayerHUDController.Instance?.HideInteractionPrompt();
        }
        else
        {
            PlayerHUDController.Instance?.HideInteractionPrompt();
        }
        if (!GetInteractPressed() || closest == null) return;
        closest.Interact();
    }
    IInteractable GetClosestInteractable()
    {
        IInteractable closest = null;
        float bestDist = interactRange;
        foreach (var col in Physics.OverlapSphere(transform.position, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;
            Vector3 closestPoint = col.ClosestPoint(transform.position);
            float d = Vector3.Distance(transform.position, closestPoint);
            if (d < bestDist)
            {
                bestDist = d;
                closest = interactable;
            }
        }
        if (closest != null)
            return closest;
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IInteractable interactable || !interactable.CanInteract)
                continue;
            float d = Vector3.Distance(transform.position, behaviour.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                closest = interactable;
            }
        }
        _currentClosest = closest;
return closest;
    }
    void OnGUI()
    {
        if (Time.timeScale <= 0f) return;
        if (_currentClosest != null)
        {
            var interactableMono = _currentClosest as MonoBehaviour;
            if (interactableMono != null)
            {
                Vector3 worldPos = interactableMono.transform.position + Vector3.up * 0.4f;
                if (Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                    if (screenPos.z > 0)
                    {
                        string keyName = "F";
                        #if ENABLE_INPUT_SYSTEM
                        MovementKeyBindings.EnsureLoaded();
                        keyName = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
                        #endif
                        GUIStyle style = new GUIStyle();
                        style.fontSize = 28;
                        style.fontStyle = FontStyle.Bold;
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = Color.black;
                        GUI.Label(new Rect(screenPos.x - 24, Screen.height - screenPos.y - 24, 50, 50), keyName, style);
                        style.normal.textColor = Color.white;
                        GUI.Label(new Rect(screenPos.x - 25, Screen.height - screenPos.y - 25, 50, 50), keyName, style);
                    }
                }
            }
        }
    }
static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.WasPressedThisFrame(MovementKeyBindings.GameAction.Interact);
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
