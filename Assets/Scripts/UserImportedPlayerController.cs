using System;
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
    private const string RpgUnarmedFolder = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed";

    [SerializeField] private float walkSpeed = 2.25f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float jumpForce = 5.5f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float rollDuration = 0.8f;
    [SerializeField] private float rollSpeed = 7f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float cameraPitchMin = -30f;
    [SerializeField] private float cameraPitchMax = 70f;
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, 1.8f, -3.6f);

    private Transform _visualRoot;
    private CharacterController _controller;
    private Camera _mainCamera;
    private Transform _cameraPivot;
    private Animator _animator;
    private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
    private float _verticalVelocity;
    private AnimationClip _currentClip;
    private float _clipTime;
    private float _yaw;
    private float _pitch = 12f;
    private PlayableGraph _playableGraph;
    private AnimationClipPlayable _clipPlayable;
    private AnimationPlayableOutput _animationOutput;
    private bool _graphCreated;

    public void AssignVisualRoot(Transform visualRoot)
    {
        _visualRoot = visualRoot;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        LoadClips();
        SetupCameraRig();
        EnsureAnimationGraph();
        PlayClip("idle");
    }

    private void Update()
    {
        if (_controller == null || _visualRoot == null)
        {
            return;
        }

        bool grounded = _controller.isGrounded;
        if (grounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -1f;
        }

        HandleCameraLook();

        float inputX = GetAxisHorizontal();
        float inputZ = GetAxisVertical();
        Vector3 input = new Vector3(inputX, 0f, inputZ).normalized;

        bool run = GetRunHeld();
        float speed = run ? runSpeed : walkSpeed;
        Vector3 movementDirection = GetMovementDirection(input);
        Vector3 worldMove = movementDirection * speed;

        if (grounded && GetJumpPressed())
        {
            _verticalVelocity = jumpForce;
        }

        _verticalVelocity += gravity * Time.deltaTime;
        worldMove.y = _verticalVelocity;
        _controller.Move(worldMove * Time.deltaTime);

        // Rotation is controlled exclusively by mouse (_yaw in HandleCameraLook).
        // Do not Slerp toward movement direction — the camera is a child of the player,
        // so rotating the player changes camForward, which changes movementDirection,
        // which changes the Slerp target — an infinite feedback loop causing spinning.

        if (input.sqrMagnitude < 0.01f)
        {
            PlayClip("idle");
        }
        else
        {
            PlayClip(run ? "run" : "walk");
        }

        SampleCurrentClip();
    }

    private void LoadClips()
    {
        _clips.Clear();

#if UNITY_EDITOR
        LoadEditorClipsFromFolder(RpgUnarmedFolder);
        ForceEditorClipOverride("idle", "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Idle.FBX");
        ForceEditorClipOverride("walk", "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Strafe-Forward.FBX");
        ForceEditorClipOverride("run", "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Run-Forward.FBX");
#endif

        if (_clips.Count == 0)
        {
            LoadRuntimeClipsFromResources("UserImportedCharacterPro");
        }

        if (!_clips.ContainsKey("walk") && _clips.TryGetValue("run", out AnimationClip runClip))
        {
            _clips["walk"] = runClip;
        }
        if (!_clips.ContainsKey("idle") && _clips.TryGetValue("walk", out AnimationClip walkClip))
        {
            _clips["idle"] = walkClip;
        }
        if (!_clips.ContainsKey("run") && _clips.TryGetValue("walk", out AnimationClip walkLoop))
        {
            _clips["run"] = walkLoop;
        }

        KeepOnlyLocomotionClips();
    }

#if UNITY_EDITOR
    private void LoadEditorClipsFromFolder(string folderPath)
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
        foreach (string guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is not AnimationClip clip)
                {
                    continue;
                }
                if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string key = ResolveKeyFromName(fileName);
                if (ShouldOverrideClip(key, fileName) || !_clips.ContainsKey(key))
                {
                    _clips[key] = clip;
                }
            }
        }
    }
#endif

    private void LoadRuntimeClipsFromResources(string resourcesFolder)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcesFolder);
        foreach (AnimationClip clip in clips)
        {
            if (clip == null || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string key = ResolveKeyFromName(clip.name);
            if (!_clips.ContainsKey(key))
            {
                _clips.Add(key, clip);
            }
        }
    }

#if UNITY_EDITOR
    private void ForceEditorClipOverride(string key, string assetPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is not AnimationClip clip)
            {
                continue;
            }
            if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _clips[key] = clip;
            return;
        }
    }
