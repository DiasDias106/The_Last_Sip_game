using UnityEngine;
[DisallowMultipleComponent]
public class ArrowTrapDamageProxy : MonoBehaviour
{
    [SerializeField] private ArrowTrapController owner;
    public void Initialize(ArrowTrapController trapOwner)
    {
        owner = trapOwner;
    }
    private void OnTriggerEnter(Collider other)
    {
        TryNotify(other);
    }
    private void OnTriggerStay(Collider other)
    {
        TryNotify(other);
    }
    private void TryNotify(Collider other)
    {
        if (owner == null || other == null)
        {
            return;
        }
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            owner.NotifyHit(playerHealth);
        }
    }
}
