using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RulesScene : MonoBehaviour
{
    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBackButtonPressed()
    {
        Utils.LoadScene("MainMenu");
    }
}
