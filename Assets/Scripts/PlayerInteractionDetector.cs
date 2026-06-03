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
    private GUIStyle _promptStyle;
[SerializeField] private float interactRange = 2.5f;

    
    static void EnsureHudExists()
    {
        if (PlayerHUDController.Instance != null)
            return;

        var hudGo = new GameObject("PlayerHUD");
        hudGo.AddComponent<PlayerHUDController>();
    }
void Update()
    {
                EnsureHudExists();
IInteractable closest = GetClosestInteractable();
        _currentClosest = closest;
        if (closest != null)
        {
            string keyName = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
            string prompt = UiText.IsPortuguese
                ? "Pressiona " + keyName + " para interagir"
                : "Press " + keyName + " to interact";
            PlayerHUDController.Instance?.ShowInteractionPrompt(prompt);
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
        if (_currentClosest == null)
            return;

        if (_promptStyle == null)
        {
            _promptStyle = new GUIStyle(GUI.skin.label);
            _promptStyle.fontSize = 22;
            _promptStyle.fontStyle = FontStyle.Bold;
            _promptStyle.alignment = TextAnchor.MiddleCenter;
            _promptStyle.normal.textColor = new Color(0.95f, 0.90f, 0.75f, 1f);
        }

        string keyName = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
        string text = UiText.IsPortuguese ? "Pressiona " + keyName + " para interagir" : "Press " + keyName + " to interact";
        Rect rect = new Rect((Screen.width - 520f) * 0.5f, Screen.height * 0.78f, 520f, 36f);
        GUI.Label(rect, text, _promptStyle);
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
