using UnityEngine;
using System.Collections;

public class Boss1Controller : BossController
{
    [Header("Charge Attack")]
    public float chargeSpeed    = 9f;
    public float chargeDuration = 0.7f;
    public float chargeCooldown = 5f;

    private float lastChargeTime = -10f;
    private static readonly int HashCharge = Animator.StringToHash("Charge");

    protected override void PerformAttack()
    {
        lastAttackTime = Time.time;
        bool canCharge = Time.time >= lastChargeTime + chargeCooldown;

        if (canCharge)
        {
            lastChargeTime = Time.time;
            animator.SetTrigger(HashCharge);
            StartCoroutine(ChargeRoutine());
        }
        else
        {
            animator.SetTrigger(HashAttack);
            ApplyMeleeDamage(attackRange);
        }
    }

    private IEnumerator ChargeRoutine()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            float origSpeed = agent.speed;
            agent.speed = chargeSpeed;
            agent.SetDestination(player.position);
            yield return new WaitForSeconds(chargeDuration);
            ApplyMeleeDamage(attackRange * 1.5f);
            agent.speed = origSpeed;
        }
        currentState = BossState.Chasing;
    }
}
