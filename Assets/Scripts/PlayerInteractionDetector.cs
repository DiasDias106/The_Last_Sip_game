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
