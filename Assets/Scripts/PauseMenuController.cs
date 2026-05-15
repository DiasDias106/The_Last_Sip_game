using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    private GameObject overlay;
    private bool isPaused;

    private void Start()
    {
        overlay = transform.Find("Overlay")?.gameObject;

        var resumeBtn   = transform.Find("Overlay/Panel/ResumeButton")?.GetComponent<Button>();
        var mainMenuBtn = transform.Find("Overlay/Panel/MainMenuButton")?.GetComponent<Button>();
        var quitBtn     = transform.Find("Overlay/Panel/QuitButton")?.GetComponent<Button>();

        if (resumeBtn   != null) resumeBtn.onClick.AddListener(ResumeGame);
        if (mainMenuBtn != null) mainMenuBtn.onClick.AddListener(LoadMainMenu);
        if (quitBtn     != null) quitBtn.onClick.AddListener(QuitGame);

        if (overlay != null) overlay.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (overlay != null) overlay.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (overlay != null) overlay.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menus");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
