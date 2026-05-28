using UnityEngine;

public sealed class SceneMusicSetup : MonoBehaviour
{
    [SerializeField] private AudioClip ambientMusic;
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private AudioClip bossMusic;

    private void Start()
    {
        foreach (var m in Object.FindObjectsByType<MenuAudioManager>(FindObjectsSortMode.None))
            m.SetAmbientMusicClip(null);

        GameMusicManager.GetOrCreate().SetupForScene(ambientMusic, combatMusic, bossMusic);
    }
}
