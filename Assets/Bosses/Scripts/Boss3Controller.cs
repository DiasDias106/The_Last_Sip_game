using UnityEngine;
using System.Collections;

public class Boss3Controller : BossController
{
    [Header("Slam Attack")]
    public float slamRadius           = 3.5f;
    public float slamDamageMultiplier = 2.5f;
    public float slamCooldown         = 7f;

    private float lastSlamTime = -10f;
    private static readonly int HashSlam = Animator.StringToHash("Slam");

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 250f;
        health         = maxHealth;
        attackDamage   = 25f;
        moveSpeed      = 1.8f;
        agent.speed    = moveSpeed;
    }

    protected override void PerformAttack()
    {
        lastAttackTime = Time.time;

        if (Time.time >= lastSlamTime + slamCooldown)
        {
            lastSlamTime = Time.time;
            animator.SetTrigger(HashSlam);
            StartCoroutine(SlamRoutine());
        }
        else
        {
            animator.SetTrigger(HashAttack);
            ApplyMeleeDamage(attackRange);
        }
    }

    private IEnumerator SlamRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Wire up: hit.GetComponent<IDamageable>()?.TakeDamage(attackDamage * slamDamageMultiplier);
            }
        }
    }
}
