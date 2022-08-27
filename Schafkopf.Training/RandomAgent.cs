namespace Schafkopf.Lib;

public class RandomAgent : ISchafkopfPlayer
{
    public Hand Hand { get; private set; }

    public void NewGame(Hand hand)
        => Hand = hand;

    private static readonly Random rng = new Random();

    public Card ChooseCard(Turn state)
        => Hand.Cards.ElementAt(rng.Next(0, Hand.Cards.Count));

    public void OnGameFinished(GameHistory game) { }

    public void OnInvalidCardPicked(Card card) { }
}
