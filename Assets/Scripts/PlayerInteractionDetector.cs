using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractionDetector : MonoBehaviour
{
    [SerializeField] private float interactRange = 2.5f;

    void Update()
    {
        IInteractable closest = GetClosestInteractable();
        if (closest != null)
        {
            string keyName = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
            PlayerHUDController.Instance?.ShowInteractionPrompt("Pressiona " + keyName + " para interagir");
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

        foreach (var col in Physics.OverlapSphere(transform.position, interactRange))
        {
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < bestDist) { bestDist = d; closest = interactable; }
        }

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
