namespace Schafkopf.Lib;

public class RandomAgent : ISchafkopfPlayer
{
    private GameCall call;
    public Hand Hand { get; private set; }

    public void NewGame(GameCall call, Hand hand)
    {
        Hand = hand;
        this.call = call;
    }

    private static readonly Random rng = new Random();

    public Card ChooseCard(Turn state)
        => Hand.Cards.ElementAt(rng.Next(0, Hand.CardsCount));

    public void OnGameFinished(GameResult result) { }

    public void OnInvalidCardPicked(Card card) { }
}
