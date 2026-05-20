using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    public float CurrentHealth { get; private set; }
    public bool  IsDead        { get; private set; }

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Die()
    {
        IsDead = true;
        onDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player morreu.");
    }
}
