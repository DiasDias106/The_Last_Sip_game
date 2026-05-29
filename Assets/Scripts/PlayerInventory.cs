using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    private const float RedPotionHealAmount = 33f;
    private const float BluePotionHealAmount = 66f;
    public const int MaxPotionCount = 5;

    public static PlayerInventory Instance { get; private set; }

    [Header("Starting Resources")]
    public int startingCoins       = 1000;
    public int startingGems        = 0;
    public int startingRedPotions  = 1;
    public int startingBluePotions = 1;
    public int startingGreenPotions = 1;

    public int Coins        { get; private set; }
    public int Gems         { get; private set; }
    public int RedPotions   { get; private set; }
    public int BluePotions  { get; private set; }
    public int GreenPotions { get; private set; }

    public UnityEvent<int>           onCoinsChanged;
    public UnityEvent<int>           onGemsChanged;
    public UnityEvent<int, int, int> onPotionsChanged;

    void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene")
            Destroy(gameObject);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        onCoinsChanged   ??= new UnityEvent<int>();
        onGemsChanged    ??= new UnityEvent<int>();
        onPotionsChanged ??= new UnityEvent<int, int, int>();

        LoadFromPrefs();
    }

    public void LoadFromPrefs()
    {
        int slot = SaveManager.GetActiveSlot();
        Coins = PlayerPrefs.GetInt($"Inv_Coins_{slot}", startingCoins);
        Gems = PlayerPrefs.GetInt($"Inv_Gems_{slot}", startingGems);
        RedPotions = PlayerPrefs.GetInt($"Inv_RedP_{slot}", startingRedPotions);
        BluePotions = PlayerPrefs.GetInt($"Inv_BlueP_{slot}", startingBluePotions);
        GreenPotions = PlayerPrefs.GetInt($"Inv_GreenP_{slot}", startingGreenPotions);
    }

    public void SaveToPrefs()
    {
        int slot = SaveManager.GetActiveSlot();
        PlayerPrefs.SetInt($"Inv_Coins_{slot}", Coins);
        PlayerPrefs.SetInt($"Inv_Gems_{slot}", Gems);
        PlayerPrefs.SetInt($"Inv_RedP_{slot}", RedPotions);
        PlayerPrefs.SetInt($"Inv_BlueP_{slot}", BluePotions);
        PlayerPrefs.SetInt($"Inv_GreenP_{slot}", GreenPotions);
        PlayerPrefs.Save();
    }

    public static void ClearPrefsForSlot(int slot)
    {
        PlayerPrefs.DeleteKey($"Inv_Coins_{slot}");
        PlayerPrefs.DeleteKey($"Inv_Gems_{slot}");
        PlayerPrefs.DeleteKey($"Inv_RedP_{slot}");
        PlayerPrefs.DeleteKey($"Inv_BlueP_{slot}");
        PlayerPrefs.DeleteKey($"Inv_GreenP_{slot}");
        PlayerPrefs.Save();
    }

    public void AddCoins(int amount)  { Coins += amount; SaveToPrefs(); onCoinsChanged?.Invoke(Coins); }
    public void AddGems(int amount)   { Gems  += amount; SaveToPrefs(); onGemsChanged?.Invoke(Gems); }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        SaveToPrefs();
        onCoinsChanged?.Invoke(Coins);
        return true;
    }

    public bool UseRedPotion()
    {
        if (RedPotions <= 0) return false;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null || playerHealth.CurrentHealth >= playerHealth.maxHealth) return false;

        RedPotions--;
        SaveToPrefs();
        playerHealth.Heal(RedPotionHealAmount);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public bool UseBluePotion()
    {
        if (BluePotions <= 0) return false;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null || playerHealth.CurrentHealth >= playerHealth.maxHealth) return false;

        BluePotions--;
        SaveToPrefs();
        playerHealth.Heal(BluePotionHealAmount);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public bool UseGreenPotion()
    {
        if (GreenPotions <= 0) return false;
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph == null || ph.CurrentHealth >= ph.maxHealth) return false;

        GreenPotions--;
        SaveToPrefs();
        ph.Heal(ph.maxHealth);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public bool CanAddRedPotion(int n = 1) => RedPotions <= MaxPotionCount - n;
    public bool CanAddBluePotion(int n = 1) => BluePotions <= MaxPotionCount - n;
    public bool CanAddGreenPotion(int n = 1) => GreenPotions <= MaxPotionCount - n;

    public void AddRedPotion(int n = 1)
    {
        RedPotions = Mathf.Min(MaxPotionCount, RedPotions + Mathf.Max(0, n));
        SaveToPrefs();
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
    }

    public void AddBluePotion(int n = 1)
    {
        BluePotions = Mathf.Min(MaxPotionCount, BluePotions + Mathf.Max(0, n));
        SaveToPrefs();
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
    }

    public void AddGreenPotion(int n = 1)
    {
        GreenPotions = Mathf.Min(MaxPotionCount, GreenPotions + Mathf.Max(0, n));
        SaveToPrefs();
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
    }
}
