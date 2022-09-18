namespace Schafkopf.Training;

public class RandomAgent : ISchafkopfAIAgent
{
    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => possibleCalls[rng.Next(possibleCalls.Length)];

    public Card ChooseCard(GameHistory history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
