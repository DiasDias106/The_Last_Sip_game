using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class LimoDaySequenceTrigger : MonoBehaviour
{
    [SerializeField] private LimoDayInteractable limoDayInteractable;

    private bool _triggered;

    private void Reset()
    {
        if (TryGetComponent<BoxCollider>(out BoxCollider col))
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (TryGetComponent<BoxCollider>(out BoxCollider col))
            col.isTrigger = true;

        if (limoDayInteractable == null)
            limoDayInteractable = Object.FindFirstObjectByType<LimoDayInteractable>();
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
        if (_triggered || limoDayInteractable == null) return;
        if (!other.CompareTag("Player")) return;

        UserImportedPlayerController ctrl = other.GetComponent<UserImportedPlayerController>()
            ?? other.GetComponentInParent<UserImportedPlayerController>();
        if (ctrl == null) return;

        _triggered = true;
        limoDayInteractable.TryBeginAutoWalk();
    }
}
