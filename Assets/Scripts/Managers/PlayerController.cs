public abstract class PlayerController
{
    public int Index;

    public string Name;

    public abstract Play ChoosePlay(GameState state);
}
