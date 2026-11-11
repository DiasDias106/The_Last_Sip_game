using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public static class MovementKeyBindings
{
    public enum GameAction
    {
        Forward,
        Left,
        Backward,
        Right,
        Run,
        Interact,
        Attack,
        HeavyAttack,
        Defend,
        Jump,
        Roll
    }
    private static readonly GameAction[] AllActions =
    {
        GameAction.Forward,
        GameAction.Left,
        GameAction.Backward,
        GameAction.Right,
        GameAction.Run,
        GameAction.Interact,
        GameAction.Attack,
        GameAction.HeavyAttack,
        GameAction.Defend,
        GameAction.Jump,
        GameAction.Roll
    };
#if ENABLE_INPUT_SYSTEM
    private static readonly Dictionary<GameAction, string> PrefKeys = new()
    {
        { GameAction.Forward, "Controls.MoveForward" },
        { GameAction.Left, "Controls.MoveLeft" },
        { GameAction.Backward, "Controls.MoveBackward" },
        { GameAction.Right, "Controls.MoveRight" },
        { GameAction.Run, "Controls.Run" },
        { GameAction.Interact, "Controls.Interact" },
        { GameAction.Attack, "Controls.Attack" },
        { GameAction.HeavyAttack, "Controls.HeavyAttack" },
        { GameAction.Defend, "Controls.Defend" },
        { GameAction.Jump, "Controls.Jump" },
        { GameAction.Roll, "Controls.Roll" }
    };
    private static readonly Dictionary<GameAction, Key> DefaultKeys = new()
    {
        { GameAction.Forward, Key.W },
        { GameAction.Left, Key.A },
        { GameAction.Backward, Key.S },
        { GameAction.Right, Key.D },
        { GameAction.Run, Key.LeftShift },
        { GameAction.Interact, Key.F },
        { GameAction.Attack, Key.J },
        { GameAction.HeavyAttack, Key.X },
        { GameAction.Defend, Key.R },
        { GameAction.Jump, Key.Space },
        { GameAction.Roll, Key.C }
    };
    private static readonly Dictionary<GameAction, Key> RuntimeKeys = new();
#endif
    private static bool _loaded;
    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }
#if ENABLE_INPUT_SYSTEM
        RuntimeKeys.Clear();
        foreach (GameAction action in AllActions)
        {
            RuntimeKeys[action] = LoadKey(PrefKeys[action], GetDefaultKey(action));
        }
        const string heavyAttackMigrationKey = "Controls.HeavyAttack.MigratedToX";
        if (!PlayerPrefs.HasKey(heavyAttackMigrationKey)
            && RuntimeKeys.TryGetValue(GameAction.HeavyAttack, out Key heavyKey)
            && heavyKey == Key.K)
        {
            RuntimeKeys[GameAction.HeavyAttack] = Key.X;
            PlayerPrefs.SetString(PrefKeys[GameAction.HeavyAttack], Key.X.ToString());
        }
        PlayerPrefs.SetInt(heavyAttackMigrationKey, 1);
        const string defendMigrationKey = "Controls.Defend.MigratedToR";
        if (!PlayerPrefs.HasKey(defendMigrationKey)
            && RuntimeKeys.TryGetValue(GameAction.Defend, out Key defendKey)
            && defendKey == Key.L)
        {
            RuntimeKeys[GameAction.Defend] = Key.R;
            PlayerPrefs.SetString(PrefKeys[GameAction.Defend], Key.R.ToString());
        }
        PlayerPrefs.SetInt(defendMigrationKey, 1);
        PlayerPrefs.Save();
#endif
        _loaded = true;
    }
    public static IEnumerable<GameAction> EnumerateActions()
    {
        return AllActions;
    }
    public static void ResetToDefaults(bool save)
    {
#if ENABLE_INPUT_SYSTEM
        RuntimeKeys.Clear();
        foreach (GameAction action in AllActions)
        {
            RuntimeKeys[action] = GetDefaultKey(action);
        }
#endif
        _loaded = true;
        if (save)
        {
            Save();
        }
    }
    public static Key GetDefaultKey(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        return DefaultKeys.TryGetValue(action, out Key key) ? key : Key.None;
#else
        return default;
#endif
    }
    public static Key GetKey(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        EnsureLoaded();
        return RuntimeKeys.TryGetValue(action, out Key key) ? key : GetDefaultKey(action);
#else
        return default;
#endif
    }
    public static void SetKey(GameAction action, Key key)
    {
#if ENABLE_INPUT_SYSTEM
        EnsureLoaded();
        GameAction? existingAction = FindActionUsingKey(key);
        if (existingAction.HasValue && existingAction.Value != action)
        {
            RuntimeKeys[existingAction.Value] = GetKey(action);
        }
        RuntimeKeys[action] = key;
#endif
    }
    public static bool IsPressed(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }
        Key key = GetKey(action);
        return key != Key.None && keyboard[key].isPressed;
#else
        return false;
#endif
    }
    public static bool WasPressedThisFrame(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }
        Key key = GetKey(action);
        return key != Key.None && keyboard[key].wasPressedThisFrame;
#else
        return false;
#endif
    }
    public static bool WasReleasedThisFrame(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }
        Key key = GetKey(action);
        return key != Key.None && keyboard[key].wasReleasedThisFrame;
#else
        return false;
#endif
    }
    public static string GetDisplayName(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        return key switch
        {
            Key.UpArrow => "UP",
            Key.DownArrow => "DOWN",
            Key.LeftArrow => "LEFT",
            Key.RightArrow => "RIGHT",
            Key.LeftShift => "SHIFT",
            Key.RightShift => "R SHIFT",
            Key.LeftCtrl => "CTRL",
            Key.RightCtrl => "R CTRL",
            Key.LeftAlt => "ALT",
            Key.RightAlt => "R ALT",
            Key.Space => "SPACE",
            Key.None => "-",
            _ => key.ToString().ToUpperInvariant()
        };
#else
        return "-";
#endif
    }
    public static void Save()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureLoaded();
        foreach (GameAction action in AllActions)
        {
            PlayerPrefs.SetString(PrefKeys[action], GetKey(action).ToString());
        }
        PlayerPrefs.Save();
#endif
    }
#if ENABLE_INPUT_SYSTEM
    private static Key LoadKey(string prefKey, Key fallback)
    {
        string raw = PlayerPrefs.GetString(prefKey, fallback.ToString());
        return System.Enum.TryParse(raw, out Key parsed) ? parsed : fallback;
    }
    private static GameAction? FindActionUsingKey(Key key)
    {
        foreach (KeyValuePair<GameAction, Key> pair in RuntimeKeys)
        {
            if (pair.Value == key)
            {
                return pair.Key;
            }
        }
        return null;
    }
#endif
    public static string GetDisplayName(GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        return GetDisplayName(GetKey(action));
#else
        return action.ToString().ToUpperInvariant();
#endif
    }
}
