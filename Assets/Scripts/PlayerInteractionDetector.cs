using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractionDetector : MonoBehaviour
{
    [SerializeField] private float interactRange = 2.5f;

    void Update()
    {
        if (!GetInteractPressed()) return;

        IInteractable closest  = null;
        float         bestDist = interactRange;

        foreach (var col in Physics.OverlapSphere(transform.position, interactRange))
        {
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < bestDist) { bestDist = d; closest = interactable; }
        }

        closest?.Interact();
    }

    static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.fKey.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
