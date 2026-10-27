using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The buttons of the main menu.
public class MainMenu : MonoBehaviour
{
    public string firstStageScene = "Stage1_Lab";

    public Button playButton;
    public Button instructionsButton;
    public Button quitButton;
    public Button closeButton;
    public GameObject instructionsPanel;

    void Start()
    {
        instructionsPanel.SetActive(false);

        playButton.onClick.AddListener(() => SceneManager.LoadScene(firstStageScene));
        instructionsButton.onClick.AddListener(() => instructionsPanel.SetActive(true));
        closeButton.onClick.AddListener(() => instructionsPanel.SetActive(false));
        quitButton.onClick.AddListener(Quit);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
