using System.Collections;
using UnityEngine;

public class BossEntranceDoorSeal : MonoBehaviour
{
    public enum TriggerMode
    {
        CrossAnchor,
        AnchorProximity,
        BossProximity
    }

    [SerializeField] private float armDistance = 4.5f;
    [SerializeField] private float crossDistance = 1.2f;
    [SerializeField] private float largeDoorCloseDuration = 1.2f;
    [SerializeField] private float bossTriggerDistance = 7f;

    private BossController[] _bosses;
    private DoorInteractable _doorInteractable;
    private Transform _largeDoorRoot;
    private Transform _sealAnchor;
    private Transform _proximityTarget;
    private Transform _leftLeaf;
    private Transform _rightLeaf;
    private Transform _player;
    private PaladinFinalBossController _finalBoss;
    private bool _triggered;
    private bool _armed;
    private float _initialSide;
    private TriggerMode _triggerMode;

    public void Initialize(
        BossController[] bosses,
        DoorInteractable doorInteractable,
        Transform largeDoorRoot,
        Transform sealAnchor,
        TriggerMode triggerMode = TriggerMode.CrossAnchor,
        Transform proximityTarget = null,
        PaladinFinalBossController finalBoss = null)
    {
        _bosses = bosses;
        _doorInteractable = doorInteractable;
        _largeDoorRoot = largeDoorRoot;
        _sealAnchor = sealAnchor != null ? sealAnchor : (doorInteractable != null ? doorInteractable.transform : largeDoorRoot);
        _triggerMode = triggerMode;
        _proximityTarget = proximityTarget;
        _finalBoss = finalBoss;

        if (_largeDoorRoot != null)
        {
            _leftLeaf = FindChildRecursive(_largeDoorRoot, "MOD_Gateway_Door_01_large_door_L");
            _rightLeaf = FindChildRecursive(_largeDoorRoot, "MOD_Gateway_Door_01_large_door_R");
        }
    }

    private void Update()
    {
        if (_triggered)
        {
            return;
        }

        if (_bosses == null || _bosses.Length == 0)
        {
            _bosses = null;
        }

        if (_player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
            {
                return;
            }

            _player = playerGo.transform;
        }

        if (_sealAnchor == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 toPlayer = _player.position - _sealAnchor.position;
        float distanceToAnchor = toPlayer.magnitude;

        if (_triggerMode == TriggerMode.BossProximity)
        {
            if (IsPlayerNearAnyBoss())
            {
                TriggerSeal();
            }

            return;
        }

        if (_triggerMode == TriggerMode.AnchorProximity)
        {
            if (distanceToAnchor <= armDistance)
            {
                TriggerSeal();
            }

            return;
        }

        if (!_armed)
        {
            if (distanceToAnchor <= armDistance)
            {
                float dot = Vector3.Dot(_sealAnchor.forward, toPlayer);
                _initialSide = Mathf.Approximately(dot, 0f) ? 1f : Mathf.Sign(dot);
                _armed = true;
            }

            return;
        }

        float currentDot = Vector3.Dot(_sealAnchor.forward, toPlayer);
        float currentSide = Mathf.Approximately(currentDot, 0f) ? _initialSide : Mathf.Sign(currentDot);
        if (currentSide != _initialSide && Mathf.Abs(currentDot) >= crossDistance)
        {
            TriggerSeal();
        }
    }

    private void TriggerSeal()
    {
        _triggered = true;

        _finalBoss?.RevealEncounterUI();

        if (_bosses != null)
        {
            for (int i = 0; i < _bosses.Length; i++)
            {
                if (_bosses[i] != null)
                {
                    _bosses[i].ActivateArena();
                }
            }
        }

        if (_doorInteractable != null)
        {
            _doorInteractable.ForceCloseAndLock();
        }

        if (_leftLeaf != null || _rightLeaf != null)
        {
            StartCoroutine(CloseLargeDoorRoutine());
        }
        else
        {
            enabled = false;
        }
    }

    private IEnumerator CloseLargeDoorRoutine()
    {
        Quaternion leftStart = _leftLeaf != null ? _leftLeaf.localRotation : Quaternion.identity;
        Quaternion rightStart = _rightLeaf != null ? _rightLeaf.localRotation : Quaternion.identity;
        Quaternion closed = Quaternion.identity;
        float elapsed = 0f;

        while (elapsed < largeDoorCloseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / largeDoorCloseDuration);

            if (_leftLeaf != null)
            {
                _leftLeaf.localRotation = Quaternion.Slerp(leftStart, closed, t);
            }

            if (_rightLeaf != null)
            {
                _rightLeaf.localRotation = Quaternion.Slerp(rightStart, closed, t);
            }

            yield return null;
        }

        if (_leftLeaf != null)
        {
            _leftLeaf.localRotation = closed;
        }

        if (_rightLeaf != null)
        {
            _rightLeaf.localRotation = closed;
        }

        Collider[] colliders = _largeDoorRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
            }
        }

        enabled = false;
    }

    private bool IsPlayerNearAnyBoss()
    {
        if (_proximityTarget != null)
        {
            Vector3 offset = _player.position - _proximityTarget.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= bossTriggerDistance * bossTriggerDistance;
        }

        if (_bosses == null || _bosses.Length == 0)
        {
            return false;
        }

        float maxDistanceSq = bossTriggerDistance * bossTriggerDistance;
        for (int i = 0; i < _bosses.Length; i++)
        {
            BossController boss = _bosses[i];
            if (boss == null)
            {
                continue;
            }

            Vector3 offset = _player.position - boss.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= maxDistanceSq)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == targetName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