#endif

    private bool ShouldOverrideClip(string key, string fileName)
    {
        if (!_clips.ContainsKey(key))
        {
            return true;
        }

        if (fileName.Contains("rpg-character@unarmed-idle") ||
            fileName.Contains("rpg-character@unarmed-strafe-forward") ||
            fileName.Contains("rpg-character@unarmed-run-forward"))
        {
            return true;
        }

        return false;
    }

    private void KeepOnlyLocomotionClips()
    {
        _clips.TryGetValue("idle", out AnimationClip idleClip);
        _clips.TryGetValue("walk", out AnimationClip walkClip);
        _clips.TryGetValue("run", out AnimationClip runClip);
        _clips.Clear();

        if (idleClip != null)
        {
            _clips["idle"] = idleClip;
        }
        if (walkClip != null)
        {
            _clips["walk"] = walkClip;
        }
        if (runClip != null)
        {
            _clips["run"] = runClip;
        }
        else if (walkClip != null)
        {
            _clips["run"] = walkClip;
        }
    }

    private static string ResolveKeyFromName(string clipName)
    {
        string lower = clipName.ToLowerInvariant();
        if (lower.Contains("roll"))
        {
            return "roll";
        }
        if (lower.Contains("crouch"))
        {
            return "crouch";
        }
        if (lower.Contains("heal"))
        {
            return "blockIdle";
        }
        if (lower.Contains("interact"))
        {
            return "block";
        }
        if (lower.Contains("attack"))
        {
            return "attack";
        }
        if (lower.Contains("block idle"))
        {
            return "blockIdle";
        }
        if (lower.Contains("block"))
        {
            return "block";
        }
        if (lower.Contains("jump"))
        {
            return "jump";
        }
        if (lower.Contains("idle"))
        {
            return "idle";
        }
        if (lower.Contains("run"))
        {
            return "run";
        }
        if (lower.Contains("strafe") || lower.Contains("walk"))
        {
            return "walk";
        }

        return "idle";
    }

    private void PlayClip(string key)
    {
        if (!_clips.TryGetValue(key, out AnimationClip clip))
        {
            return;
        }

        if (_currentClip == clip)
        {
            return;
        }

        _currentClip = clip;
        _clipTime = 0f;
        EnsureAnimationGraph();
        if (_graphCreated)
        {
            if (_clipPlayable.IsValid())
            {
                _clipPlayable.Destroy();
            }

            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _clipPlayable.SetApplyFootIK(false);
            _clipPlayable.SetApplyPlayableIK(false);
            _animationOutput.SetSourcePlayable(_clipPlayable);
            _playableGraph.Evaluate();
            return;
        }

        SampleClipAtTime(clip, 0f);
    }

    private void SampleCurrentClip()
    {
        if (_currentClip == null)
        {
            return;
        }

        if (_graphCreated && _clipPlayable.IsValid())
        {
            return;
        }

        _clipTime += Time.deltaTime;
        float length = Mathf.Max(_currentClip.length, 0.01f);
        float sampleTime = _clipTime % length;
        SampleClipAtTime(_currentClip, sampleTime);
    }

    private void SampleClipAtTime(AnimationClip clip, float sampleTime)
    {
        if (_visualRoot == null || clip == null)
        {
            return;
        }

        Vector3 localPos = _visualRoot.localPosition;
        Quaternion localRot = _visualRoot.localRotation;
        Vector3 localScale = _visualRoot.localScale;

        clip.SampleAnimation(_visualRoot.gameObject, sampleTime);

        // Ignore clip root translation/rotation so movement comes only from gameplay code.
        _visualRoot.localPosition = localPos;
        _visualRoot.localRotation = localRot;
        _visualRoot.localScale = localScale;
    }

    private void EnsureAnimationGraph()
    {
        if (_graphCreated || _visualRoot == null)
        {
            return;
        }

        _animator = _visualRoot.GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = _visualRoot.gameObject.AddComponent<Animator>();
        }

        if (_animator == null || _animator.avatar == null || !_animator.avatar.isValid || !_animator.avatar.isHuman)
        {
            return;
        }

        _animator.applyRootMotion = false;
        _playableGraph = PlayableGraph.Create("UserImportedPlayerGraph");
        _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _animationOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);
        _playableGraph.Play();
        _graphCreated = true;
    }

    private void OnDestroy()
    {
        if (_graphCreated && _playableGraph.IsValid())
        {
            _playableGraph.Destroy();
        }
    }

    private static float GetAxisHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return 0f;
        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        return x;
#else
        return Input.GetAxisRaw("Horizontal");
#endif
    }

    private static float GetAxisVertical()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return 0f;
        float z = 0f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) z -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) z += 1f;
        return z;
#else
        return Input.GetAxisRaw("Vertical");
#endif
    }

    private static bool GetRunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    private static bool GetJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    private static bool GetRollPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.cKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }

    private static bool GetAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool GetHealPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void SetupCameraRig()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
        if (_mainCamera == null)
        {
            return;
        }

        GameObject pivot = new GameObject("PlayerCameraPivot");
        _cameraPivot = pivot.transform;
        _cameraPivot.SetParent(transform, false);
        _cameraPivot.localPosition = new Vector3(0f, 1.2f, 0f);
        _cameraPivot.localRotation = Quaternion.identity;

        _mainCamera.transform.SetParent(_cameraPivot, false);
        _mainCamera.transform.localPosition = cameraLocalOffset;
        _mainCamera.transform.localRotation = Quaternion.identity;
    }

    private void HandleCameraLook()
    {
        if (_cameraPivot == null)
        {
            return;
        }

        Vector2 delta = GetMouseDelta();
        _yaw += delta.x * mouseSensitivity;
        _pitch -= delta.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, cameraPitchMin, cameraPitchMax);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        if (m == null) return Vector2.zero;
        return m.delta.ReadValue() * 0.02f;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private Vector3 GetMovementDirection(Vector3 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        // Use _yaw directly instead of camera's world transform.
        // The camera is a child of the player — if we use camera.forward, rotating
        // the player (via Slerp) changes camera.forward, which changes movementDirection,
        // which changes the Slerp target — a feedback loop that causes spinning.
        // _yaw is stable (only changes from mouse input), so this breaks the loop.
        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right   = yawRotation * Vector3.right;

        return (right * input.x + forward * input.z).normalized;
    }
}
