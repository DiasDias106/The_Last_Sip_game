using UnityEngine;
using System.Collections;

public class DemonBossController : BossController
{
    protected override void Awake()
    {
        maxHealth      = 300f;
        attackDamage   = 28f;
        moveSpeed      = 4.5f;
        detectionRange = 15f;
        attackRange    = 2f;
        attackCooldown = 1.8f;
        deathDuration  = 4f;
        patrolRadius   = 8f;
        base.Awake();
    }

    protected override void PerformAttack()
    {
        if (Random.value > 0.5f)
            anim.SetTrigger(HashAttack1);
        else
            anim.SetTrigger(HashAttack2);

        StartCoroutine(DealDamageDelayed(0.5f));
    }

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player == null) yield break;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 1f)
            player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
    }
}
