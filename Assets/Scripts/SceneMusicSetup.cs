using System.Collections;
using UnityEngine;

public sealed class SceneMusicSetup : MonoBehaviour
{
    [SerializeField] private AudioClip ambientMusic;
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] [Range(0.1f, 5f)] private float volumeMultiplier = 1f;

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() != gameObject.scene)
            return;

        foreach (var m in Object.FindObjectsByType<MenuAudioManager>(FindObjectsSortMode.None))
            m.SetAmbientMusicClip(null);

        StartCoroutine(DelayedMusicStart());
    }

    private IEnumerator DelayedMusicStart()
    {
        // Aguardar o fim do frame atual para garantir que a scene está visível antes de iniciar a música
        yield return new WaitForEndOfFrame();
        GameMusicManager.GetOrCreate().SetupForScene(ambientMusic, combatMusic, bossMusic, volumeMultiplier);
    }
}
