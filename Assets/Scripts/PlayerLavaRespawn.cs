using UnityEngine;

[DisallowMultipleComponent]
public class PlayerLavaRespawn : MonoBehaviour
{
    [SerializeField] private string lavaTag = "Lava";
    [SerializeField] private string trapBaseNameContains = "base_armadilha";

    private PlayerHealth _playerHealth;
    private bool _isDying;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null)
        {
            return;
        }

        TryTriggerTrap(hit.collider.transform);

        if (_isDying)
        {
            return;
        }

        if (!IsLava(hit.collider.transform))
        {
            return;
        }

        KillPlayer();
    }

    private bool IsLava(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(lavaTag) || t.name.ToLowerInvariant().Contains("lava"))
            {
                return true;
            }

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
                BaseArmadilhaFall fall = current.GetComponent<BaseArmadilhaFall>();
                if (fall != null)
                {
                    fall.TriggerFall();
                }
                return;
            }

            current = current.parent;
        }
    }

    private void KillPlayer()
    {
        _isDying = true;

        if (_playerHealth != null && !_playerHealth.IsDead)
        {
            _playerHealth.TakeDamage(_playerHealth.maxHealth);
        }
    }
}
