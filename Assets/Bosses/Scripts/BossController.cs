using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class BossController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 300f;
    public float attackDamage = 25f;
    public float moveSpeed = 4f;
    public float attackCooldown = 2f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 2.5f;

    [Header("Death")]
    public float deathDuration = 4f;

    [Header("Memory Drop")]
    public GameObject memoryPrefab;
    public int videoIndex = 1;

    protected static readonly int HashSpeed      = Animator.StringToHash("Speed");
    protected static readonly int HashAttack1    = Animator.StringToHash("Attack1");
    protected static readonly int HashAttack2    = Animator.StringToHash("Attack2");
    protected static readonly int HashAttack3    = Animator.StringToHash("Attack3");
    protected static readonly int HashAttack4    = Animator.StringToHash("Attack4");
    protected static readonly int HashAttack5    = Animator.StringToHash("Attack5");
    protected static readonly int HashTakeDamage = Animator.StringToHash("TakeDamage");
    protected static readonly int HashDie        = Animator.StringToHash("Die");

    protected Animator anim;
    protected NavMeshAgent agent;
    protected Transform player;
    protected float currentHealth;
    protected bool isDead;
    protected float attackTimer;

    protected virtual void Awake()
    {
        anim          = GetComponent<Animator>();
        agent         = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        agent.speed   = moveSpeed;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        attackTimer -= Time.deltaTime;
        DetectPlayer();

        if (player == null)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            anim.SetFloat(HashSpeed, 0f, 0.2f, Time.deltaTime);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > detectionRange * 1.5f)
        {
            player = null;
            return;
        }

        if (dist <= attackRange && attackTimer <= 0f)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            anim.SetFloat(HashSpeed, 0f, 0.1f, Time.deltaTime);
            FaceTarget(player.position);
            PerformAttack();
            attackTimer = attackCooldown;
        }
        else if (dist > attackRange)
        {
            if (agent.isOnNavMesh)
                agent.SetDestination(player.position);
            float normalizedSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
            anim.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }

    void DetectPlayer()
    {
        if (player != null) return;
        var cols = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (var col in cols)
        {
            if (col.CompareTag("Player"))
            {
                player = col.transform;
                break;
            }
        }
    }

    void FaceTarget(Vector3 target)
    {
        var dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);
    }

    protected abstract void PerformAttack();

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0f)
            Die();
        else
            anim.SetTrigger(HashTakeDamage);
    }

    protected virtual void Die()
    {
        isDead = true;
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;
        anim.SetTrigger(HashDie);
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDuration);

        if (memoryPrefab != null)
        {
            var spawnPos = transform.position + Vector3.up * 1f;
            var mem = Instantiate(memoryPrefab, spawnPos, Quaternion.identity);
            var pickup = mem.GetComponent<MemoryPickup>();
            if (pickup != null) pickup.videoIndex = videoIndex;
        }

        Destroy(gameObject);
    }
}
