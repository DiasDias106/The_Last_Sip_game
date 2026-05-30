using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BossController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 300f;
    public float attackDamage = 25f;
    public float moveSpeed = 4f;
    public float attackCooldown = 2f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 2.5f;

    [Header("Patrol")]
    public float patrolRadius   = 8f;
    public float patrolWaitTime = 4f;

    [Header("Death")]
    public float deathDuration = 4f;

    [Header("Memory Drop")]
    public GameObject memoryPrefab;
    public int videoIndex = 1;

    [Header("Rewards")]
    public int coinReward = 50;

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

    private enum PatrolState { Waiting, Moving }
    private PatrolState patrolState = PatrolState.Waiting;
    private Vector3     spawnPosition;
    private Vector3     patrolTarget;
    private float       patrolWaitTimer;
    private bool        bossHudShown;
    private bool        _combatMusicStarted;

    protected virtual void Awake()
    {
        var allAnims = GetComponentsInChildren<Animator>(true);
        anim = System.Array.Find(allAnims, a => a.runtimeAnimatorController != null)
               ?? (allAnims.Length > 0 ? allAnims[0] : null);
        agent         = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        agent.speed   = moveSpeed;
        anim.applyRootMotion = false;
        EnsureBossHud();
        BossHUDController.Instance?.Hide();

        spawnPosition   = transform.position;
        patrolWaitTimer = patrolWaitTime;

        // Snap to NavMesh if placed slightly off it
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                Debug.LogWarning($"[BossController] {name} is not on a NavMesh — bake the NavMesh in this scene.");
        }

        PickPatrolTarget();
    }

    protected virtual void Update()
    {
        if (isDead) return;
        attackTimer -= Time.deltaTime;
        DetectPlayer();

        if (player == null)
        {
            UpdatePatrol();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > detectionRange * 1.5f)
        {
            if (_combatMusicStarted) { _combatMusicStarted = false; GameMusicManager.Instance?.ExitBoss(); }
            player = null;
            return;
        }

        if (dist <= attackRange)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            anim.SetFloat(HashSpeed, 0f, 0.1f, Time.deltaTime);
            FaceTarget(player.position);
            if (attackTimer <= 0f)
            {
                PerformAttack();
                attackTimer = attackCooldown;
            }
        }
        else
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            float normalizedSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
            anim.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }

    private void UpdatePatrol()
    {
        if (!agent.isOnNavMesh) return;

        if (patrolState == PatrolState.Waiting)
        {
            agent.isStopped = true;
            anim.SetFloat(HashSpeed, 0f, 0.2f, Time.deltaTime);
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                agent.isStopped = false;
                agent.SetDestination(patrolTarget);
                patrolState = PatrolState.Moving;
            }
        }
        else
        {
            anim.SetFloat(HashSpeed,
                agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f),
                0.15f, Time.deltaTime);

            bool arrived = !agent.pathPending
                           && agent.remainingDistance <= agent.stoppingDistance + 0.3f
                           && agent.velocity.sqrMagnitude < 0.05f;
            if (arrived)
            {
                patrolWaitTimer = patrolWaitTime;
                patrolState     = PatrolState.Waiting;
                PickPatrolTarget();
            }
        }
    }

    private void PickPatrolTarget()
    {
        for (int i = 1; i <= 8; i++)
        {
            Vector2 circle    = Random.insideUnitCircle.normalized * patrolRadius;
            Vector3 candidate = spawnPosition + new Vector3(circle.x, 0f, circle.y);
            float   searchR   = patrolRadius * i * 0.5f;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchR, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 2f)
                {
                    patrolTarget = hit.position;
                    return;
                }
            }
        }
        patrolTarget = transform.position + transform.forward * patrolRadius * 0.5f;
    }

    [Header("Arena")]
    public bool requireArenaActivation = true;
    private bool _arenaActive = false;

    public void ActivateArena()
    {
        _arenaActive = true;
    }

    void DetectPlayer()
    {
        if (requireArenaActivation && !_arenaActive) return;
        if (player != null) return;
        var cols = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (var col in cols)
        {
            if (col.CompareTag("Player"))
            {
                player = col.transform;
                if (!_combatMusicStarted) { _combatMusicStarted = true; GameMusicManager.Instance?.EnterBoss(); }
                if (agent.isOnNavMesh) agent.isStopped = false;
                if (!bossHudShown)
                {
                    bossHudShown = true;
                    EnsureBossHud();
                    BossHUDController.Instance?.Show();
                    BossHUDController.Instance?.Refresh(currentHealth, maxHealth);
                }
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
        BossHUDController.Instance?.Refresh(currentHealth, maxHealth);
        if (currentHealth <= 0f)
            Die();
        else
            anim.SetTrigger(HashTakeDamage);
    }

    protected virtual void Die()
    {
        isDead = true;
        if (_combatMusicStarted) { _combatMusicStarted = false; GameMusicManager.Instance?.ExitBoss(); }
        BossHUDController.Instance?.Hide();
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger(HashDie);
        StartCoroutine(DeathRoutine());
    }

    private static void EnsureBossHud()
    {
        if (BossHUDController.Instance != null)
        {
            return;
        }

        var hudObject = new GameObject("BossHUD");
        hudObject.AddComponent<BossHUDController>();
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDuration);

        PlayerInventory.Instance?.AddCoins(coinReward);

        if (memoryPrefab != null)
        {
            var spawnPos = transform.position + Vector3.up * 1f;
            var mem = Instantiate(memoryPrefab, spawnPos, Quaternion.identity);
            var pickup = mem.GetComponent<MemoryPickup>();
            if (pickup != null)
            {
                pickup.videoIndex = videoIndex;
                pickup.completedLevelSceneName = SceneManager.GetActiveScene().name;
                
                // Define automaticamente o vídeo baseado na quantidade de níveis já concluídos
                string sceneName = SceneManager.GetActiveScene().name;
                
                int completedOtherLevels = 0;
                if (sceneName != "level_1" && LevelCompletionState.IsCompleted("level_1")) completedOtherLevels++;
                if (sceneName != "level_2" && LevelCompletionState.IsCompleted("level_2")) completedOtherLevels++;
                if (sceneName != "level_3" && LevelCompletionState.IsCompleted("level_3")) completedOtherLevels++;

                int memoryNumber = completedOtherLevels + 1;
                if (memoryNumber > 3) memoryNumber = 3;

                pickup.videoFileName = $"{memoryNumber}_memoria_v2";
                pickup.targetSceneName = "masmorra_spawn";
            }
        }

        Destroy(gameObject);
    }
}
