using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderBoardScene : MonoBehaviour
{
    private RequestManager requestManager;
    private AuthManager authManager;


    public GameObject rowPrefab;
    public Transform content;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update
    void Start()
    {
        requestManager = RequestManager.Instance;
        authManager = AuthManager.Instance;

        GetLeaderBoardData();
    }

    private async void GetLeaderBoardData()
    {
        Spinner.Instance.Show();

        try
        {
            string url = "game/leaderboard";
            string token = await authManager.GetIdToken();
            string resp = await requestManager.GetRequestAsync(url, token);

            Spinner.Instance.Hide();

            LeaderBoardListInfo leaderBoardInfo = Utils.JsonToObject<LeaderBoardListInfo>(resp);
            List<LeaderBoardRowInfo> leaderBoardRows = leaderBoardInfo.leaderBoardRows;

            foreach (LeaderBoardRowInfo row in leaderBoardRows)
            {
                GameObject rowObject = Instantiate(rowPrefab, content);
                rowObject.transform.Find("RankText").GetComponent<TextMeshProUGUI>().text = row.rank.ToString();
                rowObject.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = row.name.ToString();
                rowObject.transform.Find("WinText").GetComponent<TextMeshProUGUI>().text = row.wins.ToString();
                rowObject.transform.Find("TokenText").GetComponent<TextMeshProUGUI>().text = row.tokens.ToString();
            }
        }
        catch (System.Exception)
        {
            Spinner.Instance.Hide();
            AlertBar.Instance.ShowMessage("Failed loading leaderboard data.");
        }
    }
    
    public void OnBackButtonClicked()
    {
        Utils.LoadScene("Options");
    }
}
