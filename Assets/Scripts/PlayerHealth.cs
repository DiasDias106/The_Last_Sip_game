using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    private static bool s_hasPendingSceneState;
    private static float s_pendingHealth;
    private static float s_pendingStamina;
    private static string s_pendingTargetSceneName;

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
        onHealthChanged  ??= new UnityEvent<float, float>();
        onStaminaChanged ??= new UnityEvent<float, float>();
        onDamaged        ??= new UnityEvent();
        onDeath          ??= new UnityEvent();

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (s_hasPendingSceneState && string.Equals(s_pendingTargetSceneName, activeSceneName, System.StringComparison.Ordinal))
        {
            CurrentHealth = Mathf.Clamp(s_pendingHealth, 0f, maxHealth);
            CurrentStamina = Mathf.Clamp(s_pendingStamina, 0f, maxStamina);
            ClearPreservedState();
        }
        else
        {
            CurrentHealth = maxHealth;
            CurrentStamina = maxStamina;
        }
    }

    public static void PreserveCurrentForSceneTransition(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            return;
        }

        PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        s_hasPendingSceneState = true;
        s_pendingHealth = playerHealth.CurrentHealth;
        s_pendingStamina = playerHealth.CurrentStamina;
        s_pendingTargetSceneName = targetSceneName;
    }

    public static void ClearPreservedState()
    {
        s_hasPendingSceneState = false;
        s_pendingHealth = 0f;
        s_pendingStamina = 0f;
        s_pendingTargetSceneName = null;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount * (1f - DamageReduction));
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        onDamaged?.Invoke();
        PlayerHUDController.Instance?.RefreshHealth(CurrentHealth, maxHealth);
        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
        PlayerHUDController.Instance?.RefreshHealth(CurrentHealth, maxHealth);
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
