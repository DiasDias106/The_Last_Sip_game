using UnityEngine;
using System.Collections;

public class GolemBossController : BossController
{
    protected override void Awake()
    {
        maxHealth      = 600f;
        attackDamage   = 50f;
        moveSpeed      = 2f;
        detectionRange = 12f;
        attackRange    = 4f;
        attackCooldown = 3f;
        deathDuration  = 5f;
        base.Awake();
    }

    protected override void PerformAttack()
    {
        if (Random.value > 0.5f)
            anim.SetTrigger(HashAttack1);
        else
            anim.SetTrigger(HashAttack2);

        StartCoroutine(DealDamageDelayed(0.7f));
    }

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player == null) yield break;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange + 1f)
        {
            var ph = player.GetComponentInParent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>();
            if (ph != null) ph.TakeDamage(attackDamage);
        }
    }
}
