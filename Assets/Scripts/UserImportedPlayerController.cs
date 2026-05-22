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
    // ── Inspector ────────────────────────────────────────────────────────────────
    [SerializeField] private float walkSpeed          = 2.25f;
    [SerializeField] private float runSpeed           = 4.5f;
    [SerializeField] private float jumpForce          = 5.5f;
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
    [SerializeField] private float attackRadius      = 1.8f;
    [SerializeField] private float attackFwdOffset   = 1.0f;

    // ── State Machine ────────────────────────────────────────────────────────────
    private enum MoveState { Locomotion, Attacking, Rolling, HitReact, Blocking }
    private MoveState _moveState = MoveState.Locomotion;
    private float     _stateTimer;

    // ── Combat ───────────────────────────────────────────────────────────────────
    private bool  _inCombat;
    private float _combatTimer;
    private int   _comboStep;    // 0,1,2  →  attack1/2/3
    private bool  _comboQueued;
    private bool  _hitDealt;
    private bool  _isInvincible;
    private float _lmbHoldTimer;
    private bool  _heavyAttackTriggered;
    private bool  _isBlocking;
    private PlayerHealth _playerHealth;

    private const float CombatExitDelay = 3.5f;
    private const float AttackHitTime   = 0.28f;
    private const float RollDuration    = 0.75f;
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
    private Animator              _animator;
    private AnimationClip         _currentClip;
    private float                 _clipTime;
    private PlayableGraph         _playableGraph;
    private AnimationClipPlayable _clipPlayable;
    private AnimationPlayableOutput _animOutput;
    private bool                  _graphCreated;

    private const string AnimBase = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/";

    // ═══════════════════════════════════════════════════════════════════════════
    //  SETUP
    // ═══════════════════════════════════════════════════════════════════════════
    public void AssignVisualRoot(Transform vr) => _visualRoot = vr;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _mainCamera = Camera.main
            ?? FindObjectOfType<Camera>()
            ?? CreateCamera();
        _yaw = transform.eulerAngles.y;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        LoadClips();
        SetupCameraRig();
        EnsureAnimationGraph();
        PlayClip("idle");

        _playerHealth = GetComponent<PlayerHealth>();
        if (_playerHealth != null) _playerHealth.onDamaged.AddListener(TriggerHitReaction);
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
        if (Time.timeScale <= 0f) return;

        HandleCameraLook();
        _stateTimer += Time.deltaTime;

        bool grounded = _controller.isGrounded;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        switch (_moveState)
        {
            case MoveState.Locomotion: UpdateLocomotion(grounded); break;
            case MoveState.Attacking:  UpdateAttacking(grounded);  break;
            case MoveState.Rolling:    UpdateRolling(grounded);    break;
            case MoveState.HitReact:   UpdateHitReact(grounded);   break;
            case MoveState.Blocking:   UpdateBlocking(grounded);   break;
        }

        SampleCurrentClip();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LOCOMOTION
    // ═══════════════════════════════════════════════════════════════════════════
    void UpdateLocomotion(bool grounded)
    {
        float ix = GetAxis("H");
        float iz = GetAxis("V");
        Vector3 input = new Vector3(ix, 0f, iz).normalized;
        bool  run     = GetRunHeld();

        Vector3 targetVel = MoveDir(input) * (input.sqrMagnitude > 0.01f ? (run ? runSpeed : walkSpeed) : 0f);
        _currentMovementVelocity = Vector3.SmoothDamp(
            _currentMovementVelocity, targetVel, ref _movementVelocityRef, movementSmoothTime);

        if (grounded && GetJumpPressed()) _verticalVelocity = jumpForce;
        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = _currentMovementVelocity;
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        // Roll
        if (GetRollPressed() && grounded) { EnterRoll(); return; }

        // Block (hold RMB)
        if (GetBlockHeld() && grounded && !IsPointerOverUI()) { EnterBlocking(); return; }

        // Light / heavy attack — quick tap = light combo, hold 0.4s = heavy
        if (GetAttackDown() && !IsPointerOverUI())
        {
            _lmbHoldTimer         = 0f;
            _heavyAttackTriggered = false;
        }
        if (GetAttackHeld() && !IsPointerOverUI())
        {
            _lmbHoldTimer += Time.deltaTime;
            if (_lmbHoldTimer >= 0.4f && !_heavyAttackTriggered)
            {
                _heavyAttackTriggered = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                _comboStep = 3;
                EnterAttack();
                return;
            }
        }
        if (GetAttackReleased() && !_heavyAttackTriggered && !IsPointerOverUI())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _comboStep = 0;
            EnterAttack();
            return;
        }

        // Combat mode: active while enemies are nearby, lingers CombatExitDelay after they leave
        if (CheckEnemiesNearby())
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

        // Animation (input-based — no velocity lag on idle)
        if (input.sqrMagnitude < 0.01f)
            PlayClip("idle");
        else if (run)
            PlayClip("run");
        else
            PlayClip(_inCombat ? "walkCombat" : "walk");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ATTACK COMBO
    // ═══════════════════════════════════════════════════════════════════════════
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
        // Only gravity, no lateral movement
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);

        // Snap to face camera yaw
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.Euler(0f, _yaw, 0f), 20f * Time.deltaTime);

        // Deal damage once per swing
        if (!_hitDealt && _stateTimer >= AttackHitTime)
        {
            _hitDealt = true;
            DealAttackDamage();
        }

        // Buffer next combo input (only for light attack chain, not heavy)
        if (GetAttackDown() && _comboStep < 3 && !IsPointerOverUI()) _comboQueued = true;

        // Transition at 85% of clip
        float clipLen = _currentClip != null ? _currentClip.length : 0.7f;
        if (_stateTimer >= clipLen * 0.85f)
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

    void DealAttackDamage()
    {
        Vector3 origin = transform.position + Vector3.up + transform.forward * attackFwdOffset;
        var already    = new HashSet<GameObject>();
        foreach (var col in Physics.OverlapSphere(origin, attackRadius))
        {
            var root = col.transform.root.gameObject;
            if (root == gameObject || already.Contains(root)) continue;
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg is PlayerHealth) continue;
            dmg.TakeDamage(attackDamage);
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
        PlayClip("roll");
    }

    void UpdateRolling(bool grounded)
    {
        float t     = _stateTimer / RollDuration;
        float speed = RollSpeed * Mathf.Cos(t * Mathf.PI * 0.5f);
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

        _controller.Move((transform.forward * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

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
        if (_isInvincible || _isBlocking) return;
        _moveState   = MoveState.HitReact;
        _stateTimer  = 0f;
        PlayClip("getHit");
    }

    void UpdateHitReact(bool grounded)
    {
        _verticalVelocity += gravity * Time.deltaTime;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);

        float clipLen = _currentClip != null ? _currentClip.length : 0.5f;
        if (_stateTimer >= clipLen)
        {
            _moveState  = MoveState.Locomotion;
            _stateTimer = 0f;
            PlayClip("idle");
        }
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

        // Exploration (unarmed — no weapon visible)
        Override("idle",       ua + "Idle.FBX");
        Override("walk",       ua + "Strafe-Forward.FBX");
        Override("run",        ua + "Run-Forward.FBX");
        Override("roll",       ua + "DiveRoll-Forward1.FBX");

        // Combat locomotion (2Hand-Sword)
        Override("walkCombat", s2 + "Strafe-Forward.FBX");

        // Combat actions (2Hand-Sword)
        Override("attack1",    s2 + "Attack1.FBX");
        Override("attack2",    s2 + "Attack2.FBX");
        Override("attack3",    s2 + "Attack3.FBX");
        Override("attack4",    s2 + "Attack4.FBX");
        Override("block",      s2 + "Idle.FBX");
        Override("getHit",     s2 + "GetHit-F1.FBX");
#endif

        if (_clips.Count == 0)
            LoadFromResources("UserImportedCharacterPro");

        // Fallbacks
        Fallback("walk",       "run");
        Fallback("walkCombat", "walk");
        Fallback("idle",       "walk");
        Fallback("run",        "walk");
        Fallback("attack1",    "idle");
        Fallback("attack2",    "attack1");
        Fallback("attack3",    "attack2");
        Fallback("attack4",    "attack3");
        Fallback("block",      "idle");
        Fallback("getHit",     "idle");
        Fallback("roll",       "run");
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
        if (!_clips.TryGetValue(key, out var clip) || clip == _currentClip) return;
        _currentClip = clip;
        _clipTime    = 0f;

        EnsureAnimationGraph();

        if (!_graphCreated) { SampleClipAt(clip, 0f); return; }
        if (_clipPlayable.IsValid()) _clipPlayable.Destroy();
        _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
        _clipPlayable.SetApplyFootIK(false);
        _clipPlayable.SetApplyPlayableIK(false);
        _animOutput.SetSourcePlayable(_clipPlayable);
        _playableGraph.Evaluate();
    }

    void SampleCurrentClip()
    {
        if (_currentClip == null || (_graphCreated && _clipPlayable.IsValid())) return;
        _clipTime += Time.deltaTime;
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
        _animOutput = AnimationPlayableOutput.Create(_playableGraph, "Anim", _animator);
        _playableGraph.Play();
        _graphCreated = true;
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
        var d = GetMouseDelta();
        _yaw   += d.x * mouseSensitivity;
        _pitch -= d.y * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, cameraPitchMin, cameraPitchMax);
        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    void LateUpdate()
    {
        if (_cameraPivot == null || _mainCamera == null || Time.timeScale <= 0f) return;
        _cameraPivot.position = Vector3.Lerp(
            _cameraPivot.position, transform.position + Vector3.up * 1.2f, cameraFollowSpeed * Time.deltaTime);

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
        var kb = Keyboard.current;
        if (kb == null) return 0f;
        if (axis == "H")
        {
            float v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v += 1f;
            return v;
        }
        else
        {
            float v = 0f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
            return v;
        }
#else
        return axis == "H" ? Input.GetAxisRaw("Horizontal") : Input.GetAxisRaw("Vertical");
#endif
    }

    static bool GetRunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.leftShiftKey.isPressed ?? false;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    static bool GetJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    static bool GetAttackDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.leftButton.wasPressedThisFrame ?? false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool GetAttackHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.leftButton.isPressed ?? false;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static bool GetAttackReleased()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.leftButton.wasReleasedThisFrame ?? false;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    static bool GetBlockHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current?.rightButton.isPressed ?? false;
#else
        return Input.GetMouseButton(1);
#endif
    }

    static bool GetRollPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.cKey.wasPressedThisFrame ?? false;
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

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    bool CheckEnemiesNearby()
    {
        const float radius = 20f;
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
