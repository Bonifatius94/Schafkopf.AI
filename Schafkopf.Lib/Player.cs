namespace Schafkopf.Lib;

public interface ISchafkopfAIAgent
{
    GameCall MakeCall(
        ReadOnlySpan<GameCall> possibleCalls,
        int position, Hand hand, int klopfer);
    Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards);
    void OnGameFinished(GameLog final);

    bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards);
    bool CallKontra(GameLog log);
    bool CallRe(GameLog log);
}

public class Player : ISchafkopfAIAgent
{
    public Player(int id, ISchafkopfAIAgent agent)
    {
        Id = id;
        this.agent = agent;

        // // this is just to make the linter happy, obviously makes no sense
        // normLog = new GameLog(GameCall.Weiter(), new Hand[4], 0);
    }

    private ISchafkopfAIAgent agent;
    public int Id { get; private set; }

    // TODO: implement normalization here if needed
    private GameLog normalizeLog(GameLog orig) => orig;

    public bool CallKontra(GameLog log)
        => agent.CallKontra(normalizeLog(log));

    public bool CallRe(GameLog log)
        => agent.CallRe(normalizeLog(log));

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
        => agent.ChooseCard(normalizeLog(log), possibleCards);

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => agent.IsKlopfer(position, firstFourCards);

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => agent.MakeCall(possibleCalls, position, hand, klopfer);

    public void OnGameFinished(GameLog final)
        => agent.OnGameFinished(normalizeLog(final));
}
