using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class PaladinFinalBossController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float maxArmor = 200f;
    [SerializeField] private float attackDamage = 18f;
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float attackCooldown = 1.9f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8.5f;
    [SerializeField] private float attackRange = 1.9f;
    [SerializeField] private float damageRadius = 1.45f;
    [SerializeField] private float attackForwardOffset = 0.9f;

    [Header("Arena")]
    [SerializeField] private string displayName = "Fantasma do Passado";
    [SerializeField] public bool requireArenaActivation = true;

    [Header("Animation")]
    [SerializeField] private string resourcesFolder = "UserImportedCharacter";
    [SerializeField] private float attackHitDelay = 0.34f;
    [SerializeField] private float attackAnimTime = 0.9f;
    [SerializeField] private float hitReactTime = 0.45f;
    [SerializeField] private float deathAnimTime = 1.5f;

    [Header("Memory Drop")]
    [SerializeField] private GameObject memoryPrefab;
    [SerializeField] private int memoryVideoIndex = 4;
    [SerializeField] private string memoryTargetScene = "beco_dia";

    private readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

    private Transform visualRoot;
    private NavMeshAgent agent;
    private CapsuleCollider bodyCollider;
    private Transform player;
    private float currentHealth;
    private float currentArmor;
    private float attackTimer;
    private bool isDead;
    private bool isAttacking;
    private bool isHitReacting;
    private bool _arenaActive;
    private bool _combatMusicStarted;
    private AudioClip _deathSound;

    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixerPlayable;
    private AnimationClipPlayable currentPlayable;
    private AnimationClipPlayable oldPlayable;
    private AnimationPlayableOutput animOutput;
    private AnimationClip currentClip;
    private string currentClipKey;
    private bool graphCreated;
    private bool isTransitioning;
    private float transitionTime;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private Vector3 visualBaseLocalScale;
    private const float TransitionDuration = 0.2f;
    private const string AnimBase = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/";
    private const string RuntimeBossClipsFolder = "RuntimePaladinClips";

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<CapsuleCollider>();
        visualRoot = transform.Find("Visual");
        if (visualRoot != null)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseLocalRotation = visualRoot.localRotation;
            visualBaseLocalScale = visualRoot.localScale;
        }
        currentHealth = maxHealth;
        currentArmor = maxArmor;

        agent.speed = moveSpeed;
        agent.angularSpeed = 420f;
        agent.acceleration = 30f;
        agent.stoppingDistance = attackRange * 0.82f;
        agent.radius = 0.35f;
        agent.height = 1.9f;
        agent.autoBraking = true;

        bodyCollider.radius = 0.35f;
        bodyCollider.height = 1.85f;
        bodyCollider.center = new Vector3(0f, 0.92f, 0f);

        SnapBossToGround();

        gameObject.layer = LayerMask.NameToLayer("Enemy") >= 0 ? LayerMask.NameToLayer("Enemy") : gameObject.layer;

        if (visualRoot != null && visualRoot.GetComponent<VisualAnimationEventReceiver>() == null)
        {
            visualRoot.gameObject.AddComponent<VisualAnimationEventReceiver>();
        }

        ApplyBossMaterials();
        EnsureBossHud();
        BossHUDController.Instance?.Hide();
        LoadClips();
        EnsureAnimationGraph();
        PlayClip("idle");
        _deathSound = Resources.Load<AudioClip>("death_fall");

#if UNITY_EDITOR
        if (memoryPrefab == null)
        {
            memoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/memories/Prefabs/MemoryPickup.prefab");
        }
