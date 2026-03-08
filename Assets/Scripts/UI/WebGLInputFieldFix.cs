using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Component to fix copy/paste functionality for TMP_InputField in WebGL builds.
/// Attach this component to any GameObject that has TMP_InputField components you want to fix.
/// </summary>
public class WebGLInputFieldFix : MonoBehaviour
{
    private const string DEFAULT_CANVAS_ID = "unity-canvas";
    
#if UNITY_WEBGL && !UNITY_EDITOR
    public static WebGLInputFieldFix Instance { get; private set; }
    
    private TMP_InputField activeInputField;
    private static bool isInitialized = false;
    
    // Track the most recently clicked/selected input field
    private TMP_InputField mostRecentlySelectedField;
    private float mostRecentSelectionTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupInputFieldsInScene();
        InitializePasteSupportIfNeeded();
    }

    private void OnEnable()
    {
        SetupInputFieldsInScene();
    }

    private System.Collections.Generic.HashSet<TMP_InputField> registeredInputFields = new System.Collections.Generic.HashSet<TMP_InputField>();

    /// <summary>
    /// Helper method to find and set the active input field
    /// </summary>
    private void FindAndSetActiveInputField()
    {
        // First, try to find any focused input field in the scene
        TMP_InputField[] allInputFields = FindObjectsOfType<TMP_InputField>(true);
        TMP_InputField focusedField = null;
        
        foreach (var field in allInputFields)
        {
            if (field != null && field.isFocused)
            {
                focusedField = field;
                break;
            }
        }
        
        if (focusedField != null)
        {
            activeInputField = focusedField;
            mostRecentlySelectedField = focusedField;
            mostRecentSelectionTime = Time.time;
        }
        // If no focused field, use the most recently selected field
        else if (mostRecentlySelectedField != null)
        {
            activeInputField = mostRecentlySelectedField;
        }
    }

    private void SetupInputFieldsInScene()
    {
        TMP_InputField[] allInputFields = FindObjectsOfType<TMP_InputField>(true);
        
        foreach (var inputField in allInputFields)
        {
            if (inputField != null && !registeredInputFields.Contains(inputField))
            {
                registeredInputFields.Add(inputField);
                
                // Track selection
                inputField.onSelect.AddListener((text) =>
                {
                    activeInputField = inputField;
                    mostRecentlySelectedField = inputField;
                    mostRecentSelectionTime = Time.time;
                });
                
                inputField.onDeselect.AddListener((text) =>
                {
                    // Don't clear activeInputField on deselect - keep it for next paste
                    // Only clear if it's a different field being selected
                });
                
                inputField.onFocusSelectAll = true;
                
                // Add pointer click handler to track clicks
                EventTrigger trigger = inputField.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = inputField.gameObject.AddComponent<EventTrigger>();
                }
                
                bool entryExists = false;
                foreach (var existingEntry in trigger.triggers)
                {
                    if (existingEntry.eventID == EventTriggerType.PointerClick)
                    {
                        entryExists = true;
                        break;
                    }
                }
                
                if (!entryExists)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerClick;
                    entry.callback.AddListener((data) => {
                        activeInputField = inputField;
                        mostRecentlySelectedField = inputField;
                        mostRecentSelectionTime = Time.time;
                    });
                    trigger.triggers.Add(entry);
                }
            }
        }
        
    }

    private void OnDestroy()
    {
        if (isInitialized)
        {
            WebGLClipboardHelper.DisablePasteSupport(DEFAULT_CANVAS_ID);
            WebGLClipboardHelper.DisableKeyboardSupport();
            isInitialized = false;
        }
    }

    private void InitializePasteSupportIfNeeded()
    {
        if (!isInitialized)
        {
            try
            {
                WebGLClipboardHelper.EnablePasteSupport(DEFAULT_CANVAS_ID, gameObject.name);
                WebGLClipboardHelper.EnableKeyboardSupport(DEFAULT_CANVAS_ID, gameObject.name);
                isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WebGL Clipboard Fix: Failed to initialize paste support. Error: {e.Message}");
            }
        }
    }

    private void HandleCopy()
    {
        if (activeInputField == null)
        {
            Debug.LogWarning("WebGL Clipboard: HandleCopy called but activeInputField is null");
            return;
        }

        string textToCopy = activeInputField.text;
        int selectionAnchor = activeInputField.selectionAnchorPosition;
        int selectionFocus = activeInputField.selectionFocusPosition;
        
        if (selectionAnchor != selectionFocus && selectionAnchor >= 0 && selectionFocus >= 0)
        {
            int start = Mathf.Min(selectionAnchor, selectionFocus);
            int end = Mathf.Max(selectionAnchor, selectionFocus);
            if (start < textToCopy.Length && end <= textToCopy.Length)
            {
                textToCopy = textToCopy.Substring(start, end - start);
            }
        }
        else
        {
            Debug.Log($"WebGL Clipboard: No selection, copying entire text: '{textToCopy}'");
        }

        if (!string.IsNullOrEmpty(textToCopy))
        {
            WebGLClipboardHelper.CopyToClipboard(textToCopy);
        }
        else
        {
            Debug.LogWarning("WebGL Clipboard: No text to copy");
        }
    }

    /// <summary>
    /// Called by JavaScript when Ctrl+C is pressed
    /// </summary>
    public void OnWebGLKeyCopy(string dummy)
    {
        // Try to find active input field if not tracked
        if (activeInputField == null)
        {
            FindAndSetActiveInputField();
        }
        
        if (activeInputField == null)
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: No active input field for copy");
            return;
        }
        
        if (!activeInputField.isFocused)
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: Input field not focused for copy. Forcing focus and continuing.");
            activeInputField.ActivateInputField();
        }

        HandleCopy();
    }

    /// <summary>
    /// Called by JavaScript when Ctrl+V is pressed
    /// </summary>
    public void OnWebGLKeyPaste(string dummy)
    {
        // Always check for the currently focused field first (most recent click)
        // This ensures we use the field the user just clicked, not a stale reference
        FindAndSetActiveInputField();
        
        if (activeInputField == null)
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: No active input field for paste");
            return;
        }
        
        if (!activeInputField.isFocused)
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: Input field not focused for paste. Forcing focus and continuing.");
            activeInputField.ActivateInputField();
        }

        InitializePasteSupportIfNeeded();

        try
        {
            WebGLClipboardHelper.ReadClipboard(gameObject.name);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"WebGL Clipboard Fix: Failed to read clipboard: {e.Message}");
        }
    }

    /// <summary>
    /// Called by JavaScript when paste event is detected or clipboard is read
    /// </summary>
    public void OnWebGLPaste(string pastedText)
    {
        if (activeInputField == null)
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: OnWebGLPaste - activeInputField is null");
            return;
        }
        
        if (string.IsNullOrEmpty(pastedText))
        {
            Debug.LogWarning("⚠️ WebGL Clipboard: OnWebGLPaste - pastedText is empty");
            return;
        }

        // Insert pasted text at caret position
        string currentText = activeInputField.text;
        int caretPos = activeInputField.caretPosition;
        
        int selectionAnchor = activeInputField.selectionAnchorPosition;
        int selectionFocus = activeInputField.selectionFocusPosition;
        
        if (selectionAnchor != selectionFocus && selectionAnchor >= 0 && selectionFocus >= 0)
        {
            int start = Mathf.Min(selectionAnchor, selectionFocus);
            int end = Mathf.Max(selectionAnchor, selectionFocus);
            
            if (start >= 0 && end <= currentText.Length)
            {
                currentText = currentText.Substring(0, start) + pastedText + currentText.Substring(end);
                caretPos = start + pastedText.Length;
            }
        }
        else
        {
            if (caretPos >= 0 && caretPos <= currentText.Length)
            {
                currentText = currentText.Insert(caretPos, pastedText);
                caretPos += pastedText.Length;
            }
        }

        // Update input field
        activeInputField.text = currentText;
        activeInputField.caretPosition = caretPos;
        
        // CRITICAL FIX: After successful paste, ensure this field remains the active field
        // This prevents the next paste from going to a previous field
        mostRecentlySelectedField = activeInputField;
        mostRecentSelectionTime = Time.time;
        
        // Ensure the field stays focused after paste
        if (!activeInputField.isFocused)
        {
            activeInputField.ActivateInputField();
        }
        
    }
#else
    private void Start()
    {
        enabled = false;
    }
#endif
}

