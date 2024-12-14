using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string PlayerConfigSceneName = "PlayerConfigMenu";
    private const string StartClosedGameSceneName = "StartClosedGameMenu";

    public Button StartClosedGameButton;
    public Button PlayerConfigButton;
    public Button QuitButton;

    void Start()
    {
        // add listeners
        StartClosedGameButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(StartClosedGameSceneName, LoadSceneMode.Additive);
        });
        PlayerConfigButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(PlayerConfigSceneName, LoadSceneMode.Additive);
        });
        QuitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

    void Update()
    {
    }
}
