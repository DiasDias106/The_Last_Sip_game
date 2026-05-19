using UnityEngine;

public class Boss2Controller : BossController
{
    [Header("Ranged Attack")]
    public float rangedRange     = 10f;
    public float rangedCooldown  = 4f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;

    private float lastRangedTime = -10f;
    private static readonly int HashRangedAttack = Animator.StringToHash("RangedAttack");

    protected override void PerformAttack()
    {
        lastAttackTime = Time.time;
        float dist = Vector3.Distance(transform.position, player.position);
        bool canRanged = dist > attackRange && Time.time >= lastRangedTime + rangedCooldown;

        if (canRanged)
        {
            lastRangedTime = Time.time;
            animator.SetTrigger(HashRangedAttack);
            ShootProjectile();
        }
        else
        {
            animator.SetTrigger(HashAttack);
            ApplyMeleeDamage(attackRange);
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || player == null) return;
        Vector3 origin = transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;
        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (player.position + Vector3.up - origin).normalized;
            rb.linearVelocity = dir * projectileSpeed;
        }
        Destroy(proj, 6f);
    }
}
