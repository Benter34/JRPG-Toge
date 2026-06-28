using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Menu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Fade")]
    [SerializeField] private Screen_Fader screenFader;

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialPanel;

    private bool isLoading;

    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void PlayGame()
    {
        if (isLoading)
            return;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        isLoading = true;

        if (screenFader != null)
            yield return screenFader.FadeIn();

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
    public void Quit()
    {
        QuitGame();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}