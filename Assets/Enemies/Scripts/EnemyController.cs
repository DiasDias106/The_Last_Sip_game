using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Detection")]
    public float detectionRange = 7f;
    public float attackRange    = 1.8f;

    [Header("Patrol")]
    public float patrolRadius   = 5f;
    public float patrolWaitTime = 3f;

    [Header("Stats")]
    public float maxHealth      = 40f;
    public float attackDamage   = 10f;
    public float attackCooldown = 1.5f;
    public float moveSpeed      = 2.5f;

    [Header("Death")]
    public float deathAnimDuration = 2f;
    public float shrinkDuration    = 0.6f;

    [Header("Rewards")]
    public int coinReward = 10;

    private enum State { Idle, Patrolling, Chasing, Attacking, Dead }
    private State state = State.Idle;

    private Animator     animator;
    private NavMeshAgent agent;
    private Transform    player;
    private float        health;
    private float        lastAttackTime;
    private float        patrolWaitTimer;
    private Vector3      patrolTarget;
    private Vector3      spawnPosition;
    private float        attackingExitTimer;

    private static readonly int HashIsWalking  = Animator.StringToHash("IsWalking");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");
    private static readonly int HashTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int HashDie        = Animator.StringToHash("Die");

    private void Awake()
    {
        animator      = GetComponent<Animator>();
        agent         = GetComponent<NavMeshAgent>();
        health        = maxHealth;
        spawnPosition = transform.position;
        agent.speed   = moveSpeed;
        agent.stoppingDistance = attackRange * 0.85f;
        patrolWaitTimer = patrolWaitTime;

        // Root motion fights NavMeshAgent and causes walking-in-place
        animator.applyRootMotion = false;

        // Put on Enemy layer so physics matrix prevents pushing the player
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) gameObject.layer = enemyLayer;

        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        PickNewPatrolTarget();
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (state != State.Chasing && state != State.Attacking)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    player = hit.transform;
                    state  = State.Chasing;
                    break;
                }
            }
        }

        switch (state)
        {
            case State.Idle:       UpdateIdle();    break;
            case State.Patrolling: UpdatePatrol();  break;
            case State.Chasing:    UpdateChase();   break;
            case State.Attacking:  UpdateAttack();  break;
        }
    }

    private void UpdateIdle()
    {
        animator.SetBool(HashIsWalking, false);
        patrolWaitTimer -= Time.deltaTime;
        if (patrolWaitTimer <= 0f)
        {
            PickNewPatrolTarget();
            state = State.Patrolling;
        }
    }

    private void UpdatePatrol()
    {
        if (!agent.isOnNavMesh) { state = State.Idle; return; }

        animator.SetBool(HashIsWalking, true);
        agent.isStopped = false;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            agent.SetDestination(patrolTarget);

        bool arrived = !agent.pathPending
                       && agent.remainingDistance <= agent.stoppingDistance + 0.2f
                       && agent.velocity.sqrMagnitude < 0.05f;
        if (arrived)
        {
            patrolWaitTimer = patrolWaitTime;
            state = State.Idle;
        }
    }

    private void UpdateChase()
    {
        if (player == null) { state = State.Idle; return; }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            state              = State.Attacking;
            attackingExitTimer = 0f;
            if (agent.isOnNavMesh) agent.isStopped = true;
            animator.SetBool(HashIsWalking, false);
        }
        else
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            animator.SetBool(HashIsWalking, true);
        }

        if (dist > detectionRange * 2f)
        {
            state  = State.Patrolling;
            player = null;
        }
    }

    private void UpdateAttack()
    {
        if (player == null) { state = State.Idle; return; }

        float dist = Vector3.Distance(transform.position, player.position);

        // Smooth rotation — instant snap every frame causes the trembling
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);

        attackingExitTimer += Time.deltaTime;

        // Require a minimum dwell time before leaving attack state to prevent flip-flop jitter
        bool canExit = attackingExitTimer >= 0.4f;

        if (canExit && dist > attackRange * 1.5f)
        {
            state = State.Chasing;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger(HashAttack);
            player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
        }
    }

    private void PickNewPatrolTarget()
    {
        for (int i = 1; i <= 8; i++)
        {
            Vector2 circle    = Random.insideUnitCircle.normalized * patrolRadius;
            Vector3 candidate = spawnPosition + new Vector3(circle.x, 0, circle.y);
            float   searchR   = patrolRadius * i * 0.5f;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchR, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 1.5f)
                {
                    patrolTarget = hit.position;
                    return;
                }
            }
        }
        patrolTarget = transform.position + transform.forward * patrolRadius * 0.5f;
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;
        health -= amount;
        animator.SetTrigger(HashTakeDamage);
        if (player == null)
        {
            player = FindPlayer();
            state  = State.Chasing;
        }
        if (health <= 0f) Die();
    }

    private void Die()
    {
        state = State.Dead;
        if (agent.isOnNavMesh) agent.isStopped = true;
        animator.SetTrigger(HashDie);
        PlayerInventory.Instance?.AddCoins(coinReward);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathAnimDuration);

        float   elapsed       = 0f;
        Vector3 originalScale = transform.localScale;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }
        Destroy(gameObject);
    }

    private Transform FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}
