using UnityEngine;
using System.Collections;

public class MinotaurBossController : BossController
{
    [Header("Minotaur")]
    public float runDistanceThreshold = 5f;

    protected override void Awake()
    {
        maxHealth      = 400f;
        attackDamage   = 30f;
        moveSpeed      = 5f;
        detectionRange = 18f;
        attackRange    = 3f;
        attackCooldown = 2.5f;
        deathDuration  = 3f;
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        // Shift between walk and run speed based on distance
        if (!isDead && player != null && agent.enabled && agent.isOnNavMesh)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            agent.speed = dist > runDistanceThreshold ? moveSpeed : moveSpeed * 0.5f;
        }
    }

    protected override void PerformAttack()
    {
        int attack = Random.Range(1, 6);
        switch (attack)
        {
            case 1: anim.SetTrigger(HashAttack1); break;
            case 2: anim.SetTrigger(HashAttack2); break;
            case 3: anim.SetTrigger(HashAttack3); break;
            case 4: anim.SetTrigger(HashAttack4); break;
            default: anim.SetTrigger(HashAttack5); break;
        }
        StartCoroutine(DealDamageDelayed(0.5f));
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
