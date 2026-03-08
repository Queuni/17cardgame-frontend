using System;
using TMPro;
using UnityEngine;

public class RegisterUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmInput;
    public TMP_InputField displayNameInput;

    private AuthManager authManager;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update

    private void Start()
    {
        authManager = AuthManager.Instance;
    }

    public void OnRegisterButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirm = confirmInput.text;
        string displayName = displayNameInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)
            || string.IsNullOrEmpty(confirm) || string.IsNullOrEmpty(displayName))
        {
            AlertBar.Instance.ShowMessage("Please fill in all fields.");
            return;
        }

        if (!Utils.IsValidPassword(password))
        {
            AlertBar.Instance.ShowMessage("At least 8 chars, must contain at least one letter and one number.");
            return;
        }

        if (password != confirm)
        {
            AlertBar.Instance.ShowMessage("Your passwords don't match.");
            return;
        }

        if (Utils.IsValidEmail(email) == false)
        {
            AlertBar.Instance.ShowMessage("Invalid email format.");
            return;
        }

        // Start API request coroutine
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

        YesNoDialog.Instance.Show(
            "To continue, you must confirm that you’ve read and agree to the Privacy Policy",
            async () =>
            {
                Spinner.Instance.Show();
                string resultText = await authManager.SignUp(email, password, displayName);
                Spinner.Instance.Hide();

                AlertBar.Instance.ShowMessage(resultText);

                if (!string.IsNullOrEmpty(authManager.profileInfo.email))
                {
                    Utils.LoadScene("Login");
                }
            },
            null);
    }

    public void OnToLoginClicked()
    {
        Utils.LoadScene("Login");
    }

    public void OnPrivacyClicked()
    {
        Application.OpenURL($"{Constants.WEBSITE_URL}privacy-policy");
    }
}
