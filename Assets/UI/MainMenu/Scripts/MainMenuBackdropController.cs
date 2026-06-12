using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public sealed class MainMenuBackdropController : MonoBehaviour
{
    [SerializeField] private string backdropSceneName = "beco_noite";
    [SerializeField] private Camera menuCamera;
    [SerializeField] private AudioListener menuAudioListener;
    [SerializeField] private Color fallbackLightColor = new(0.45f, 0.5f, 0.6f, 1f);
    [SerializeField] private float fallbackLightIntensity = 0.7f;
    [SerializeField] private Vector3 fallbackLightEulerAngles = new(28f, -118f, 0f);
    private static readonly string[] DisabledBackdropRoots =
    {
        "EventSystem",
        "PauseMenuCanvas",
        "BottlePickup_Beco",
        "SceneMusicSetup",
        "LimoNightSequence",
        "LimoPreview"
    };
    private Light fallbackLight;
    private void Awake()
    {
        ResolveMenuCameraReferences();
        StartCoroutine(EnsureBackdropSceneRoutine());
    }
    private IEnumerator EnsureBackdropSceneRoutine()
    {
        if (string.IsNullOrWhiteSpace(backdropSceneName) || !Application.CanStreamedLevelBeLoaded(backdropSceneName))
        {
            yield break;
        }
        Scene backdropScene = SceneManager.GetSceneByName(backdropSceneName);
        if (!backdropScene.IsValid() || !backdropScene.isLoaded)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(backdropSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                yield break;
            }
            while (!loadOperation.isDone)
            {
                yield return null;
            }
            backdropScene = SceneManager.GetSceneByName(backdropSceneName);
        }
        if (!backdropScene.IsValid() || !backdropScene.isLoaded)
        {
            yield break;
        }
        DisableMenuCamera();
        ConfigureBackdropScene(backdropScene);
        HideMainMenuBackground();
    }
    private void ConfigureBackdropScene(Scene backdropScene)
    {
        foreach (GameObject rootObject in backdropScene.GetRootGameObjects())
        {
            if (ShouldDisableRoot(rootObject.name))
            {
                rootObject.SetActive(false);
                continue;
            }
            foreach (AudioSource audioSource in rootObject.GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.enabled = false;
            }
        }
        EnsureFallbackLight(backdropScene);
    }
    private static bool ShouldDisableRoot(string rootName)
    {
        for (int i = 0; i < DisabledBackdropRoots.Length; i++)
        {
            if (rootName == DisabledBackdropRoots[i])
            {
                return true;
            }
        }
        return false;
    }
    private void HideMainMenuBackground()
    {
        GameObject mainMenuPanel = GameObject.Find("MainMenuPanel");
        if (mainMenuPanel == null)
        {
            return;
        }
        Transform backgroundTransform = mainMenuPanel.transform.Find("Background");
        if (backgroundTransform == null || !backgroundTransform.TryGetComponent(out Image backgroundImage))
        {
            return;
        }
        backgroundImage.enabled = false;
    }
    private void DisableMenuCamera()
    {
        if (menuCamera != null)
        {
            menuCamera.enabled = false;
        }
        if (menuAudioListener != null)
        {
            menuAudioListener.enabled = false;
        }
    }
    private void ResolveMenuCameraReferences()
    {
        if (menuCamera == null && Camera.main != null && Camera.main.gameObject.scene == gameObject.scene)
        {
            menuCamera = Camera.main;
        }
        if (menuAudioListener == null && menuCamera != null)
        {
            menuAudioListener = menuCamera.GetComponent<AudioListener>();
        }
    }
    private void EnsureFallbackLight(Scene backdropScene)
    {
        if (HasActiveLight(backdropScene))
        {
            if (fallbackLight != null)
            {
                fallbackLight.enabled = false;
            }
            return;
        }
        if (fallbackLight == null)
        {
            GameObject lightObject = new("MenuBackdropLight", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(fallbackLightEulerAngles);
            fallbackLight = lightObject.GetComponent<Light>();
            fallbackLight.type = LightType.Directional;
            fallbackLight.shadows = LightShadows.None;
        }
        fallbackLight.color = fallbackLightColor;
        fallbackLight.intensity = fallbackLightIntensity;
        fallbackLight.enabled = true;
    }
    private static bool HasActiveLight(Scene backdropScene)
    {
        foreach (GameObject rootObject in backdropScene.GetRootGameObjects())
        {
            foreach (Light lightComponent in rootObject.GetComponentsInChildren<Light>(true))
            {
                if (lightComponent.enabled && lightComponent.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
