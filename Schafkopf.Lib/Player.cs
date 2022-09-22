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

public class Player
{
    public Player(int id, ISchafkopfAIAgent agent)
    {
        Id = id;
        this.agent = agent;
    }

    private ISchafkopfAIAgent agent;

    public int Id { get; private set; }
    public Hand Hand { get; private set; }

    public void Discard(Card card)
        => Hand = Hand.Discard(card);

    public void NewGame(Hand hand)
        => Hand = hand;

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

    #region Normalization

    private GameLog normalizeLog(GameLog log)
    {
        // TODO: this needs to be optimized by a lot
        // best would be storing the data inside the GameLog in a normalized way
        // and only yielding the data differently, such that no normalization is required

        var normCall = normalizeCall(log.Call);
        var normHands = normalizeHands(log.InitialHands);
        int normKommtRaus = normalizePlayerId(log.Turns[0].FirstDrawingPlayerId, Id);

        var normLog = new GameLog(normCall, normHands, normKommtRaus);
        var normLogIter = normLog.GetEnumerator();

        foreach (var turn in log.Turns)
        {
            normLogIter.MoveNext();

            // info: this yields the cards in the order they were played
            foreach (var card in turn.AllCards)
                normLog.NextCard(card);
        }

        return normLog;
    }

    private GameCall normalizeCall(GameCall call)
    {
        if (call.Mode == GameMode.Weiter)
            return call;

        int callingPlayer = normalizePlayerId(call.CallingPlayerId, Id);
        int partnerPlayer = normalizePlayerId(call.PartnerPlayerId, Id);

        if (call.Mode == GameMode.Sauspiel)
            return GameCall.Sauspiel(callingPlayer, partnerPlayer, call.GsuchteFarbe);
        else if (call.Mode == GameMode.Wenz)
            return GameCall.Wenz(callingPlayer, call.IsTout);
        else // if (call.Mode == GameMode.Solo)
            return GameCall.Solo(callingPlayer, call.Trumpf, call.IsTout);
    }

    private Hand[] cache = new Hand[4];
    private Hand[] normalizeHands(IReadOnlyList<Hand> hands)
    {
        cache[0] = hands[Id];
        cache[1] = hands[(Id + 1) % 4];
        cache[2] = hands[(Id + 2) % 4];
        cache[3] = hands[(Id + 3) % 4];
        return cache;
    }

    private static int normalizePlayerId(int id, int offset)
        => (((id - offset) & 0x03) + 4) & 0x03;
        // TODO: optimize this because "mod 4" is the same as "& 0x03"

    #endregion Normalization
}
