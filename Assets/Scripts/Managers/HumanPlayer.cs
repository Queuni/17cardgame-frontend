using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HumanPlayer : PlayerController
{
    public List<Card> Selected = new();

    public override Play ChoosePlay(GameState state)
    {
        List<Card> chosen = null;
        if (Selected != null && Selected.Count > 0)
        {
            chosen = new List<Card>(Selected);
        }
        
        var play = Rules.BuildPlay(chosen);
        if (play == null)
        {
            Debug.Log("Invalid combination - not a playable hand");
            return null;
        }

        if (!Rules.CanMatchAndBeat(play, state.CurrentTopPlay))
        {
            return null;
        }

        return play;
    }

    public Play SuggestPlay(GameState state)
    {
        var hand = state.Hands[Index].ToList();
        // Use Hard difficulty for better suggestions
        return Rules.SelectPlayFromHand(hand, state, CPUDifficulty.Hard);
    }
}
