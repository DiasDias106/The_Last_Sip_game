using UnityEngine;
using System.Collections;
[DisallowMultipleComponent]
public class PlayerLavaRespawn : MonoBehaviour
{
    [SerializeField] private string lavaTag = "Lava";
    [SerializeField] private string trapBaseNameContains = "base_armadilha";
    [SerializeField] private float sinkSpeed    = 1.2f;
    [SerializeField] private float sinkDuration = 4f;
    private PlayerHealth                _playerHealth;
    private UserImportedPlayerController _playerController;
    private CharacterController          _characterController;
    private bool _isDying;
    private void Awake()
    {
        _playerHealth       = GetComponent<PlayerHealth>();
        _playerController   = GetComponent<UserImportedPlayerController>();
        _characterController = GetComponent<CharacterController>();
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null) return;
        TryTriggerTrap(hit.collider.transform);
        if (_isDying) return;
        if (!IsLava(hit.collider.transform)) return;
        StartCoroutine(SinkAndDie());
    }
    private bool IsLava(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(lavaTag) || t.name.ToLowerInvariant().Contains("lava"))
                return true;
            t = t.parent;
        }
        return false;
    }
    private void TryTriggerTrap(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.name.ToLowerInvariant().Contains(trapBaseNameContains))
            {
                var fall = current.GetComponent<BaseArmadilhaFall>();
                if (fall != null) fall.TriggerFall();
                return;
            }
            current = current.parent;
        }
    }
    private IEnumerator SinkAndDie()
    {
        _isDying = true;
        if (_playerController != null) _playerController.enabled = false;
        if (_characterController != null) _characterController.enabled = false;
        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
            yield return null;
        }
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        if (_playerController != null) _playerController.SuppressDeathSound = true;
        if (_playerHealth != null && !_playerHealth.IsDead)
            _playerHealth.TakeDamage(_playerHealth.maxHealth);
    }
}
