using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerDeathSceneLoader : MonoBehaviour
{
    [SerializeField] private string deathSceneName = "masmorra_spawn";

    private PlayerHealth _playerHealth;
    private bool _loading;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.onDeath.AddListener(LoadDeathScene);
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.onDeath.RemoveListener(LoadDeathScene);
        }
    }

    private void LoadDeathScene()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        SceneManager.LoadScene(deathSceneName);
    }
}
