namespace Schafkopf.Training;

public class RandomAgent : ISchafkopfAIAgent
{
    public RandomAgent(HeuristicGameCaller caller)
        => this.caller = caller;

    private HeuristicGameCaller caller;
    private static readonly Random rng = new Random();

    public void OnGameFinished(GameLog final) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => caller.MakeCall(possibleCalls, position, hand, klopfer);

    public Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameLog history)
        => false;

    public bool CallRe(GameLog history)
        => false;
}
