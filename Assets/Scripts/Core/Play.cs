using System.Collections.Generic;

public class Play
{
    public PlayType Type;
    public List<Card> Cards;
    public int Strength;

    public Play(PlayType type, List<Card> cards, int strength)
    {
        Type = type;
        Cards = cards;
        Strength = strength;
    }

    public override string ToString() => $"{Type} ({Cards.Count}) [{string.Join(", ", Cards)} ...]";
}
