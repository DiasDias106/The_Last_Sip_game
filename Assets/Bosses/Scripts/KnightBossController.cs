using UnityEngine;
using System.Collections;

public class KnightBossController : BossController
{
    [Header("Knight")]
    private int comboHits;

    protected override void Awake()
    {
        maxHealth      = 250f;
        attackDamage   = 22f;
        moveSpeed      = 6f;
        detectionRange = 15f;
        attackRange    = 2.5f;
        attackCooldown = 1.8f;
        deathDuration  = 5f;
        base.Awake();
    }

    protected override void PerformAttack()
    {
        comboHits++;
        if (comboHits >= 3)
        {
            // Jump attack every 3rd hit
            anim.SetTrigger(HashAttack2);
            comboHits = 0;
        }
        else
        {
            anim.SetTrigger(HashAttack1);
        }

        StartCoroutine(DealDamageDelayed(0.4f));
    }

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player == null) yield break;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange + 1f)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
        }
    }
}
