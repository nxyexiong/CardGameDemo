using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerConfigMenuController : MonoBehaviour
{
    private const string SceneName = "PlayerConfigMenu";
    private const string PlayerNameKey = "PlayerName";
    private const string PlayerNameDefaultValue = "Player";

    public InputField PlayerNameInput;
    public Button ReturnButton;

    void Start()
    {
        // load default
        LoadDefaultPlayerConfig();

        // init ui
        PlayerNameInput.text = PlayerPrefs.GetString("PlayerName") ?? string.Empty;

        // init listeners
        PlayerNameInput.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetString(PlayerNameKey, value);
        });
        ReturnButton.onClick.AddListener(() =>
        {
            _ = SceneManager.UnloadSceneAsync(SceneName);
        });
    }

    void Update()
    {
    }

    private void LoadDefaultPlayerConfig()
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerNameKey)))
            PlayerPrefs.SetString(PlayerNameKey, PlayerNameDefaultValue);
    }
}