#endif
    }

    private void SnapBossToGround()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 8f, Vector3.down, 30f);
        float bestDistance = float.MaxValue;
        Vector3 bestPoint = transform.position;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestPoint = hit.point;
                found = true;
            }
        }

        if (found)
        {
            transform.position = new Vector3(transform.position.x, bestPoint.y, transform.position.z);
            if (agent != null && agent.enabled)
            {
                agent.Warp(transform.position);
            }
        }
    }

    private void Update()
    {
        if (isDead)
        {
            UpdateTransition();
            return;
        }

        attackTimer -= Time.deltaTime;
        ResolvePlayer();

        if (player == null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            if (!isAttacking && !isHitReacting)
            {
                PlayClip("idle");
            }
            UpdateTransition();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange * 1.75f)
        {
            player = null;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            PlayClip("idle");
            UpdateTransition();
            return;
        }

        if (isHitReacting)
        {
            FaceTarget(player.position);
            UpdateTransition();
            return;
        }

        if (isAttacking)
        {
            FaceTarget(player.position);
            UpdateTransition();
            return;
        }

        if (distance <= attackRange)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            FaceTarget(player.position);
            PlayClip("idle");

            if (attackTimer <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            FaceTarget(player.position);
            PlayClip("run");
        }

        UpdateTransition();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
        {
            return;
        }

        float remainingDamage = amount;
        if (currentArmor > 0f)
        {
            float absorbed = Mathf.Min(currentArmor, remainingDamage);
            currentArmor -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0f)
        {
            currentHealth -= remainingDamage;
        }

        BossHUDController.Instance?.RefreshWithArmor(currentHealth, maxHealth, currentArmor, maxArmor);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            StartCoroutine(DeathRoutine());
            return;
        }

        if (!isHitReacting && !isAttacking)
        {
            StartCoroutine(HitReactRoutine());
        }
    }

    public void RevealEncounterUI()
    {
        if (isDead)
        {
            return;
        }

        ResolvePlayer();
        EnsureBossHud();
        BossHUDController.Instance?.Show();
        BossHUDController.Instance?.RefreshWithArmor(currentHealth, maxHealth, currentArmor, maxArmor);
    }

    private IEnumerator HitReactRoutine()
    {
        isHitReacting = true;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        PlayClip("getHit");
        yield return new WaitForSeconds(hitReactTime);
        isHitReacting = false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        PlayClip("attack1");
        yield return new WaitForSeconds(attackHitDelay);
        DealDamage();
        yield return new WaitForSeconds(Mathf.Max(0.05f, attackAnimTime - attackHitDelay));
        isAttacking = false;
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isAttacking = false;
        isHitReacting = false;
        if (_combatMusicStarted) { _combatMusicStarted = false; GameMusicManager.Instance?.ExitBoss(); }
        if (agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        PlayClip("death");
        StartCoroutine(PlayDeathSoundRoutine());
        yield return new WaitForSeconds(deathAnimTime);

        if (memoryPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1f;
            GameObject mem = Instantiate(memoryPrefab, spawnPos, Quaternion.identity);
            MemoryPickup pickup = mem.GetComponent<MemoryPickup>();
            if (pickup != null)
            {
                pickup.videoIndex = memoryVideoIndex;
                pickup.videoFileName = "4_memoria_completa_fim_v2";
                pickup.targetSceneName = memoryTargetScene;
                pickup.completedLevelSceneName = "boss_room";
                pickup.loadSceneOnInteract = true;
            }
        }

        BossHUDController.Instance?.Hide();
        Destroy(gameObject);
    }

    private IEnumerator PlayDeathSoundRoutine()
    {
        yield return new WaitForSeconds(0.4f);
        if (_deathSound != null)
        {
            AudioSource audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.PlayOneShot(_deathSound, 1.0f);
            audio.PlayOneShot(_deathSound, 1.0f);
            audio.PlayOneShot(_deathSound, 1.0f);
        }
    }

    private void DealDamage()
    {
        Vector3 origin = transform.position + transform.forward * attackForwardOffset + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(origin, damageRadius);
        foreach (Collider hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || ReferenceEquals(damageable, this))
            {
                continue;
            }

            damageable.TakeDamage(attackDamage);
        }
    }

    public void ActivateArena()
    {
        _arenaActive = true;
    }

    private void ResolvePlayer()
    {
        if (requireArenaActivation && !_arenaActive) return;
        if (player != null) return;

        foreach (Collider col in Physics.OverlapSphere(transform.position, detectionRange))
        {
            if (!col.CompareTag("Player")) continue;
            player = col.transform;
            EnsureBossHud();
            BossHUDController.Instance?.SetName(displayName);
            BossHUDController.Instance?.Show();
            BossHUDController.Instance?.RefreshWithArmor(currentHealth, maxHealth, currentArmor, maxArmor);
            if (!_combatMusicStarted)
            {
                _combatMusicStarted = true;
                GameMusicManager.Instance?.EnterBoss();
            }
            break;
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    private void LoadClips()
    {
        clips.Clear();

#if UNITY_EDITOR
        string ua = AnimBase + "Unarmed/RPG-Character@Unarmed-";
        string s2 = AnimBase + "2Hand-Sword/RPG-Character@2Hand-Sword-";

        Override("idle", ua + "Idle.FBX");
        Override("walk", ua + "Strafe-Forward.FBX");
        Override("run", ua + "Run-Forward.FBX");
        Override("attack1", s2 + "Attack1.FBX");
        Override("getHit", s2 + "GetHit-F1.FBX");

        Override("death", s2 + "Knockdown1.FBX");
#endif

        LoadRuntimeBossClip("idle", "2Hand-Sword-Idle");
        LoadRuntimeBossClip("walk", "2Hand-Sword-Strafe-Forward");
        LoadRuntimeBossClip("run", "2Hand-Sword-Strafe-Forward");
        LoadRuntimeBossClip("attack1", "RPG-Character@2Hand-Sword-Attack1");
        LoadRuntimeBossClip("getHit", "2Hand-Sword-GetHit-F1");
        LoadRuntimeBossClip("death", "2Hand-Sword-Knockdown1");

        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(resourcesFolder))
        {
            if (clip == null || clip.name.StartsWith("__preview__"))
            {
                continue;
            }

            string key = ResolveKey(clip.name);
            if (!clips.ContainsKey(key))
            {
                clips[key] = clip;
            }
        }

        Fallback("run", "walk");
        Fallback("walk", "idle");
        Fallback("attack1", "idle");
        Fallback("getHit", "idle");
        Fallback("death", "getHit");
    }

    private void LoadRuntimeBossClip(string key, string clipName)
    {
        if (clips.ContainsKey(key))
        {
            return;
        }

        AnimationClip[] loaded = Resources.LoadAll<AnimationClip>(RuntimeBossClipsFolder);
        for (int i = 0; i < loaded.Length; i++)
        {
            AnimationClip clip = loaded[i];
            if (clip == null || clip.name.StartsWith("__preview__"))
            {
                continue;
            }

            if (string.Equals(clip.name, clipName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals("RPG-Character@" + clip.name, clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                clips[key] = clip;
                return;
            }
        }
    }

#if UNITY_EDITOR
    private void Override(string key, string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                clips[key] = clip;
                return;
            }
        }
    }
#endif

    private void Fallback(string key, string from)
    {
        if (!clips.ContainsKey(key) && clips.TryGetValue(from, out AnimationClip clip))
        {
            clips[key] = clip;
        }
    }

    private string ResolveKey(string clipName)
    {
        string normalized = clipName.ToLowerInvariant();
        if (normalized.Contains("death")) return "death";
        if (normalized.Contains("impact") || normalized.Contains("gethit") || normalized.Contains("get-hit")) return "getHit";
        if (normalized.Contains("attack")) return "attack1";
        if (normalized.Contains("run")) return "run";
        if (normalized.Contains("walk")) return "walk";
        if (normalized.Contains("idle")) return "idle";
        return "idle";
    }

    private void EnsureAnimationGraph()
    {
        if (graphCreated || visualRoot == null)
        {
            return;
        }

        Animator animator = visualRoot.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return;
        }

        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;

        playableGraph = PlayableGraph.Create(name + "_BossGraph");
        animOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 2, true);
        animOutput.SetSourcePlayable(mixerPlayable);
        playableGraph.Play();
        graphCreated = true;
    }

    private void ApplyBossMaterials()
    {
        if (visualRoot == null)
        {
            return;
        }

#if UNITY_EDITOR
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bosses/Materials/PaladinBoss_Body.mat");
        Material metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bosses/Materials/PaladinBoss_Metal.mat");
        Material shield = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bosses/Materials/PaladinBoss_Shield.mat");
        Material weapon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bosses/Materials/PaladinBoss_Weapon.mat");
        if (body == null || metal == null || shield == null || weapon == null)
        {
            return;
        }

        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            Material chosen = body;
            string n = renderer.name.ToLowerInvariant();
            if (n.Contains("helmet"))
            {
                chosen = metal;
            }
            else if (n.Contains("shield"))
            {
                chosen = shield;
            }
            else if (n.Contains("sword"))
            {
                chosen = weapon;
            }

            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = chosen;
            }
            renderer.sharedMaterials = mats;
        }
