using UnityEngine;
using UnityEngine.AI;

public enum BossState { Sleeping, WakingUp, Chasing, Attacking, Dead }

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class BossController : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackRange = 2f;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float attackDamage = 15f;
    public float attackCooldown = 2f;
    public float moveSpeed = 3f;

    [Header("Wake-up")]
    public float wakeUpDuration = 2.5f;

    protected BossState currentState = BossState.Sleeping;
    protected Animator animator;
    protected NavMeshAgent agent;
    protected Transform player;
    protected float health;
    protected float lastAttackTime;

    protected static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    protected static readonly int HashAttack    = Animator.StringToHash("Attack");
    protected static readonly int HashWakeUp    = Animator.StringToHash("WakeUp");
    protected static readonly int HashDie       = Animator.StringToHash("Die");

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        agent    = GetComponent<NavMeshAgent>();
        health   = maxHealth;
        agent.speed            = moveSpeed;
        agent.stoppingDistance = attackRange * 0.85f;
        agent.isStopped        = true;
    }

    protected virtual void Update()
    {
        if (currentState == BossState.Dead || currentState == BossState.WakingUp)
            return;

        if (currentState == BossState.Sleeping)
        {
            ScanForPlayer();
            return;
        }

        if (player == null)
        {
            player = FindPlayer();
            if (player == null) return;
        }

        UpdateCombat();
    }

    private void ScanForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                StartWakeUp();
                return;
            }
        }
    }

    private Transform FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }

    private void StartWakeUp()
    {
        currentState = BossState.WakingUp;
        animator.SetTrigger(HashWakeUp);
        Invoke(nameof(FinishWakeUp), wakeUpDuration);
    }

    private void FinishWakeUp()
    {
        currentState    = BossState.Chasing;
        agent.isStopped = false;
    }

    private void UpdateCombat()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (currentState == BossState.Chasing)
        {
            if (agent.isOnNavMesh)
                agent.SetDestination(player.position);
            animator.SetBool(HashIsWalking, true);

            if (dist <= attackRange)
            {
                currentState    = BossState.Attacking;
                agent.isStopped = true;
                animator.SetBool(HashIsWalking, false);
            }
        }
        else if (currentState == BossState.Attacking)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            if (dist > attackRange * 1.2f)
            {
                currentState    = BossState.Chasing;
                agent.isStopped = false;
            }
            else if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
    }

    protected virtual void PerformAttack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(HashAttack);
        ApplyMeleeDamage(attackRange);
    }

    protected void ApplyMeleeDamage(float range)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Wire up player health here: hit.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (currentState == BossState.Dead) return;
        health -= amount;
        if (health <= 0f) Die();
    }

    protected virtual void Die()
    {
        currentState    = BossState.Dead;
        agent.isStopped = true;
        animator.SetTrigger(HashDie);
        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
