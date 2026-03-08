using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
public class RequestManager : MonoBehaviour
{
    public static RequestManager Instance;

    // Start is called before the first frame update
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // stays across scenes
        }
        else
        {
            Destroy(gameObject); // prevent duplicates
        }
    }

    public async Task<string> GetRequestAsync(string mainUrl, string token = null, int timeout = 10)
    {
        string baseUrl = Debug.isDebugBuild ? Constants.LOCAL_HTTP_URL : Constants.SERVER_HTTPS_URL;
        string url = baseUrl + mainUrl;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Optional Token Authentication
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");

            if (Debug.isDebugBuild)
            {
                // Accept all SSL certificates only for DEV/STAGING
                request.certificateHandler = new AcceptAllCertificates();
            }

#if UNITY_WEBGL
            request.timeout = 20;
#else
            request.timeout = timeout; // ✅ Timeout protection
#endif

            var operation = request.SendWebRequest();

            // Await request completion safely
            while (!operation.isDone)
                await Task.Yield();

            // ✅ Success case
            string response = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                response = request.downloadHandler.text;
                return response;
            }

            // ❌ Error cases — centralized reporting
            string errorText = request.downloadHandler?.text;
            long responseCode = request.responseCode;
            
            if (string.IsNullOrEmpty(errorText))
            {
                Spinner.Instance?.Hide();
                AlertBar.Instance?.ShowMessage("Unable to connect to the server.");
                Debug.LogError($"GetRequestAsync failed: No response body. Status: {responseCode}, Error: {request.error}");
            }
            else
            {
                // Log server error response for debugging
                Debug.LogError($"GetRequestAsync failed: Status {responseCode}, Response: {errorText}");
            }

            return null;
        }
    }

    public async Task<string> PostRequestAsync(string mainUrl, string json, string token = null, int timeout = 5)
    {
        string baseUrl = Debug.isDebugBuild ? Constants.LOCAL_HTTP_URL : Constants.SERVER_HTTPS_URL;
        string url = baseUrl + mainUrl;
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // Convert JSON body to bytes
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // SSL certificate — dev only!
            if (Debug.isDebugBuild)
            {
                request.certificateHandler = new AcceptAllCertificates();
            }

            // Optional Authorization
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");

#if UNITY_WEBGL
            request.timeout = 20;
#else
            request.timeout = timeout;
#endif

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // ✅ async friendly

            // ✅ Success
            string response = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                response = request.downloadHandler.text;
                return response;
            }

            // ❌ Error handling

            if (string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Spinner.Instance.Hide();
                AlertBar.Instance.ShowMessage("Unable to connect to the server.");
            }

            return null;
        }
    }

    public async Task<string> DeleteRequestAsync(string mainUrl, string token = null, int timeout = 10)
    {
        string baseUrl = Debug.isDebugBuild ? Constants.LOCAL_HTTP_URL : Constants.SERVER_HTTPS_URL;
        string url = baseUrl + mainUrl;
        using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            // SSL certificate — dev only!
            if (Debug.isDebugBuild)
            {
                request.certificateHandler = new AcceptAllCertificates();
            }

            // Optional Authorization
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");

#if UNITY_WEBGL
            request.timeout = 20;
#else
            request.timeout = timeout;
#endif

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            // ✅ Success
            string response = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                response = request.downloadHandler.text;
                return response;
            }

            // ❌ Error handling
            string errorText = request.downloadHandler?.text;
            long responseCode = request.responseCode;

            if (string.IsNullOrEmpty(errorText))
            {
                Spinner.Instance?.Hide();
                AlertBar.Instance?.ShowMessage("Unable to connect to the server.");
                Debug.LogError($"DeleteRequestAsync failed: No response body. Status: {responseCode}, Error: {request.error}");
            }
            else
            {
                Debug.LogError($"DeleteRequestAsync failed: Status {responseCode}, Response: {errorText}");
            }

            return null;
        }
    }


    public async Task<FirebaseSignResponse> FirebasePost(string url, string jsonBody)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        // Parse response (Firebase may return 200 OK even with errors in the JSON)
        FirebaseSignResponse data;
        string responseText = req.downloadHandler.text;

        try
        {
            data = Utils.JsonToObject<FirebaseSignResponse>(responseText);

            // Debug: Log the response for troubleshooting
            Debug.Log($"FirebasePost response: Success={req.result == UnityWebRequest.Result.Success}, HasError={data.error != null}, HasIdToken={!string.IsNullOrEmpty(data.idToken)}");
        }
        catch (Exception ex)
        {
            // If parsing fails, create error response
            Debug.Log($"FirebasePost JSON parse error: {ex.Message}, Response: {responseText}");
            return new FirebaseSignResponse
            {
                error = new FirebaseError
                {
                    message = req.result != UnityWebRequest.Result.Success
                        ? req.error ?? responseText
                        : responseText
                }
            };
        }

        // If HTTP request failed and no error in response, add one
        if (req.result != UnityWebRequest.Result.Success && data.error == null)
        {
            data.error = new FirebaseError
            {
                message = req.error ?? responseText
            };
        }

        return data;
    }

    public class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
