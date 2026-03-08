using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileEditScene : MonoBehaviour
{

    public TMP_InputField displayNameInput;
    public Image currrentAvatar;
    public Transform avatarParent;
    public Button buttonPrefab;

    private AuthManager authManager;
    private RequestManager requestManager;

    private int avatarIndex = 0;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update
    void Start()
    {
        authManager = AuthManager.Instance;  
        requestManager = RequestManager.Instance;

        if (displayNameInput != null )
        {
            displayNameInput.text = authManager.profileInfo.displayName;
        }
        avatarIndex = authManager.profileInfo.avatarIndex;
        currrentAvatar.sprite = authManager.getAvatarSprite(avatarIndex);

        CreateAvatarButtons();

        Spinner.Instance.Hide();
    }

    private void CreateAvatarButtons()
    {
        for (int i = 0; i < authManager.avatarCount; i++)
        {
            Button avatarButton = Instantiate(buttonPrefab, avatarParent);
            Image img = avatarButton.GetComponent<Image>();

            img.sprite = authManager.getAvatarSprite(i);

            int index = i;

            avatarButton.onClick.AddListener(() => OnSampleAvatarClicked(index));
        }
    }

    public void OnSampleAvatarClicked(int index)
    {
        avatarIndex = index;

        if (index < authManager.avatarCount)
        {
            currrentAvatar.sprite = authManager.getAvatarSprite(index);
        }
    }

    public async void OnUpdateButtonClicked()
    {
        string displayName = displayNameInput.text;
        string playerId = authManager.profileInfo.playerId;

        // Validate display name
        if (string.IsNullOrEmpty(displayName))
        {
            AlertBar.Instance.ShowMessage("Display name cannot be empty.");
            return;
        }

        // Check if display name is changed
        if (displayName != authManager.profileInfo.displayName)
        {
            bool isExist = await authManager.checkUsernameExist(displayName);
            if (isExist)
            {
                AlertBar.Instance.ShowMessage("Display name is already taken.");
                return;
            }
        }

        UpdateProfileInfo profileInfo = new UpdateProfileInfo
        {
            playerId = playerId,
            displayName = displayName,
            avatarIndex = avatarIndex,
        };

        string profileJson = Utils.ObjectToJson(profileInfo);

        string token = await authManager.GetIdToken();
        string url = "auth/profile";

        Spinner.Instance.Show();
        string response = await requestManager.PostRequestAsync(url, profileJson, token);
        Spinner.Instance.Hide();

        AlertBar.Instance.ShowMessage("Profile updated.");

        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(response);
        if (resultInfo.result == "success")
        {
            authManager.profileInfo.avatarIndex = avatarIndex;
            authManager.profileInfo.displayName = displayName;
            Utils.LoadScene("MainMenu");
        }
        else
        {
            AlertBar.Instance.ShowMessage("Failed to save profile.");
        }
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }
}
