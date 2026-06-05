using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class LimoDaySequenceTrigger : MonoBehaviour
{
    [SerializeField] private LimoDayInteractable limoDayInteractable;

    private bool _triggered;

    private void Reset()
    {
        if (TryGetComponent<BoxCollider>(out BoxCollider triggerCollider))
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (TryGetComponent<BoxCollider>(out BoxCollider triggerCollider))
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStart(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStart(other);
    }

    private void TryStart(Collider other)
    {
        if (_triggered || limoDayInteractable == null || other == null)
        {
            return;
        }

        UserImportedPlayerController controller = other.GetComponent<UserImportedPlayerController>()
            ?? other.GetComponentInParent<UserImportedPlayerController>();
        if (controller == null)
        {
            return;
        }

        if (limoDayInteractable.StartSequence(controller))
        {
            _triggered = true;
        }
    }
}
