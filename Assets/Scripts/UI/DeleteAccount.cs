using UnityEngine;
using System.Threading.Tasks;

public class DeleteAccount : MonoBehaviour
{
    private RequestManager requestManager;
    private AuthManager authManager;

    void Start()
    {
        requestManager = RequestManager.Instance;
        authManager = AuthManager.Instance;

        if (requestManager == null)
        {
            Debug.LogError("DeleteAccount: RequestManager.Instance is null!");
        }

        if (authManager == null)
        {
            Debug.LogError("DeleteAccount: AuthManager.Instance is null!");
        }

        Spinner.Instance.Hide();
    }

    public void OnConfirmClicked()
    {
        YesNoDialog.Instance.Show("Are you sure you want to delete your account? This action cannot be undone.", () =>
        {
            // User confirmed deletion
            DeleteUserAccount();
        }, () =>
        {
            // User cancelled
            // Do nothing
        });
    }

    private async void DeleteUserAccount()
    {
        // Validate managers
        if (requestManager == null)
        {
            requestManager = RequestManager.Instance;
            if (requestManager == null)
            {
                AlertBar.Instance.ShowMessage("Request manager not available. Please restart the app.");
                return;
            }
        }

        if (authManager == null)
        {
            authManager = AuthManager.Instance;
            if (authManager == null)
            {
                AlertBar.Instance.ShowMessage("Auth manager not available. Please restart the app.");
                return;
            }
        }

        // Check if user is authenticated
        if (authManager.profileInfo == null)
        {
            AlertBar.Instance.ShowMessage("You must be logged in to delete your account.");
            return;
        }

        try
        {
            // Show spinner
            Spinner.Instance.Show();

            // Get Firebase ID token for authentication
            string token = await authManager.GetIdToken();
            if (string.IsNullOrEmpty(token))
            {
                Spinner.Instance.Hide();
                AlertBar.Instance.ShowMessage("Authentication failed. Please log in again.");
                return;
            }

            // Call delete account API
            string url = "auth/delete-account";
            string response = await requestManager.DeleteRequestAsync(url, token);

            Spinner.Instance.Hide();

            if (string.IsNullOrEmpty(response))
            {
                AlertBar.Instance.ShowMessage("Failed to delete account. Please try again.");
                return;
            }

            // Parse response
            ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(response);
            
            if (resultInfo != null && resultInfo.status == "success")
            {
                // Account deleted successfully
                AlertBar.Instance.ShowMessage("Account deleted successfully.");

                // Clear profile info
                if (authManager.profileInfo != null)
                {
                    authManager.profileInfo = null;
                }

                // Wait a moment for the message to be visible
                await Task.Delay(1500);

                // Navigate to login scene
                Utils.LoadScene("Login");
            }
            else
            {
                // Show error message
                string errorMsg = resultInfo?.msg ?? "Failed to delete account. Please try again.";
                AlertBar.Instance.ShowMessage(errorMsg);
            }
        }
        catch (System.Exception e)
        {
            Spinner.Instance.Hide();
            Debug.LogError($"DeleteAccount error: {e.Message}");
            AlertBar.Instance.ShowMessage("An error occurred while deleting your account. Please try again.");
        }
    }

    public void OnBackClicked()
    {
        Utils.LoadScene("Options");
    }
}
