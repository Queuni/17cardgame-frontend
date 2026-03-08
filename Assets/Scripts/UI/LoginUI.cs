using TMPro;
using UnityEngine;

public class LoginUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    private AuthManager authManager;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    private void Start()
    {
        authManager = AuthManager.Instance;

#if !UNITY_WEBGL && (UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        // Only set resolution for desktop platforms (Windows, Mac, Editor)
        // Mobile and WebGL should use native resolution
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(1280, 720, false);
        Screen.fullScreen = false;
#endif

        Spinner.Instance.Hide();
    }

    public async void OnLoginButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            AlertBar.Instance.ShowMessage("Please fill in all fields.");
            return;
        }
        
        if (Utils.IsValidEmail(email) == false)
        {
            AlertBar.Instance.ShowMessage("Invalid email format.");
            return;
        }

        if (!authManager.isFirebaseReady)
        {
            AlertBar.Instance.ShowMessage("Firebase is not ready.");
            return;
        }

        if (!authManager.isServerRunning)
        {
            AlertBar.Instance.ShowMessage("Unable to connect to the server.");
            return;
        }

        // Start API request coroutine
        Spinner.Instance.Show();
        string resultText = await authManager.SignIn(email, password);
        Spinner.Instance.Hide();

        AlertBar.Instance.ShowMessage(resultText);
        if (resultText == "Login successful!")
        {
            authManager.gameMode = GameMode.Online;
            Utils.LoadScene("MainMenu");
        }
    }

    public void OnToRegisterClicked()
    {
        Utils.LoadScene("Register");
    }

    public void OnPlayLocalClicked()
    {
        authManager.gameMode = GameMode.Local;
        Utils.LoadScene("MainMenu");
    }

    public void OnPasswordResetButtonClicked()
    {
        string email = emailInput.text;
       
        if (string.IsNullOrEmpty(email))
        {
            AlertBar.Instance.ShowMessage("Please input your email.");
        }
        else if (Utils.IsValidEmail(email) == false)
        {
            AlertBar.Instance.ShowMessage("Invalid email format");
            return;
        }
        else if (!authManager.isFirebaseReady)
        {
            AlertBar.Instance.ShowMessage("Firebase is not ready.");
            return;
        }
        else
        {
            YesNoDialog.Instance.Show("Send password recovery link to your email?", async () =>
            {
                Spinner.Instance.Show();
                string resultMsg = await authManager.SendPasswordResetRequest(email);
                Spinner.Instance.Hide();

                AlertBar.Instance.ShowMessage(resultMsg);
            }, () =>
            {
            });
        }
    }
}
