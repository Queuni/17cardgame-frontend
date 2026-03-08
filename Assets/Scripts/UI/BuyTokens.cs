#if !UNITY_WEBGL
using Firebase.Firestore;
#endif

using TMPro;
using UnityEngine;

public class BuyTokens : MonoBehaviour
{
    RequestManager requestManager;
    AuthManager authManager;
    IPaymentService paymentService;

    public TextMeshProUGUI myTokenText;

#if !UNITY_WEBGL
    private ListenerRegistration tokenListener;
#endif

    // Start is called before the first frame update

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    private void Start()
    {
        requestManager = RequestManager.Instance;
        authManager = AuthManager.Instance;

        // Check if managers are initialized
        if (requestManager == null)
        {
            Debug.LogError("RequestManager.Instance is null!");
            AlertBar.Instance?.ShowMessage("RequestManager not initialized.");
            return;
        }

        if (authManager == null)
        {
            Debug.LogError("AuthManager.Instance is null!");
            AlertBar.Instance?.ShowMessage("AuthManager not initialized.");
            return;
        }

        // Check if profileInfo is available
        if (authManager.profileInfo == null)
        {
            Debug.LogError("authManager.profileInfo is null!");
            AlertBar.Instance?.ShowMessage("Profile info not loaded. Please login again.");
            return;
        }

        // Pick the right payment method for this device
        paymentService = PaymentServiceFactory.GetPaymentService();
        if (paymentService != null)
        {
            paymentService.Initialize((success) =>
            {
                if (success)
                {
                    Debug.Log("Payment service initialized successfully");
                }
                else
                {
                    Debug.LogError("Payment service initialization failed");
                    AlertBar.Instance?.ShowMessage("Payment service unavailable. Please restart the app.");
                }
            });
        }
        else
        {
            Debug.LogError("Failed to get payment service");
        }

        GetMyTokenAmount();
        AddTokenListener();
    }

    private void AddTokenListener()
    {
#if !UNITY_WEBGL
        if (authManager == null || authManager.profileInfo == null)
        {
            Debug.LogError("Cannot add token listener: authManager or profileInfo is null");
            return;
        }

        if (string.IsNullOrEmpty(authManager.profileInfo.playerId))
        {
            Debug.LogError("Cannot add token listener: playerId is null or empty");
            return;
        }

        if (myTokenText == null)
        {
            Debug.LogError("Cannot add token listener: myTokenText is not assigned");
            return;
        }

        string playerId = authManager.profileInfo.playerId;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DocumentReference docRef = db.Collection("players").Document(playerId);

        tokenListener = docRef.Listen(snapshot =>
        {
            if (!snapshot.Exists)
            {
                Debug.Log("Document does not exist!");
                return;
            }

            // Get token value
            int tokens = snapshot.GetValue<int>("token");

            // Update your UI
            if (myTokenText != null)
            {
                myTokenText.text = tokens.ToString();
            }
            //AlertBar.Instance.ShowMessage("Your token is updated.");
        });
#endif
    }

    private async void GetMyTokenAmount()
    {
        if (authManager == null || authManager.profileInfo == null)
        {
            Debug.LogError("Cannot get token amount: authManager or profileInfo is null");
            AlertBar.Instance?.ShowMessage("Profile info not loaded.");
            return;
        }

        if (string.IsNullOrEmpty(authManager.profileInfo.email))
        {
            Debug.LogError("Cannot get token amount: email is null or empty");
            AlertBar.Instance?.ShowMessage("Email not available.");
            return;
        }

        if (myTokenText == null)
        {
            Debug.LogError("Cannot get token amount: myTokenText is not assigned");
            return;
        }

        string email = authManager.profileInfo.email;
        string url = $"auth/my-tokens?email={email}";

        Spinner.Instance?.Show();
        string token = await authManager.GetIdToken();
        string response = await requestManager.GetRequestAsync(url, token);
        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(response);
        Spinner.Instance?.Hide();

        if (resultInfo != null && resultInfo.result != null)
        {
            myTokenText.text = resultInfo.result.ToString();
        }
        else
        {
            AlertBar.Instance?.ShowMessage("Failed get tokens.");
        }
    }

    public void OnBuy20TokenClicked()
    {
        if (YesNoDialog.Instance == null)
        {
            Debug.LogError("YesNoDialog.Instance is null!");
            return;
        }
        YesNoDialog.Instance.Show("Buy 20 tokens for $10?", () =>
        {
            SendBuyTokensRequest(20, 10);
        }
            , () =>
              {

              });
    }

    public void OnBuy50TokenClicked()
    {
        if (YesNoDialog.Instance == null)
        {
            Debug.LogError("YesNoDialog.Instance is null!");
            return;
        }
        YesNoDialog.Instance.Show("Buy 50 tokens for $20?", () =>
        {
            SendBuyTokensRequest(50, 20);
        }
            , () =>
            {

            });
    }

    public void OnBuy100TokenClicked()
    {
        if (YesNoDialog.Instance == null)
        {
            Debug.LogError("YesNoDialog.Instance is null!");
            return;
        }
        YesNoDialog.Instance.Show("Buy 100 tokens for $40?", () =>
        {
            SendBuyTokensRequest(100, 40);
        }
            , () =>
            {

            });
    }

    private void SendBuyTokensRequest(int tokenAmount, int price)
    {
        // Make sure payment service is ready
        if (paymentService == null)
        {
            Debug.LogError("Payment service is not available");
            AlertBar.Instance?.ShowMessage("Payment service not initialized. Please wait and try again.");
            return;
        }

        if (!paymentService.IsAvailable)
        {
            AlertBar.Instance?.ShowMessage("Purchases are not available on this platform.");
            return;
        }

        if (!paymentService.IsInitialized)
        {
            AlertBar.Instance?.ShowMessage("Payment service is initializing. Please wait a moment and try again.");
            return;
        }

        // Start purchase - uses IAP on mobile, Stripe on WebGL
        paymentService.PurchaseTokens(
            tokenAmount,
            price,
            onSuccess: () =>
            {
                Debug.Log($"Successfully purchased {tokenAmount} tokens");
                AlertBar.Instance?.ShowMessage($"Successfully purchased {tokenAmount} tokens!");
                
                // Refresh token display
                GetMyTokenAmount();
            },
            onFailed: (errorMessage) =>
            {
                Debug.LogError($"Purchase failed: {errorMessage}");
                AlertBar.Instance?.ShowMessage($"Purchase failed: {errorMessage}");
            }
        );
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
#if !UNITY_WEBGL
        if (tokenListener != null)
        {
            tokenListener.Stop();
            tokenListener = null;
        }
#endif
    }
}
