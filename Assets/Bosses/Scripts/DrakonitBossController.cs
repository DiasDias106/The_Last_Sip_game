using UnityEngine;
using System.Collections;

public class DrakonitBossController : BossController
{
    protected override void Awake()
    {
        maxHealth      = 50f;
        attackDamage   = 38f;
        moveSpeed      = 3.5f;
        detectionRange = 16f;
        attackRange    = 2.5f;
        attackCooldown = 2.2f;
        deathDuration  = 5f;
        patrolRadius   = 9f;
        base.Awake();
    }

    protected override void PerformAttack()
    {
        if (Random.value > 0.4f)
            anim.SetTrigger(HashAttack1);
        else
            anim.SetTrigger(HashAttack2);

        StartCoroutine(DealDamageDelayed(0.6f));
    }

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player == null) yield break;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 1f)
        {
            var ph = player.GetComponentInParent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>();
            if (ph != null) ph.TakeDamage(attackDamage);
        }
    }
}
