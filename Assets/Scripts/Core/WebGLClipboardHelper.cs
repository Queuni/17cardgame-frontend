using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLClipboardHelper
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_Copy(string text);

    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_EnablePaste(string canvasId, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_DisablePaste(string canvasId);

    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_ReadClipboard(string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_EnableKeyboard(string canvasId, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void ClipboardWebGL_DisableKeyboard();

    public static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("WebGLClipboardHelper: Attempted to copy empty text");
            return;
        }

        try
        {
            ClipboardWebGL_Copy(text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to copy text: {e.Message}");
        }
    }

    public static void ReadClipboard(string callbackObjectName)
    {
        if (string.IsNullOrEmpty(callbackObjectName))
        {
            Debug.LogWarning("WebGLClipboardHelper: Callback object name is empty");
            return;
        }

        try
        {
            ClipboardWebGL_ReadClipboard(callbackObjectName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to read clipboard: {e.Message}");
        }
    }

    public static void EnablePasteSupport(string canvasId, string callbackObjectName)
    {
        if (string.IsNullOrEmpty(canvasId) || string.IsNullOrEmpty(callbackObjectName))
        {
            Debug.LogWarning("WebGLClipboardHelper: Canvas ID or callback object name is empty");
            return;
        }

        try
        {
            ClipboardWebGL_EnablePaste(canvasId, callbackObjectName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to enable paste support: {e.Message}");
        }
    }

    public static void DisablePasteSupport(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId))
        {
            return;
        }

        try
        {
            ClipboardWebGL_DisablePaste(canvasId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to disable paste support: {e.Message}");
        }
    }

    public static void EnableKeyboardSupport(string canvasId, string callbackObjectName)
    {
        if (string.IsNullOrEmpty(canvasId) || string.IsNullOrEmpty(callbackObjectName))
        {
            Debug.LogWarning("WebGLClipboardHelper: Canvas ID or callback object name is empty");
            return;
        }

        try
        {
            ClipboardWebGL_EnableKeyboard(canvasId, callbackObjectName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to enable keyboard support: {e.Message}");
        }
    }

    public static void DisableKeyboardSupport()
    {
        try
        {
            ClipboardWebGL_DisableKeyboard();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebGLClipboardHelper: Failed to disable keyboard support: {e.Message}");
        }
    }
#else
    // Non-WebGL platforms - stub implementations
    public static void CopyToClipboard(string text)
    {
        GUIUtility.systemCopyBuffer = text;
    }

    public static void ReadClipboard(string callbackObjectName)
    {
        // Not implemented for non-WebGL
    }

    public static void EnablePasteSupport(string canvasId, string callbackObjectName)
    {
        // Not needed for non-WebGL
    }

    public static void DisablePasteSupport(string canvasId)
    {
        // Not needed for non-WebGL
    }

    public static void EnableKeyboardSupport(string canvasId, string callbackObjectName)
    {
        // Not needed for non-WebGL
    }

    public static void DisableKeyboardSupport()
    {
        // Not needed for non-WebGL
    }
#endif
}

