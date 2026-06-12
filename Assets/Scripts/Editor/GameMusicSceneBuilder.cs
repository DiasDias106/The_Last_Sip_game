using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class GameMusicSceneBuilder
{
    private const string AudioPath = "Assets/Audio/ImportedFromZip/Audio/";
    private static readonly (string scene, string ambient, string combat, string boss)[] SceneConfigs =
    {
        ("Assets/Scenes/beco_dia.unity",       "becoManha.mp3",      null,             null),
        ("Assets/Scenes/beco_noite.unity",     "becoNoite.mp3",      null,             null),
        ("Assets/Scenes/masmorra_spawn.unity", "Spawn_masmorra.mp3", null,             null),
        ("Assets/Scenes/level_1.unity",        "FundoMasmorras.mp3", "BattleNPC.mp3",  "MiniBoss_Level_1.mp3"),
        ("Assets/Scenes/level_2.unity",        "FundoMasmorras.mp3", "BattleNPC.mp3",  "MiniBoss_Level_2.mp3"),
        ("Assets/Scenes/level_3.unity",        "FundoMasmorras.mp3", "BattleNPC.mp3",  "MiniBoss_Level_3.mp3"),
        ("Assets/Scenes/boss_room.unity",      "FundoMasmorras.mp3", "BattleNPC.mp3",  "MasmorraBoss.mp3"),
    };
    [MenuItem("Tools/Setup Scene Music")]
    public static void SetupAllScenes()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        foreach (var (scenePath, ambientName, combatName, bossName) in SceneConfigs)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var existing in Object.FindObjectsByType<SceneMusicSetup>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);
            var go    = new GameObject("SceneMusicSetup");
            var setup = go.AddComponent<SceneMusicSetup>();
            var so    = new SerializedObject(setup);
            SetClip(so, "ambientMusic", ambientName);
            SetClip(so, "combatMusic",  combatName);
            SetClip(so, "bossMusic",    bossName);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GameMusicSceneBuilder] Configured: {scenePath}");
        }
        if (!string.IsNullOrEmpty(currentScenePath))
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        Debug.Log("[GameMusicSceneBuilder] All scenes configured!");
    }
    private static void SetClip(SerializedObject so, string fieldName, string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + clipName);
        if (clip == null) { Debug.LogWarning($"[GameMusicSceneBuilder] Clip not found: {AudioPath + clipName}"); return; }
        so.FindProperty(fieldName).objectReferenceValue = clip;
    }
}
