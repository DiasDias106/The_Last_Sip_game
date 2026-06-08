using UnityEngine;

[DisallowMultipleComponent]
public class SpikeTrapController : MonoBehaviour
{
    [SerializeField] private bool instantKill = true;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageCooldown = 0.4f;

    private readonly Collider[] overlapResults = new Collider[8];
    private BoxCollider cachedBoxCollider;
    private float nextDamageTime;

    private void Awake()
    {
        cachedBoxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        TryDamagePlayerInArea();
    }

    private void TryDamagePlayerInArea()
    {
        if (cachedBoxCollider == null || Time.time < nextDamageTime)
        {
            return;
        }

        Bounds bounds = cachedBoxCollider.bounds;
        Vector3 halfExtents = new Vector3(
            bounds.extents.x * 0.95f,
            Mathf.Max(bounds.extents.y, 1f),
            bounds.extents.z * 0.95f);

        int hitCount = Physics.OverlapBoxNonAlloc(
            bounds.center + Vector3.up * 0.5f,
            halfExtents,
            overlapResults,
            transform.rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapResults[i];
            if (other == null)
            {
                continue;
            }

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsDead)
            {
                continue;
            }

            float damageToApply = instantKill ? playerHealth.CurrentHealth + playerHealth.maxHealth : damage;
            playerHealth.TakeDamage(damageToApply);
            nextDamageTime = Time.time + damageCooldown;
            return;
        }
    }
}
