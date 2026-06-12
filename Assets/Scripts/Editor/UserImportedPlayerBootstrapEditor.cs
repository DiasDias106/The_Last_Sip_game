#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class UserImportedPlayerBootstrapEditor
{
    private const string Level1Path = "Assets/Scenes/level_1.unity";
    static UserImportedPlayerBootstrapEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            UserImportedPlayerBootstrap.EnsurePlayerSpawned();
        }
    }
}
#endif
