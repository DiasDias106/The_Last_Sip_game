using UnityEngine;
using System.Collections;

public class LavaGolemBossController : BossController
{
    protected override void Awake()
    {
        maxHealth      = 350f;
        attackDamage   = 40f;
        moveSpeed      = 2f;
        detectionRange = 14f;
        attackRange    = 2.5f;
        attackCooldown = 2.5f;
        deathDuration  = 3f;
        patrolRadius   = 7f;
        base.Awake();
    }

    protected override void PerformAttack()
    {
        anim.SetTrigger(HashAttack1);
        StartCoroutine(DealDamageDelayed(0.5f));
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
