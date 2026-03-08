using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class YesNoDialog : MonoBehaviour
{
    public GameObject dialogPanel;
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    public static YesNoDialog Instance;

    private void Awake()
    {
        // Singleton logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Start()
    {
        dialogPanel.SetActive(false);
    }

    public void Show(string message, Action onYes, Action onNo)
    {
        dialogPanel.SetActive(true);
        messageText.text = message;

        // Clear old listeners first
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() => {
            dialogPanel.SetActive(false);
            onYes?.Invoke();
        });

        noButton.onClick.AddListener(() => {
            dialogPanel.SetActive(false);
            onNo?.Invoke();
        });
    }
}
