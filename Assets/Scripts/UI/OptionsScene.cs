using UnityEngine;
using UnityEngine.UI;

public class OptionsScene : MonoBehaviour
{
    // Start is called before the first frame update

    public Toggle autoSuggestToggle;
    public Toggle autoPassToggle;

    public Button leaderboardButton;
    public Button deleteAcountButton;

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        bool isAutoSuggest = PlayerPrefs.GetInt(GameOptionsKeys.AutoSuggest, 1) == 1;
        autoSuggestToggle.isOn = isAutoSuggest;
        autoSuggestToggle.onValueChanged.AddListener(OnAutoSuggestToggleChanged);

        bool isAutoPass = PlayerPrefs.GetInt(GameOptionsKeys.AutoPass, 0) == 1;
        autoPassToggle.isOn = isAutoPass;
        autoPassToggle.onValueChanged.AddListener(OnAutoPassToggleChanged);

        autoPassToggle.gameObject.SetActive(false);

        if (AuthManager.Instance.gameMode == GameMode.Local)
        {
            leaderboardButton.gameObject.SetActive(false);
            deleteAcountButton.gameObject.SetActive(false);
        }
    }

    public void OnAutoSuggestToggleChanged(bool isOn)
    {
        bool isAutoSuggest = autoSuggestToggle.isOn;
        PlayerPrefs.SetInt(GameOptionsKeys.AutoSuggest, isAutoSuggest ? 1 : 0);
    }

    public void OnAutoPassToggleChanged(bool isOn)
    {
        bool isAutoPass = autoPassToggle.isOn;
        PlayerPrefs.SetInt(GameOptionsKeys.AutoPass, isAutoPass ? 1 : 0);
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }

    public void OnLeaderBoardButtonClicked()
    {
        Utils.LoadScene("LeaderBoard");
    }

    public void OnPolicyButtonClicked()
    {   
        Application.OpenURL($"{Constants.WEBSITE_URL}privacy-policy");
    }

    public void OnDeleteAccountClicked()
    {
        Utils.LoadScene("DeleteAccount");
    }

}
