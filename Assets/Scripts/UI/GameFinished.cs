using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameFinished : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform content;
    public GameObject rowPrefab;

    private PrepareManager prepareManager;

    private void Start()
    {
        prepareManager = PrepareManager.Instance;

        List<GameScoreInfo> scoreInfoList = prepareManager.scoreInfoList;
        scoreInfoList = scoreInfoList.OrderByDescending(info => info.wins).ToList();
        for (int i = 0; i < scoreInfoList.Count; i++)
        {
            GameScoreInfo info = scoreInfoList[i];

            var row = Instantiate(rowPrefab, content);
            row.transform.Find("NoText").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
            row.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = info.name;
            row.transform.Find("WinText").GetComponent<TextMeshProUGUI>().text = info.wins.ToString();
        }


        if (prepareManager.gameMode == GameMode.Local && scoreInfoList.First().name == "You")
        {
            AuthManager.Instance.GetRewardToken();

            AlertBar.Instance.ShowMessage("Victory reward: +10 tokens!");
        }
    }

    public void OnFinishButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }
}
