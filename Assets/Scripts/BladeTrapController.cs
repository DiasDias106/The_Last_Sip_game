using UnityEngine;

[DisallowMultipleComponent]
public class BladeTrapController : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField] private float swingDuration = 1.6f;
    [SerializeField] private float maxSwingAngle = 18f;
    [SerializeField] private bool startRotatingPositive = true;

    [Header("Damage")]
    [SerializeField] private bool instantKill = true;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageCooldown = 0.6f;

    private readonly Collider[] overlapResults = new Collider[8];

    private Quaternion startLocalRotation;
    private Collider cachedCollider;
    private float swingProgress;
    private int swingDirection;
    private float nextDamageTime;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        InitializeState();
    }

    private void OnEnable()
    {
        cachedCollider = GetComponent<Collider>();
        InitializeState();
    }

    private void Update()
    {
        if (swingDuration <= 0.01f)
        {
            swingDuration = 0.01f;
        }

        swingProgress += swingDirection * (Time.deltaTime / swingDuration);
        if (swingProgress >= 1f)
        {
            swingProgress = 1f;
            swingDirection = -1;
        }
        else if (swingProgress <= 0f)
        {
            swingProgress = 0f;
            swingDirection = 1;
        }

        float easedProgress = Mathf.SmoothStep(0f, 1f, swingProgress);
        float currentAngle = Mathf.Lerp(-maxSwingAngle, maxSwingAngle, easedProgress);
        transform.localRotation = startLocalRotation * Quaternion.Euler(0f, 0f, currentAngle);
        TryDamagePlayer();
    }

    private void InitializeState()
    {
        startLocalRotation = transform.localRotation;
        swingProgress = startRotatingPositive ? 0f : 1f;
        swingDirection = startRotatingPositive ? 1 : -1;
        nextDamageTime = 0f;
    }

    private void TryDamagePlayer()
    {
        if (cachedCollider == null || Time.time < nextDamageTime)
        {
            return;
        }

        Bounds bounds = cachedCollider.bounds;
        int hitCount = Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            overlapResults,
            transform.rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

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
