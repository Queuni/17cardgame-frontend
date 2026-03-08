using System;
using System.Threading;
using UnityEngine;

public class MainThread : MonoBehaviour
{
    private static SynchronizationContext unityContext;

    void Awake()
    {
        unityContext = SynchronizationContext.Current;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Run any action safely on Unity's main thread.
    /// </summary>
    public static void Run(Action action)
    {
        if (unityContext == null)
        {
            Debug.LogWarning("UnityMainThread not initialized yet!");
            return;
        }
        unityContext.Post(_ => action?.Invoke(), null);
    }
}
