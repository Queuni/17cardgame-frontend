using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundFinished : MonoBehaviour
{
    private GameObject winnerTextObj;
    private GameObject betTextObj;
    private GameObject againButtonObj;
    private GameObject backMenuButtonObj;

    private TextMeshProUGUI betText;
    private Image tokenImage;
    private TextMeshProUGUI winnerText;
    private Button againButton;
    private Button backMenuButton;

    private PrepareManager prepareManager;


    // Start is called before the first frame update
    void Start()
    {
        prepareManager = PrepareManager.Instance;

        // Set winner name and bet token
        SetWinnerInfo();
        AddEventListener();
    }

    private void SetWinnerInfo()
    {
        WinnerInfo winnerInfo = prepareManager.winnerInfo;

        winnerTextObj = GameObject.Find("WinnerText");
        if (winnerTextObj != null)
        {
            winnerText = winnerTextObj.GetComponent<TextMeshProUGUI>();
            if (winnerText != null)
            {
                winnerText.text = winnerInfo.winnerName + " Wins!";
            }
            else
            {
                Debug.Log("WinnerText component not found");
            }
        }
        else
        {
            Debug.Log("WinnerText object not found");
        }

        betTextObj = GameObject.Find("BetText");
        if (betTextObj != null)
        {
            betText = betTextObj.GetComponent<TextMeshProUGUI>();
            if (betText != null)
            {
                betText.text = prepareManager.winnerInfo.wonToken.ToString();
            }
            else
            {
                Debug.Log("BetText component not found");
            }
        }
        else
        {
            Debug.Log("BetText object not found");
        }

        tokenImage = GameObject.Find("BetTokenImage").GetComponent<Image>();
        if (tokenImage != null)
        {
            if (prepareManager.gameMode == GameMode.Online)
            {
                tokenImage.sprite = Resources.Load<Sprite>("images/coin__150");
            }
            else if (prepareManager.gameMode == GameMode.Local)
            {
                tokenImage.sprite = Resources.Load<Sprite>("images/coin_150_silver");
            }
        }
        else
        {
            Debug.Log("TokenImage component not found");
        }
    }

    private void AddEventListener()
    {
        againButtonObj = GameObject.Find("AgainButton");
        if (againButtonObj != null )
        {
            againButton = againButtonObj.GetComponent<Button>();
            againButton.onClick.AddListener(OnAgainClicked);
        }
        else
        {
            Debug.Log("AgainButton object not found");
        }

        backMenuButtonObj = GameObject.Find("BackButton");
        if ( backMenuButtonObj != null )
        {
            backMenuButton = backMenuButtonObj.GetComponent<Button>();
            backMenuButton.onClick.AddListener(OnBackMenuClicked);
        }
        else
        {
            Debug.Log("BackButton object not found");
        }
    }

    private void OnAgainClicked()
    {
        Utils.CloseDialogScene("RoundFinished");
        // Start the coroutine on GameManager so it doesn't stop when this scene closes
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartCoroutine(gameManager.RestartRound());
        }
        else
        {
            Debug.Log("GameManager Object Not Found");
        }
    }

    private void OnBackMenuClicked()
    {
        Utils.CloseDialogScene("RoundFinished");
        Utils.LoadScene("MainMenu");
    }
}
