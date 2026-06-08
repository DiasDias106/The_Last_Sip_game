using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArrowTrapController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float firstShotDelay = 1f;
    [SerializeField] private float fireInterval = 3f;
    [SerializeField] private float fireDuration = 0.75f;
    [SerializeField] private float activeDamageDuration = 0.8f;
    [SerializeField] private float reloadDelay = 0.45f;
    [SerializeField] private float respawnDuration = 0.35f;

    [Header("Shot")]
    [SerializeField] private bool instantKill = true;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float travelDistance = 8f;
    [SerializeField] private float respawnOffset = 0.5f;
    [SerializeField] private Vector3 localShootDirection = Vector3.up;
    [SerializeField, Range(0.05f, 1f)] private float respawnStartScaleMultiplier = 0.2f;

    [Header("References")]
    [SerializeField] private Transform movingVisual;

    private readonly HashSet<PlayerHealth> damagedTargetsThisShot = new HashSet<PlayerHealth>();

    private BoxCollider movingVisualCollider;
    private Coroutine fireLoop;
    private Vector3 loadedLocalPosition;
    private Vector3 loadedLocalScale;
    private int currentShotId;
    private bool damageWindowActive;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureDamageCollider();
    }

    private void OnEnable()
    {
        CacheReferences();
        ConfigureDamageCollider();
        SetLoadedPoseImmediate();
        fireLoop = StartCoroutine(FireLoop());
    }

    private void OnDisable()
    {
        if (fireLoop != null)
        {
            StopCoroutine(fireLoop);
            fireLoop = null;
        }

        currentShotId++;
        damageWindowActive = false;
        damagedTargetsThisShot.Clear();

        if (movingVisualCollider != null)
        {
            movingVisualCollider.enabled = false;
        }

        SetLoadedPoseImmediate();
    }

    public void NotifyHit(PlayerHealth playerHealth)
    {
        if (!damageWindowActive || playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        if (!damagedTargetsThisShot.Add(playerHealth))
        {
            return;
        }

        float damageToApply = instantKill ? GetFatalDamage(playerHealth) : damage;
        playerHealth.TakeDamage(damageToApply);
    }

    private IEnumerator FireLoop()
    {
        if (firstShotDelay > 0f)
        {
            yield return new WaitForSeconds(firstShotDelay);
        }

        while (enabled && gameObject.activeInHierarchy)
        {
            float startTime = Time.time;
            yield return FireSequence();

            float remaining = fireInterval - (Time.time - startTime);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }
    }

    private IEnumerator FireSequence()
    {
        if (movingVisual == null)
        {
            yield break;
        }

        currentShotId++;
        damagedTargetsThisShot.Clear();

        damageWindowActive = true;
        if (movingVisualCollider != null)
        {
            movingVisualCollider.enabled = true;
        }

        yield return AnimateVisual(0f, travelDistance, 1f, 1f, fireDuration);

        if (activeDamageDuration > fireDuration)
        {
            yield return new WaitForSeconds(activeDamageDuration - fireDuration);
        }

        damageWindowActive = false;
        if (movingVisualCollider != null)
        {
            movingVisualCollider.enabled = false;
        }

        if (reloadDelay > 0f)
        {
            yield return new WaitForSeconds(reloadDelay);
        }

        SetRespawnPoseImmediate();
        yield return AnimateVisual(-respawnOffset, 0f, respawnStartScaleMultiplier, 1f, respawnDuration);
    }

    private IEnumerator AnimateVisual(float fromOffset, float toOffset, float fromScaleMultiplier, float toScaleMultiplier, float duration)
    {
        if (movingVisual == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetPose(toOffset, toScaleMultiplier);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float offset = Mathf.LerpUnclamped(fromOffset, toOffset, eased);
            float scaleMultiplier = Mathf.LerpUnclamped(fromScaleMultiplier, toScaleMultiplier, eased);

            SetPose(offset, scaleMultiplier);
            yield return null;
        }

        SetPose(toOffset, toScaleMultiplier);
    }

    private void SetLoadedPoseImmediate()
    {
        SetPose(0f, 1f);
    }

    private void SetRespawnPoseImmediate()
    {
        SetPose(-respawnOffset, respawnStartScaleMultiplier);
    }

    private void SetPose(float offset, float scaleMultiplier)
    {
        if (movingVisual == null)
        {
            return;
        }

        movingVisual.localPosition = loadedLocalPosition + (GetNormalizedShootDirection() * offset);
        movingVisual.localScale = loadedLocalScale * scaleMultiplier;
    }

    private void CacheReferences()
    {
        if (movingVisual == null)
        {
            movingVisual = FindMovingVisual();
        }

        if (movingVisual != null)
        {
            loadedLocalPosition = movingVisual.localPosition;
            loadedLocalScale = movingVisual.localScale;
            movingVisualCollider = movingVisual.GetComponent<BoxCollider>();
        }
        else
        {
            loadedLocalPosition = Vector3.zero;
            loadedLocalScale = Vector3.one;
            movingVisualCollider = null;
        }
    }

    private void ConfigureDamageCollider()
    {
        if (movingVisual == null)
        {
            return;
        }

        if (movingVisualCollider == null)
        {
            movingVisualCollider = movingVisual.gameObject.AddComponent<BoxCollider>();
        }

        movingVisualCollider.isTrigger = true;
        movingVisualCollider.enabled = false;

        Rigidbody body = movingVisual.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = movingVisual.gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        ArrowTrapDamageProxy proxy = movingVisual.GetComponent<ArrowTrapDamageProxy>();
        if (proxy == null)
        {
            proxy = movingVisual.gameObject.AddComponent<ArrowTrapDamageProxy>();
        }

        proxy.Initialize(this);
    }

    private Transform FindMovingVisual()
    {
        Transform namedChild = transform.Find("SM_PROP_trap_floor_spikes_dungeon_05");
        if (namedChild != null)
        {
            return namedChild;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("SM_PROP_trap_floor_spikes_dungeon_05"))
            {
                return child;
            }
        }

        return transform.childCount > 0 ? transform.GetChild(0) : null;
    }

    private Vector3 GetNormalizedShootDirection()
    {
        return localShootDirection.sqrMagnitude > 0.0001f ? localShootDirection.normalized : Vector3.up;
    }

    private float GetFatalDamage(PlayerHealth playerHealth)
    {
        float effectiveMultiplier = Mathf.Max(0.01f, 1f - playerHealth.DamageReduction);
        return (playerHealth.CurrentHealth / effectiveMultiplier) + 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheReferences();
            if (movingVisualCollider != null)
            {
                movingVisualCollider.isTrigger = true;
            }
        }
    }
#endif
}
