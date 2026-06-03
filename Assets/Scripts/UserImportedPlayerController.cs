using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UserImportedPlayerController : MonoBehaviour
{
    private const string BecoSceneName = "beco";
    private const string BecoNoiteSceneName = "beco_noite";
    private const string BecoDiaSceneName = "beco_dia";
    private const string RuntimePaladinClipsResourceFolder = "RuntimePaladinClips";
    // ── Inspector ────────────────────────────────────────────────────────────────
    [SerializeField] private float walkSpeed          = 2.25f;
    [SerializeField] private float runSpeed           = 4.5f;
    [SerializeField] private float jumpForce          = 9f;
    [SerializeField] private float gravity            = -18f;
    [SerializeField] private float turnSpeed          = 6f;
    [SerializeField] private float mouseSensitivity   = 5.0f;
    [SerializeField] private float cameraPitchMin     = -30f;
    [SerializeField] private float cameraPitchMax     = 70f;
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, 0.4f, -3.2f);
    [SerializeField] private float cameraFollowSpeed  = 15f;
    [SerializeField] private float movementSmoothTime = 0.1f;

    [Header("Combat")]
    [SerializeField] private float attackDamage      = 15f;
    [SerializeField] private float heavyAttackDamage = 40f;
    [SerializeField] private float attackRadius      = 2.0f;
    [SerializeField] private float attackFwdOffset   = 1.0f;

    // ── State Machine ────────────────────────────────────────────────────────────
    private enum MoveState { Locomotion, Drinking, Attacking, Rolling, HitReact, Blocking, Dead }
    private MoveState _moveState = MoveState.Locomotion;
    private float     _stateTimer;

    // ── Combat ───────────────────────────────────────────────────────────────────
    private bool  _inCombat;
    private float _combatTimer;
    private float _fallTimer;
    private float _airTime;
    private const float FallDeathTime = 5f;
    private int   _comboStep;    // 0,1,2  →  attack1/2/3
    private bool  _comboQueued;
    private bool  _hitDealt;
    private bool  _isInvincible;
    private bool  _isBlocking;
    private PlayerHealth _playerHealth;

    private const float CombatExitDelay = 3.5f;
    private const float AttackHitTime   = 0.28f;
    private const float RollDuration    = 1.1f;
    private const float RollSpeed       = 7f;

    // ── Movement ─────────────────────────────────────────────────────────────────
    private Transform          _visualRoot;
    private CharacterController _controller;
    private Camera             _mainCamera;
    private Transform          _cameraPivot;
    private float              _verticalVelocity;
    private float              _yaw;
    private float              _pitch = 12f;
    private bool               _isTargetingMode;
    private Vector3            _currentMovementVelocity;
    private Vector3            _movementVelocityRef;

    // ── Animation ────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
    private Animator _animator;
    private PlayableGraph _playableGraph;
    private AnimationPlayableOutput _animOutput;
    private AnimationLayerMixerPlayable _layerMixerPlayable;
    private AnimationMixerPlayable _mixerPlayable;
    private AnimationClipPlayable _clipPlayable;
    private AnimationClipPlayable _oldClipPlayable;
    private AnimationClipPlayable _drinkClipPlayable;
    private AnimationClip _currentClip;
    private bool _graphCreated = false;
    private float _clipTime = 0f;
    private float _transitionTime = 0f;
    private float _transitionDuration = 0.25f;
    private bool _isTransitioning = false;
    private bool _isScriptedSequencePlaying;
    private Coroutine             _activeScriptedSequence;
    private bool _externalControlLocked;
    private AnimationClip _potionDrinkIntroClip;
    private AnimationClip _potionDrinkLoopClip;
    private float _currentClipSpeed = 1f;
    private bool _drinkOverlayActive;
    private bool _isBecoScavengerMode;
    private Transform _cameraFollowOverride;
    private bool _cameraFollowOverrideAlign;
    private float _cameraFollowOverridePitch = 14f;
    private const float PotionDrinkStartTime = 1.97f;
    private const float PotionDrinkPlaybackSpeed = 2f;

    public bool IsScriptedSequencePlaying => _isScriptedSequencePlaying;
    public bool IsExternalControlLocked => _externalControlLocked;
    public Transform VisualRoot => _visualRoot;

    private const string AnimBase = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/";

    // ═══════════════════════════════════════════════════════════════════════════
    //  SETUP
    // ═══════════════════════════════════════════════════════════════════════════
    public void AssignVisualRoot(Transform vr) => _visualRoot = vr;

    public void SetExternalControlLocked(bool locked)
    {
        _externalControlLocked = locked;
        if (!locked)
        {
            return;
        }

        _currentMovementVelocity = Vector3.zero;
        _movementVelocityRef = Vector3.zero;
    }

    public void MoveExternally(Vector3 worldVelocity)
    {
        if (_controller == null)
        {
            return;
        }

        bool grounded = _controller.isGrounded;
        if (grounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -1f;
        }

        _verticalVelocity += gravity * Time.deltaTime;
        Vector3 move = worldVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);
    }

    public void FaceDirection(Vector3 worldDirection)
    {
        Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        transform.rotation = rotation;
        _yaw = rotation.eulerAngles.y;
    }

    public void PlayExternalAnimation(string key)
    {
        PlayClip(key);
    }

    public void SetVisualVisible(bool visible)
    {
        if (_visualRoot == null)
        {
            return;
        }

        foreach (Renderer renderer in _visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }

    public void SetCameraFollowOverride(Transform target, bool alignToTargetForward = true, float pitch = 14f, bool snapImmediately = false)
    {
        _cameraFollowOverride = target;
        _cameraFollowOverrideAlign = alignToTargetForward;
        _cameraFollowOverridePitch = pitch;

        if (!snapImmediately || _cameraPivot == null || target == null)
        {
            return;
        }

        _pitch = pitch;
        if (alignToTargetForward)
        {
            _yaw = target.eulerAngles.y;
        }

        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        _cameraPivot.position = target.position + Vector3.up * 1.2f;
        _mainCamera.transform.localPosition = cameraLocalOffset;
        _mainCamera.transform.localRotation = Quaternion.identity;
    }

    public void ClearCameraFollowOverride()
    {
        _cameraFollowOverride = null;
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _mainCamera = Camera.main
            ?? FindObjectOfType<Camera>()
            ?? CreateCamera();
        _yaw = transform.eulerAngles.y;
        _isBecoScavengerMode = IsBecoScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        LoadClips();
        EnsurePotionDrinkClip();
        SetupCameraRig();
        EnsureAnimationGraph();
        PlayClip("idle");

        _playerHealth = GetComponent<PlayerHealth>();
        if (_playerHealth != null)
        {
            _playerHealth.onDamaged.AddListener(TriggerHitReaction);
            _playerHealth.onDeath.AddListener(EnterDeath);
        }
    }

    void OnDestroy()
    {
        if (_graphCreated && _playableGraph.IsValid()) _playableGraph.Destroy();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (_controller == null || _visualRoot == null) return;
        if (_isScriptedSequencePlaying)
        {
            UpdateAnimationTransition();
            SampleCurrentClip();
            return;
        }

        if (_externalControlLocked)
        {
            bool externalGrounded = _controller.isGrounded;
            if (externalGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
            UpdateAnimationTransition();
            SampleCurrentClip();
            return;
        }

        if (Time.timeScale <= 0f) return;

        HandleCameraLook();
        _stateTimer += Time.deltaTime;

        bool grounded = _controller.isGrounded;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        if (!grounded) _airTime += Time.deltaTime;
        else _airTime = 0f;

        // Detetar queda prolongada do mapa — ragdoll + morte ao fim de 5s
        if (!grounded && _verticalVelocity < -3f && _moveState != MoveState.Dead)
        {
            _fallTimer += Time.deltaTime;
            if (_fallTimer >= FallDeathTime)
            {
                _fallTimer = 0f;
                PlayClip("knockdown");
                if (_playerHealth != null && !_playerHealth.IsDead)
                    _playerHealth.TakeDamage(_playerHealth.maxHealth);
            }
        }
        else
        {
            _fallTimer = 0f;
        }

        switch (_moveState)
        {
            case MoveState.Locomotion: UpdateLocomotion(grounded); break;
            case MoveState.Drinking:   UpdateDrinking(grounded);   break;
            case MoveState.Attacking:  UpdateAttacking(grounded);  break;
            case MoveState.Rolling:    UpdateRolling(grounded);    break;
            case MoveState.HitReact:   UpdateHitReact(grounded);   break;
            case MoveState.Blocking:   UpdateBlocking(grounded);   break;
            case MoveState.Dead:       UpdateDead(grounded);       break;
        }

        UpdateAnimationTransition();
        SampleCurrentClip();
    }

    public void PlayBottleDrinkSequence(AnimationClip drinkIntroClip, AnimationClip drinkLoopClip, AnimationClip collapseClip, Transform bottleTransform, Action onComplete)
    {
        if (_activeScriptedSequence != null)
        {
            StopCoroutine(_activeScriptedSequence);
        }

        _activeScriptedSequence = StartCoroutine(PlayBottleDrinkSequenceRoutine(drinkIntroClip, drinkLoopClip, collapseClip, bottleTransform, onComplete));
    }

    public void PlayConsumableDrinkSequence(Action onDrinkMoment)
    {
        if (_activeScriptedSequence != null)
        {
            StopCoroutine(_activeScriptedSequence);
        }

        _activeScriptedSequence = StartCoroutine(PlayConsumableDrinkSequenceRoutine(onDrinkMoment));
    }

    private IEnumerator PlayBottleDrinkSequenceRoutine(AnimationClip drinkIntroClip, AnimationClip drinkLoopClip, AnimationClip collapseClip, Transform bottleTransform, Action onComplete)
    {
        _isScriptedSequencePlaying = true;
        _currentMovementVelocity = Vector3.zero;
        _movementVelocityRef = Vector3.zero;
        _verticalVelocity = -1f;

        AttachBottleToRightHand(bottleTransform);
        yield return WaitForSecondsRoutine(0.1f);

        if (drinkIntroClip != null)
        {
            PlayDirectClipImmediate(drinkIntroClip, 0f, 1f);
            yield return WaitForClipSeconds(drinkIntroClip, 1f, 5f);
        }

        if (drinkLoopClip != null)
        {
            PlayDirectClipImmediate(drinkLoopClip, 0f, 1f);
            yield return WaitForClipSeconds(drinkLoopClip, 1f, 4f);
        }

        if (bottleTransform != null)
        {
            bottleTransform.gameObject.SetActive(false);
        }

        yield return WaitForSecondsRoutine(0.1f);

        if (collapseClip != null)
        {
            PlayDirectClipImmediate(collapseClip, 0f, 1f);
            yield return WaitForClipSeconds(collapseClip, 1f, 8f);
            // Congelar no último frame (evita loop da animação)
            float lastFrame = Mathf.Max(0f, collapseClip.length - 0.02f);
            _clipTime = lastFrame;
            _currentClipSpeed = 0f;
            if (_graphCreated && _clipPlayable.IsValid())
            {
                _clipPlayable.SetTime(lastFrame);
                _clipPlayable.SetSpeed(0);
                _playableGraph.Evaluate();
            }
        }

        // Invocar transição sem desativar _isScriptedSequencePlaying
        // O player fica deitado enquanto o ecrã faz fade to black
        _activeScriptedSequence = null;
        onComplete?.Invoke();

        // Segurança: caso a transição de cena falhe, desbloquear ao fim de 5s
        yield return WaitForSecondsRoutine(5f);
        _isScriptedSequencePlaying = false;
    }

    private IEnumerator PlayConsumableDrinkSequenceRoutine(Action onDrinkMoment)
    {
        _moveState = MoveState.Drinking;
        _stateTimer = 0f;
        _currentMovementVelocity = Vector3.zero;
        _movementVelocityRef = Vector3.zero;
        _verticalVelocity = -1f;

        if (_potionDrinkLoopClip != null)
        {
            PlayDrinkOverlayClip(_potionDrinkLoopClip, PotionDrinkStartTime, PotionDrinkPlaybackSpeed);
            yield return WaitForSecondsRoutine(3f);
        }
        else
        {
            yield return WaitForSecondsRoutine(3f);
        }

        onDrinkMoment?.Invoke();
        yield return WaitForSecondsRoutine(0.15f);

        if (_moveState == MoveState.Drinking)
        {
            _moveState = MoveState.Locomotion;
            StopDrinkOverlay();
            PlayClip("idle");
        }
        _activeScriptedSequence = null;
    }

    private void AttachBottleToRightHand(Transform bottleTransform)
    {
        if (bottleTransform == null || _visualRoot == null)
        {
            return;
        }

        Transform rightHand = FindHandBone(_visualRoot, "righthand")
            ?? FindHandBone(_visualRoot, "hand.r")
            ?? FindHandBone(_visualRoot, "rhand");

        if (rightHand == null)
        {
            return;
        }

        bottleTransform.SetParent(rightHand, false);
        bottleTransform.localPosition = new Vector3(0.035f, 0.035f, 0.02f);
        bottleTransform.localRotation = Quaternion.Euler(72f, -18f, 94f);
        bottleTransform.localScale = Vector3.one * 0.35f;
    }

    private static Transform FindHandBone(Transform root, string nameFragment)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string normalized = child.name.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
            if (normalized.Contains(nameFragment))
            {
                return child;
            }
        }

        return null;
    }

    private IEnumerator WaitForClipSeconds(AnimationClip clip, float normalizedDuration = 1f, float maxDuration = 10f)
    {
        float duration = clip == null ? 0f : Mathf.Max(0.05f, Mathf.Min(clip.length * normalizedDuration, maxDuration));
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForSecondsRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LOCOMOTION
    // ═══════════════════════════════════════════════════════════════════════════
    void UpdateLocomotion(bool grounded)
    {
        if (GetRedPotionPressed())
        {
            TryUseHealingPotion(true);
            return;
        }

        if (GetBluePotionPressed())
        {
            TryUseHealingPotion(false);
            return;
        }

        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        bool  run     = !_isBecoScavengerMode && GetRunHeld();

        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? (run ? runSpeed : walkSpeed) : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        if (!_isBecoScavengerMode && grounded && GetJumpPressed()) _verticalVelocity = jumpForce;
        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        // Roll
        if (!_isBecoScavengerMode && GetRollPressed() && grounded) { EnterRoll(); return; }

        // Block
        if (!_isBecoScavengerMode && GetBlockHeld() && grounded && !IsPointerOverUI()) { EnterBlocking(); return; }

        if (!_isBecoScavengerMode && GetHeavyAttackPressed() && !IsPointerOverUI())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _comboStep = 3;
            EnterAttack();
            return;
        }

        if (!_isBecoScavengerMode && GetAttackPressed() && !IsPointerOverUI())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _comboStep = 0;
            EnterAttack();
            return;
        }
        // Combat mode: ativo quando inimigos avistam o player (via GameMusicManager)
        bool enemiesEngaged = GameMusicManager.Instance != null && GameMusicManager.Instance.IsInCombat;
        if (enemiesEngaged)
        {
            _inCombat    = true;
            _combatTimer = CombatExitDelay;
        }
        else if (_inCombat)
        {
            _combatTimer -= Time.deltaTime;
            if (_combatTimer <= 0f) _inCombat = false;
        }

        if (GetCtrlPressed()) _isTargetingMode = !_isTargetingMode;

        // Rotation
        Vector3 hVel = new Vector3(_currentMovementVelocity.x, 0f, _currentMovementVelocity.z);
        if (_isTargetingMode)
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        else if (hVel.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(hVel.normalized), turnSpeed * Time.deltaTime);

        // Animation
        if (!grounded && _verticalVelocity > 0.5f)
            PlayClip("jump");
        else if (!grounded && _verticalVelocity < -1f && _airTime > 0.3f)
            PlayClip("fall");
        else if (input.sqrMagnitude < 0.01f)
            PlayClip(_inCombat ? "idleCombat" : "idle");
        else if (run)
            PlayClip("run");
        else
            PlayClip(_inCombat ? "walkCombat" : "walk");
    }

    private void TryUseHealingPotion(bool useRedPotion)
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null || _playerHealth == null || _playerHealth.IsDead)
        {
            return;
        }

        if (_playerHealth.CurrentHealth >= _playerHealth.maxHealth)
        {
            PlayerHUDController.Instance?.ShowTemporaryMessage("A vida esta cheia.");
            return;
        }

        bool hasPotion = useRedPotion ? inventory.RedPotions > 0 : inventory.BluePotions > 0;
        if (!hasPotion)
        {
            return;
        }

        PlayConsumableDrinkSequence(() =>
        {
            if (useRedPotion)
            {
                inventory.UseRedPotion();
            }
            else
            {
                inventory.UseBluePotion();
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ATTACK COMBO
    // ═══════════════════════════════════════════════════════════════════════════
    void UpdateDrinking(bool grounded)
    {
        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        bool run = !_isBecoScavengerMode && GetRunHeld();

        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? (run ? runSpeed : walkSpeed) : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        if (!_isBecoScavengerMode && grounded && GetJumpPressed()) _verticalVelocity = jumpForce;
        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        Vector3 hVel = new Vector3(_currentMovementVelocity.x, 0f, _currentMovementVelocity.z);
        if (_isTargetingMode)
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        else if (hVel.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(hVel.normalized), turnSpeed * Time.deltaTime);

        if (input.sqrMagnitude < 0.01f)
            PlayClip("idle");
        else if (run)
            PlayClip("run");
        else
            PlayClip(_inCombat ? "walkCombat" : "walk");
    }

    void EnterAttack()
    {
        _moveState   = MoveState.Attacking;
        _stateTimer  = 0f;
        _hitDealt    = false;
        _comboQueued = false;

        string key = _comboStep == 0 ? "attack1" : _comboStep == 1 ? "attack2" : _comboStep == 2 ? "attack3" : "attack4";
        PlayClip(key);
    }

    void UpdateAttacking(bool grounded)
    {
        // Allow slow movement while attacking
        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? walkSpeed * 0.4f : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        // Snap to face camera yaw
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.Euler(0f, _yaw, 0f), 20f * Time.deltaTime);

        // Deal damage once per swing
        if (!_hitDealt && _stateTimer >= AttackHitTime)
        {
            _hitDealt = true;
            float dmg = _comboStep == 3 ? heavyAttackDamage : attackDamage;
            DealAttackDamage(dmg);
        }

        // Buffer next combo input (only for light attack chain, not heavy)
        if (GetAttackPressed() && _comboStep < 3 && !IsPointerOverUI()) _comboQueued = true;

        // Transition at 85% of clip, or after safety timeout
        float clipLen = _currentClip != null ? _currentClip.length : 0.7f;
        if (_stateTimer >= clipLen * 0.85f || _stateTimer >= 2.5f)
        {
            if (_comboQueued && _comboStep < 2)
            {
                _comboStep++;
                EnterAttack();
            }
            else
            {
                _comboStep  = 0;
                _moveState  = MoveState.Locomotion;
                _stateTimer = 0f;
                PlayClip("idle");
            }
        }
    }

    void DealAttackDamage(float damage)
    {
        Vector3 origin = transform.position + Vector3.up * 0.9f + transform.forward * attackFwdOffset;

        var already = new HashSet<GameObject>();
        foreach (var col in Physics.OverlapSphere(origin, attackRadius))
        {
            if (col.isTrigger) continue;
            var root = col.transform.root.gameObject;
            if (root == gameObject || already.Contains(root)) continue;
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg is PlayerHealth) continue;
            dmg.TakeDamage(damage);
            already.Add(root);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ROLL / DODGE
    // ═══════════════════════════════════════════════════════════════════════════
    void EnterRoll()
    {
        _moveState    = MoveState.Rolling;
        _stateTimer   = 0f;
        _isInvincible = true;

        // Limpar playables para transição INSTANTÂNEA — sem blend de walk/run → roll
        if (_graphCreated && _playableGraph.IsValid())
        {
            if (_oldClipPlayable.IsValid())  { _playableGraph.Disconnect(_mixerPlayable, 1); _oldClipPlayable.Destroy(); }
            if (_clipPlayable.IsValid())     { _playableGraph.Disconnect(_mixerPlayable, 0); _clipPlayable.Destroy(); }
            _isTransitioning = false;
            _mixerPlayable.SetInputWeight(0, 1f);
            _mixerPlayable.SetInputWeight(1, 0f);
        }
        _currentClip = null;
        PlayClip("roll");
    }

    void UpdateRolling(bool grounded)
    {
        // Movimento usa RollDuration para a curva de velocidade
        float t     = _stateTimer / RollDuration;
        float speed = RollSpeed * Mathf.Cos(Mathf.Min(t, 1f) * Mathf.PI * 0.5f);
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        _controller.Move((transform.forward * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

        // Sair ao fim de RollDuration — devolve controlo ao jogador imediatamente
        if (_stateTimer >= RollDuration)
        {
            _isInvincible = false;
            _moveState    = MoveState.Locomotion;
            _stateTimer   = 0f;
            PlayClip("idle");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HIT REACTION
    // ═══════════════════════════════════════════════════════════════════════════
    public void TriggerHitReaction()
    {
        if (_isInvincible || _isBlocking || _moveState == MoveState.Dead) return;
        _moveState   = MoveState.HitReact;
        _stateTimer  = 0f;
        _isInvincible = true;
        PlayClip("getHit");
    }

    void UpdateHitReact(bool grounded)
    {
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? walkSpeed * 0.5f : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        if (GetRollPressed() && grounded)
        {
            _isInvincible = false;
            EnterRoll();
            return;
        }

        float clipLen = _currentClip != null ? _currentClip.length : 0.5f;
        if (_stateTimer >= Mathf.Min(clipLen, 0.5f))
        {
            _isInvincible = false;
            _moveState    = MoveState.Locomotion;
            _stateTimer   = 0f;
            PlayClip("idle");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DEATH
    // ═══════════════════════════════════════════════════════════════════════════
    public void EnterDeath()
    {
        if (_moveState == MoveState.Dead) return;
        _moveState = MoveState.Dead;
        _stateTimer = 0f;
        _isInvincible = false;
        _isBlocking = false;
        if (_playerHealth != null) _playerHealth.DamageReduction = 0f;

        if (_activeScriptedSequence != null)
        {
            StopCoroutine(_activeScriptedSequence);
            _activeScriptedSequence = null;
        }
        _isScriptedSequencePlaying = false;

        PlayClip("knockdown");
    }

    void UpdateDead(bool grounded)
    {
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BLOCK / SHIELD
    // ═══════════════════════════════════════════════════════════════════════════
    void EnterBlocking()
    {
        _isBlocking = true;
        _moveState  = MoveState.Blocking;
        _stateTimer = 0f;
        if (_playerHealth != null) _playerHealth.DamageReduction = 0.75f;
        PlayClip("block");
    }

    void UpdateBlocking(bool grounded)
    {
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? walkSpeed * 0.4f : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.Euler(0f, _yaw, 0f), 8f * Time.deltaTime);

        if (!GetBlockHeld())
        {
            _isBlocking = false;
            _moveState  = MoveState.Locomotion;
            _stateTimer = 0f;
            if (_playerHealth != null) _playerHealth.DamageReduction = 0f;
            PlayClip("idle");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CLIP LOADING
    // ═══════════════════════════════════════════════════════════════════════════
    void LoadClips()
    {
        _clips.Clear();

#if UNITY_EDITOR
        string ua = AnimBase + "Unarmed/RPG-Character@Unarmed-";
        string s2 = AnimBase + "2Hand-Sword/RPG-Character@2Hand-Sword-";

        string ki = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";

        // Exploration (Kevin Iglesias — natural locomotion)
        Override("idle",       ki + "Idles/HumanM@Idle01.fbx");
        Override("idleStatic", ki + "Idles/HumanM@Idle01.fbx");
        Override("walk",       ki + "Movement/Walk/HumanM@Walk01_Forward.fbx");
        Override("run",        ki + "Movement/Run/HumanM@Run01_Forward.fbx");
        Override("roll",       ua + "DiveRoll-Forward1.FBX");
        Override("jump",       ki + "Movement/Jump/HumanM@Jump01.fbx");
        Override("fall",       ki + "Movement/Jump/HumanM@Fall01.fbx");

        // Combat locomotion e idle de combate (2Hand-Sword)
        Override("walkCombat", s2 + "Strafe-Forward.FBX");
        Override("idleCombat", s2 + "Idle.FBX");

        // Combat actions (2Hand-Sword)
        Override("attack1",    s2 + "Attack1.FBX");
        Override("attack2",    s2 + "Attack2.FBX");
        Override("attack3",    s2 + "Attack3.FBX");
        Override("attack4",    s2 + "Attack4.FBX");
        Override("block",      s2 + "Idle.FBX");
        Override("getHit",     s2 + "GetHit-F1.FBX");
        Override("knockdown",  s2 + "Knockdown1.FBX");
#endif

        if (_clips.Count == 0)
            LoadRuntimePaladinClipOverrides();

        if (_clips.Count == 0)
            LoadFromResources("UserImportedCharacterPro");

        // Fallbacks
        Fallback("walk",       "run");
        Fallback("walkCombat", "walk");
        Fallback("idleCombat", "idle");
        Fallback("idle",       "walk");
        Fallback("idleStatic", "idle");
        Fallback("run",        "walk");
        Fallback("jump",       "run");
        Fallback("fall",       "run");
        Fallback("attack1",    "idle");
        Fallback("attack2",    "attack1");
        Fallback("attack3",    "attack2");
        Fallback("attack4",    "attack3");
        Fallback("block",      "idle");
        Fallback("getHit",     "idle");
        Fallback("knockdown",  "getHit");
        Fallback("roll",       "run");
    }

    void EnsurePotionDrinkClip()
    {
        if (_potionDrinkIntroClip != null && _potionDrinkLoopClip != null)
        {
            return;
        }

        if (_potionDrinkIntroClip == null)
        {
            _potionDrinkIntroClip = FindRuntimeClipByName("MCU_am_Stand_Trans_Moonshine");
        }

        if (_potionDrinkLoopClip == null)
        {
            _potionDrinkLoopClip = FindRuntimeClipByName("Drinking");
        }

#if UNITY_EDITOR
        if (_potionDrinkLoopClip == null)
        {
            _potionDrinkLoopClip = FindClipAtPath(
                "Assets/ImportedAnimations/Paladin WProp J Nordstrom@Drinking.fbx",
                "Drinking");
        }

        if (_potionDrinkIntroClip == null)
        {
            _potionDrinkIntroClip = FindClipAtPath(
                "Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Stand_Trans_Moonshine.FBX",
                "MCU_am_Stand_Trans_Moonshine");
        }

        if (_potionDrinkLoopClip == null)
        {
            _potionDrinkLoopClip = FindClipAtPath(
                "Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Moonshine_01_Drink_Loop.FBX",
                "MCU_am_Moonshine_01_Drink_Loop");
        }
#endif
    }

    static AnimationClip FindRuntimeClipByName(string clipName)
    {
        AnimationClip[] clips = Resources.FindObjectsOfTypeAll<AnimationClip>();
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    void Override(string key, string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
            { _clips[key] = c; return; }
        }
    }

    static AnimationClip FindClipAtPath(string assetPath, string preferredClipName)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (obj is AnimationClip clip && clip.name == preferredClipName)
            {
                return clip;
            }
        }

        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }
#endif

    void LoadFromResources(string folder)
    {
        foreach (var c in Resources.LoadAll<AnimationClip>(folder))
        {
            if (c == null || c.name.StartsWith("__preview__")) continue;
            string k = ResolveKey(c.name);
            if (!_clips.ContainsKey(k)) _clips[k] = c;
        }
    }

    void LoadRuntimePaladinClipOverrides()
    {
        AnimationClip[] runtimeClips = Resources.LoadAll<AnimationClip>(RuntimePaladinClipsResourceFolder);
        if (runtimeClips == null || runtimeClips.Length == 0)
        {
            return;
        }

        OverrideRuntimeClip(runtimeClips, "idle", "Unarmed-Idle");
        OverrideRuntimeClip(runtimeClips, "walk", "Unarmed-Strafe-Forward");
        OverrideRuntimeClip(runtimeClips, "run", "Unarmed-Run-Forward");
        OverrideRuntimeClip(runtimeClips, "roll", "Unarmed-DiveRoll-Forward1");
        OverrideRuntimeClip(runtimeClips, "walkCombat", "2Hand-Sword-Strafe-Forward");
        OverrideRuntimeClip(runtimeClips, "attack1", "2Hand-Sword-Attack1");
        OverrideRuntimeClip(runtimeClips, "attack2", "2Hand-Sword-Attack2");
        OverrideRuntimeClip(runtimeClips, "attack3", "2Hand-Sword-Attack3");
        OverrideRuntimeClip(runtimeClips, "attack4", "2Hand-Sword-Attack4");
        OverrideRuntimeClip(runtimeClips, "block", "2Hand-Sword-Idle");
        OverrideRuntimeClip(runtimeClips, "getHit", "2Hand-Sword-GetHit-F1");
        OverrideRuntimeClip(runtimeClips, "knockdown", "2Hand-Sword-Knockdown1");
    }

    void OverrideRuntimeClip(AnimationClip[] clips, string key, string clipName)
    {
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || clip.name != clipName || clip.name.StartsWith("__preview__"))
            {
                continue;
            }

            _clips[key] = clip;
            return;
        }
    }

    void Fallback(string key, string from)
    {
        if (!_clips.ContainsKey(key) && _clips.TryGetValue(from, out var c)) _clips[key] = c;
    }

    static string ResolveKey(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("attack"))  return n.Contains("1") ? "attack1" : n.Contains("2") ? "attack2" : "attack3";
        if (n.Contains("gethit") || n.Contains("get-hit")) return "getHit";
        if (n.Contains("roll")   || n.Contains("dive"))    return "roll";
        if (n.Contains("strafe"))  return "walkCombat";
        if (n.Contains("idle"))    return "idle";
        if (n.Contains("run"))     return "run";
        if (n.Contains("walk"))    return "walk";
        return "idle";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PLAYABLE GRAPH
    // ═══════════════════════════════════════════════════════════════════════════
    void PlayClip(string key)
    {
        if (!_clips.TryGetValue(key, out var clip)) return;
        PlayDirectClip(clip);
    }

    private void PlayDirectClip(AnimationClip clip)
    {
        if (clip == null) return;
        if (_graphCreated && (!_playableGraph.IsValid() || !_animOutput.IsOutputValid()))
        {
            _graphCreated = false;
        }
        if (_currentClip == clip) return;
        _currentClip = clip;
        _currentClipSpeed = 1f;
        _clipTime    = 0f;

        EnsureAnimationGraph();

        if (!_graphCreated) { SampleClipAt(clip, 0f); return; }

        if (_oldClipPlayable.IsValid()) _oldClipPlayable.Destroy();

        if (_clipPlayable.IsValid())
        {
            _oldClipPlayable = _clipPlayable;
            _playableGraph.Disconnect(_mixerPlayable, 0);
            _playableGraph.Connect(_oldClipPlayable, 0, _mixerPlayable, 1);
            _mixerPlayable.SetInputWeight(1, 1f);
        }

        _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
        _clipPlayable.SetApplyFootIK(false);
        _clipPlayable.SetApplyPlayableIK(false);
        
        _playableGraph.Connect(_clipPlayable, 0, _mixerPlayable, 0);
        
        if (_oldClipPlayable.IsValid())
        {
            _mixerPlayable.SetInputWeight(0, 0f);
            _isTransitioning = true;
            _transitionTime = 0f;
        }
        else
        {
            _mixerPlayable.SetInputWeight(0, 1f);
            _isTransitioning = false;
        }

        _playableGraph.Evaluate();
    }

    private void SeekCurrentClip(float timeSeconds)
    {
        float clampedTime = Mathf.Max(0f, timeSeconds);
        _clipTime = clampedTime;

        if (_graphCreated && _clipPlayable.IsValid())
        {
            _clipPlayable.SetTime(clampedTime);
            _playableGraph.Evaluate();
            return;
        }

        if (_currentClip != null)
        {
            SampleClipAt(_currentClip, clampedTime);
        }
    }

    private void PlayDirectClipImmediate(AnimationClip clip, float startTimeSeconds, float playbackSpeed)
    {
        if (clip == null) return;

        PlayDirectClip(clip);

        _isTransitioning = false;
        _currentClip = clip;
        _currentClipSpeed = Mathf.Max(0.01f, playbackSpeed);
        _clipTime = Mathf.Clamp(startTimeSeconds, 0f, Mathf.Max(0.01f, clip.length - 0.01f));

        if (_graphCreated && _mixerPlayable.IsValid())
        {
            _mixerPlayable.SetInputWeight(0, 1f);
            _mixerPlayable.SetInputWeight(1, 0f);
        }

        if (_graphCreated && _clipPlayable.IsValid())
        {
            _clipPlayable.SetTime(_clipTime);
            _clipPlayable.SetSpeed(_currentClipSpeed);
            _playableGraph.Evaluate();
            return;
        }

        SampleClipAt(clip, _clipTime);
    }

    private void PlayDrinkOverlayClip(AnimationClip clip, float startTimeSeconds, float playbackSpeed)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAnimationGraph();
        if (!_graphCreated)
        {
            PlayDirectClipImmediate(clip, startTimeSeconds, playbackSpeed);
            return;
        }

        StopDrinkOverlay();

        if (!_layerMixerPlayable.IsValid())
        {
            _layerMixerPlayable = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
            _playableGraph.Connect(_mixerPlayable, 0, _layerMixerPlayable, 0);
            _layerMixerPlayable.SetInputWeight(0, 1f);
            _layerMixerPlayable.SetInputWeight(1, 0f);
            _layerMixerPlayable.SetLayerMaskFromAvatarMask(1, CreateUpperBodyMask());
        }

        _drinkClipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
        _drinkClipPlayable.SetApplyFootIK(false);
        _drinkClipPlayable.SetApplyPlayableIK(false);
        _drinkClipPlayable.SetTime(Mathf.Clamp(startTimeSeconds, 0f, Mathf.Max(0.01f, clip.length - 0.01f)));
        _drinkClipPlayable.SetSpeed(Mathf.Max(0.01f, playbackSpeed));

        _playableGraph.Connect(_drinkClipPlayable, 0, _layerMixerPlayable, 1);
        _layerMixerPlayable.SetInputWeight(1, 1f);
        _animOutput.SetSourcePlayable(_layerMixerPlayable);
        _playableGraph.Evaluate();
        _drinkOverlayActive = true;
    }

    private void StopDrinkOverlay()
    {
        if (!_graphCreated || !_layerMixerPlayable.IsValid())
        {
            return;
        }

        _layerMixerPlayable.SetInputWeight(1, 0f);
        if (_drinkClipPlayable.IsValid())
        {
            _playableGraph.Disconnect(_layerMixerPlayable, 1);
            _drinkClipPlayable.Destroy();
        }

        _animOutput.SetSourcePlayable(_mixerPlayable);
        _drinkOverlayActive = false;
    }

    private void SetCurrentClipPlaybackSpeed(float speed)
    {
        _currentClipSpeed = Mathf.Max(0.01f, speed);

        if (_graphCreated && _clipPlayable.IsValid())
        {
            _clipPlayable.SetSpeed(_currentClipSpeed);
        }
    }

    private void UpdateAnimationTransition()
    {
        if (!_isTransitioning || !_graphCreated || !_mixerPlayable.IsValid()) return;
        
        _transitionTime += Time.deltaTime;
        float t = Mathf.Clamp01(_transitionTime / _transitionDuration);
        
        _mixerPlayable.SetInputWeight(0, t);
        _mixerPlayable.SetInputWeight(1, 1f - t);
        
        if (t >= 1f)
        {
            _isTransitioning = false;
            if (_oldClipPlayable.IsValid())
            {
                _playableGraph.Disconnect(_mixerPlayable, 1);
                _oldClipPlayable.Destroy();
            }
        }
    }

    void SampleCurrentClip()
    {
        if (_currentClip == null || (_graphCreated && _clipPlayable.IsValid())) return;
        _clipTime += Time.deltaTime * _currentClipSpeed;
        SampleClipAt(_currentClip, _clipTime % Mathf.Max(_currentClip.length, 0.01f));
    }

    void SampleClipAt(AnimationClip clip, float t)
    {
        if (_visualRoot == null || clip == null) return;
        var lp = _visualRoot.localPosition;
        var lr = _visualRoot.localRotation;
        var ls = _visualRoot.localScale;
        clip.SampleAnimation(_visualRoot.gameObject, t);
        _visualRoot.localPosition = lp;
        _visualRoot.localRotation = lr;
        _visualRoot.localScale    = ls;
    }

    void EnsureAnimationGraph()
    {
        if (_graphCreated || _visualRoot == null) return;
        _animator = _visualRoot.GetComponent<Animator>()
                    ?? _visualRoot.gameObject.AddComponent<Animator>();
        if (_animator?.avatar == null || !_animator.avatar.isValid || !_animator.avatar.isHuman) return;
        _animator.applyRootMotion = false;
        _playableGraph = PlayableGraph.Create("PlayerGraph");
        _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        _mixerPlayable = AnimationMixerPlayable.Create(_playableGraph, 2);
        _animOutput = AnimationPlayableOutput.Create(_playableGraph, "Anim", _animator);
        _animOutput.SetSourcePlayable(_mixerPlayable);
        
        _playableGraph.Play();
        _graphCreated = true;
    }

    private static AvatarMask CreateUpperBodyMask()
    {
        AvatarMask mask = new AvatarMask();
        foreach (AvatarMaskBodyPart bodyPart in Enum.GetValues(typeof(AvatarMaskBodyPart)))
        {
            mask.SetHumanoidBodyPartActive(bodyPart, false);
        }

        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

        return mask;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════════════════════════════════════
    void SetupCameraRig()
    {
        if (_mainCamera == null) return;
        var pivot = new GameObject("PlayerCameraPivot");
        _cameraPivot = pivot.transform;
        _cameraPivot.position = transform.position + Vector3.up * 1.2f;
        _mainCamera.transform.SetParent(_cameraPivot, false);
        _mainCamera.transform.localPosition = cameraLocalOffset;
        _mainCamera.transform.localRotation = Quaternion.identity;
    }

    void HandleCameraLook()
    {
        if (_cameraPivot == null) return;
        if (_cameraFollowOverride != null) return;
        var d = GetMouseDelta();
        _yaw   += d.x * mouseSensitivity;
        _pitch -= d.y * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, cameraPitchMin, cameraPitchMax);
        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    void LateUpdate()
    {
        if (_cameraPivot == null || _mainCamera == null || Time.timeScale <= 0f) return;

        if (_cameraFollowOverride != null)
        {
            _pitch = Mathf.Lerp(_pitch, _cameraFollowOverridePitch, 6f * Time.deltaTime);
            if (_cameraFollowOverrideAlign)
            {
                _yaw = Mathf.LerpAngle(_yaw, _cameraFollowOverride.eulerAngles.y, 6f * Time.deltaTime);
            }

            _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _cameraPivot.position = Vector3.Lerp(
                _cameraPivot.position,
                _cameraFollowOverride.position + Vector3.up * 1.2f,
                cameraFollowSpeed * Time.deltaTime);

            _mainCamera.transform.localPosition = cameraLocalOffset;
            _mainCamera.transform.localRotation = Quaternion.identity;
            return;
        }
        else
        {
            _cameraPivot.position = Vector3.Lerp(
                _cameraPivot.position, transform.position + Vector3.up * 1.2f, cameraFollowSpeed * Time.deltaTime);
        }

        Vector3 dir  = cameraLocalOffset.normalized;
        float   dist = cameraLocalOffset.magnitude;
        int mask = ~0;
        int pl = LayerMask.NameToLayer("Player"), ig = LayerMask.NameToLayer("Ignore Raycast");
        if (pl >= 0) mask &= ~(1 << pl);
        if (ig >= 0) mask &= ~(1 << ig);

        if (Physics.SphereCast(_cameraPivot.position, 0.3f,
            _cameraPivot.TransformDirection(dir), out RaycastHit hit, dist, mask))
            _mainCamera.transform.localPosition = dir * hit.distance;
        else
            _mainCamera.transform.localPosition = Vector3.Lerp(
                _mainCamera.transform.localPosition, cameraLocalOffset, 10f * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INPUT
    // ═══════════════════════════════════════════════════════════════════════════
    float GetAxis(string axis)
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        if (axis == "H")
        {
            float v = 0f;
            if (MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Left) || Keyboard.current?.leftArrowKey.isPressed == true)  v -= 1f;
            if (MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Right) || Keyboard.current?.rightArrowKey.isPressed == true) v += 1f;
            return v;
        }
        else
        {
            float v = 0f;
            if (MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Backward) || Keyboard.current?.downArrowKey.isPressed == true)  v -= 1f;
            if (MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Forward) || Keyboard.current?.upArrowKey.isPressed == true)    v += 1f;
            return v;
        }
#else
        return axis == "H" ? Input.GetAxisRaw("Horizontal") : Input.GetAxisRaw("Vertical");
#endif
    }

    static bool GetRunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Run);
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    static bool GetJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.WasPressedThisFrame(MovementKeyBindings.GameAction.Jump);
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    static bool GetAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.leftButton.wasPressedThisFrame ?? false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool GetHeavyAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.rightButton.wasPressedThisFrame ?? false;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    static bool GetBlockHeld()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.IsPressed(MovementKeyBindings.GameAction.Defend);
#else
        return Input.GetKey(KeyCode.R);
#endif
    }

    static bool GetRollPressed()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.WasPressedThisFrame(MovementKeyBindings.GameAction.Roll);
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }

    static bool GetCtrlPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.leftCtrlKey.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(KeyCode.LeftControl);
#endif
    }

    static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return (Mouse.current?.delta.ReadValue() ?? Vector2.zero) * 0.02f;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    static bool IsPointerOverUI() =>
        UnityEngine.EventSystems.EventSystem.current != null
        && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

    static bool GetRedPotionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.qKey.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    static bool GetBluePotionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.eKey.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    private static bool IsBecoScene(string sceneName)
    {
        return sceneName == BecoSceneName
            || sceneName == BecoNoiteSceneName
            || sceneName == BecoDiaSceneName;
    }

    bool CheckEnemiesNearby()
    {
        const float radius = 6f; // raio reduzido para não entrar em combate ao spawn
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) return false;
        return Physics.CheckSphere(transform.position, radius, 1 << enemyLayer);
    }

    Vector3 MoveDir(Vector3 input)
    {
        if (input.sqrMagnitude <= 0.0001f) return Vector3.zero;
        var yaw = Quaternion.Euler(0f, _yaw, 0f);
        return (yaw * Vector3.right * input.x + yaw * Vector3.forward * input.z).normalized;
    }

    Camera CreateCamera()
    {
        var go = new GameObject("MainCamera");
        go.tag = "MainCamera";
        go.AddComponent<AudioListener>();
        return go.AddComponent<Camera>();
    }
}
