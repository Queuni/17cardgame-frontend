using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Utils
{
    public static string previousSceneName;

    public static void LoadScene(string sceneName, bool destroyAll = true)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            previousSceneName = sceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"Scene '{sceneName}' not found!");
        }
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string password)
    {
        // At least 8 chars, must contain at least one letter and one number
        var pattern = @"^(?=.*[A-Za-z])(?=.*\d)[^\s]{8,}$";
        return Regex.IsMatch(password, pattern);
    }

    public static void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public static void LoadSceneAsDialog(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public static void CloseDialogScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }

    public static string ObjectToJson<T>(T obj)
    {
        return JsonUtility.ToJson(obj, true);
    }

    public static T JsonToObject<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    public static void LogToFile(string message)
    {
        string logPath = Path.Combine(Application.persistentDataPath, "game_log.txt");
        string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
        File.AppendAllText(logPath, logEntry);
    }
}
