using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth  = 100f;
    public float maxStamina = 100f;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent<float, float> onStaminaChanged;
    public UnityEvent               onDamaged;
    public UnityEvent               onDeath;

    public float CurrentHealth    { get; private set; }
    public float CurrentStamina  { get; private set; }
    public bool  IsDead          { get; private set; }
    public float DamageReduction { get; set; } = 0f;

    void Awake()
    {
        CurrentHealth  = maxHealth;
        CurrentStamina = maxStamina;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount * (1f - DamageReduction));
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        onDamaged?.Invoke();
        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void UseStamina(float amount)
    {
        if (IsDead) return;
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        onStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        if (IsDead) return;
        CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
        onStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    void Die()
    {
        IsDead = true;
        onDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player morreu.");
    }
}
