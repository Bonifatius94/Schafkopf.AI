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

        // this is just to make the linter happy, obviously makes no sense
        normLog = new GameLog(GameCall.Weiter(), new Hand[4], 0);
    }

    private ISchafkopfAIAgent agent;
    public int Id { get; private set; }

    #region Bookkeeping

    private GameLog normLog;
    public Hand Hand { get; private set; }

    public void Discard(Card card)
        => Hand = Hand.Discard(card);

    public void NewGame(GameLog newLog)
    {
        Hand = newLog.InitialHands[Id];
        var normCall = normalizeCall(newLog.Call);
        int normKommtRaus = normalizePlayerId(newLog.Turns[0].FirstDrawingPlayerId, Id);
        var normHands = normalizeHands(newLog.InitialHands);
        normLog = new GameLog(normCall, normHands, normKommtRaus);
    }

    private Card[] turnCache = new Card[4];
    private GameLog normalizeLog(GameLog log)
    {
        int cardsPlayed = log.CardsPlayed;
        int cardId = normLog.CardsPlayed;

        if (cardsPlayed == cardId)
            return normLog;

        var normTurns = normLog.Skip(normLog.TurnCount - 1);
        foreach (var normTurn in normTurns)
        {
            int turnId = cardId / 4;
            var turn = log.Turns[turnId];
            turn.CopyCards(turnCache);
            int kommtRaus = turn.FirstDrawingPlayerId;

            for (int i = normTurn.CardsCount; i < turn.CardsCount; i++)
            {
                int posOfTurn = (cardId + kommtRaus) % 4;
                var card = turnCache[posOfTurn];
                normLog.NextCard(card);
                cardId++;
            }

            if (cardId == cardsPlayed)
                break;
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
        cache[1] = hands[(Id + 1) & 0x03];
        cache[2] = hands[(Id + 2) & 0x03];
        cache[3] = hands[(Id + 3) & 0x03];
        return cache;
    }

    private static int normalizePlayerId(int id, int offset)
        => (((id - offset) & 0x03) + 4) & 0x03;
        // TODO: optimize this because "mod 4" is the same as "& 0x03"

    #endregion Bookkeeping

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
