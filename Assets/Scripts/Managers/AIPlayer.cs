using System.Collections.Generic;
using System.Linq;

public class AIPlayer : PlayerController
{
    public CPUDifficulty Difficulty;
    public AIPlayer(CPUDifficulty diff) => Difficulty = diff;

    public override Play ChoosePlay(GameState state)
    {
        var hand = state.Hands[Index].ToList();
        Play chosen = Rules.SelectPlayFromHand(hand, state, Difficulty);

        // Clear FirstTrick flag when AI plays
        if (chosen != null && state.FirstTrick)
        {
            state.FirstTrick = false;
        }

        return chosen;
    }
}