#endif
    }

    private void PlayClip(string key)
    {
        if (!clips.TryGetValue(key, out AnimationClip clip) || clip == null)
        {
            return;
        }

        PlayDirectClip(key, clip);
    }

    private void PlayDirectClip(string key, AnimationClip clip)
    {
        if (!graphCreated)
        {
            currentClip = clip;
            currentClipKey = key;
            return;
        }

        if (currentClip == clip && currentClipKey == key)
        {
            return;
        }

        currentClip = clip;
        currentClipKey = key;

        if (oldPlayable.IsValid())
        {
            oldPlayable.Destroy();
        }

        if (currentPlayable.IsValid())
        {
            oldPlayable = currentPlayable;
            playableGraph.Disconnect(mixerPlayable, 0);
            playableGraph.Connect(oldPlayable, 0, mixerPlayable, 1);
            mixerPlayable.SetInputWeight(1, 1f);
        }

        currentPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        currentPlayable.SetApplyFootIK(false);
        currentPlayable.SetApplyPlayableIK(false);
        currentPlayable.SetTime(0);
        playableGraph.Connect(currentPlayable, 0, mixerPlayable, 0);

        if (oldPlayable.IsValid())
        {
            mixerPlayable.SetInputWeight(0, 0f);
            isTransitioning = true;
            transitionTime = 0f;
        }
        else
        {
            mixerPlayable.SetInputWeight(0, 1f);
            mixerPlayable.SetInputWeight(1, 0f);
            isTransitioning = false;
        }

        playableGraph.Evaluate();
        RestoreVisualRootTransform();
    }

    private void LateUpdate()
    {
        RestoreVisualRootTransform();

        if (!graphCreated || !currentPlayable.IsValid() || currentClip == null)
        {
            return;
        }

        if (!IsLoopingClip(currentClipKey))
        {
            return;
        }

        double clipLength = currentClip.length;
        if (clipLength <= 0.01d)
        {
            return;
        }

        double time = currentPlayable.GetTime();
        if (time >= clipLength)
        {
            currentPlayable.SetTime(time % clipLength);
        }
    }

    private void RestoreVisualRootTransform()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = visualBaseLocalPosition;
        visualRoot.localRotation = visualBaseLocalRotation;
        visualRoot.localScale = visualBaseLocalScale;
    }

    private static bool IsLoopingClip(string key)
    {
        return key == "idle" || key == "walk" || key == "run";
    }

    private void UpdateTransition()
    {
        if (!isTransitioning || !graphCreated)
        {
            return;
        }

        transitionTime += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTime / TransitionDuration);
        mixerPlayable.SetInputWeight(0, t);
        mixerPlayable.SetInputWeight(1, 1f - t);

        if (t >= 1f)
        {
            isTransitioning = false;
            if (oldPlayable.IsValid())
            {
                playableGraph.Disconnect(mixerPlayable, 1);
                oldPlayable.Destroy();
            }
        }
    }

    private void OnDestroy()
    {
        if (graphCreated && playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackForwardOffset + Vector3.up * 0.9f, damageRadius);
    }
}
