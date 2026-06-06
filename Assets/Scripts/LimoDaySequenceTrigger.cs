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
        // Sequence is now triggered by F key inside LimoDayInteractable — nothing to do here.
    }
}
