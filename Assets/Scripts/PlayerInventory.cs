using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Starting Resources")]
    public int startingCoins       = 0;
    public int startingGems        = 0;
    public int startingRedPotions  = 0;
    public int startingBluePotions = 0;
    public int startingGreenPotions = 0;

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

        Coins        = startingCoins;
        Gems         = startingGems;
        RedPotions   = startingRedPotions;
        BluePotions  = startingBluePotions;
        GreenPotions = startingGreenPotions;
    }

    public void AddCoins(int amount)  { Coins += amount; onCoinsChanged?.Invoke(Coins); }
    public void AddGems(int amount)   { Gems  += amount; onGemsChanged?.Invoke(Gems); }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        onCoinsChanged?.Invoke(Coins);
        return true;
    }

    public bool UseRedPotion()
    {
        if (RedPotions <= 0) return false;
        RedPotions--;
        FindObjectOfType<PlayerHealth>()?.Heal(30f);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public bool UseBluePotion()
    {
        if (BluePotions <= 0) return false;
        BluePotions--;
        FindObjectOfType<PlayerHealth>()?.Heal(60f);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public bool UseGreenPotion()
    {
        if (GreenPotions <= 0) return false;
        GreenPotions--;
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null) ph.Heal(ph.maxHealth);
        onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions);
        return true;
    }

    public void AddRedPotion(int n = 1)   { RedPotions   += n; onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions); }
    public void AddBluePotion(int n = 1)  { BluePotions  += n; onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions); }
    public void AddGreenPotion(int n = 1) { GreenPotions += n; onPotionsChanged?.Invoke(RedPotions, BluePotions, GreenPotions); }
}
