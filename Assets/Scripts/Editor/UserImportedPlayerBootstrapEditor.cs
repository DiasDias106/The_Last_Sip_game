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
            // Removed forced loading of level_1 so the user can test masmorra_spawn
            // if (SceneManager.GetActiveScene().name != "level_1")
            // {
            //     EditorSceneManager.LoadScene(Level1Path, LoadSceneMode.Single);
            // }
            UserImportedPlayerBootstrap.EnsurePlayerSpawned();
        }
    }
}
#endif
