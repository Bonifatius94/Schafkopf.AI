namespace Schafkopf.Training;

public class RandomAgent : ISchafkopfAIAgent
{
    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            IEnumerable<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => possibleCalls.ElementAt(rng.Next(possibleCalls.Count()));

    public Card ChooseCard(GameHistory history, IEnumerable<Card> possibleCards)
        => possibleCards.ElementAt(rng.Next(possibleCards.Count()));

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
