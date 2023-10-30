namespace Schafkopf.Lib;

public struct GameSessionMeta
{
    public GameSessionMeta() { }

    public void NextGame(GameCall call, int klopfer)
    {
        Call = call;
        IsKontraCalled = false;
        IsReCalled = false;
        Klopfer = (byte)klopfer;
    }

    public GameCall Call = GameCall.Weiter();
    public bool IsKontraCalled = false;
    public bool IsReCalled = false;
    public byte Klopfer = 0;

    public void Kontra() => IsKontraCalled = true;
    public void Re() => IsReCalled = true;

    public int Multipliers => Klopfer + (IsKontraCalled ? 1 : 0) + (IsReCalled ? 1 : 0);

    public IEnumerable<int> CallerIds => callerIds();
    public IEnumerable<int> OpponentIds => opponentIds();

    private IEnumerable<int> callerIds()
    {
        yield return Call.CallingPlayerId;
        if (Call.Mode == GameMode.Sauspiel)
            yield return Call.PartnerPlayerId;
    }

    private IEnumerable<int> opponentIds()
    {
        for (int id = 0; id < 4; id++)
            if (id != Call.CallingPlayerId &&
                    (Call.Mode != GameMode.Sauspiel || id != Call.PartnerPlayerId))
                yield return id;
    }
}

public struct GameLog
{
    private static readonly Turn[] EMPTY_HISTORY = new Turn[8];

    public static GameLog NewLiveGame(
        GameCall call, ReadOnlySpan<Hand> initialHands, int firstPlayerId, int klopfer = 0)
    {
        var meta = new GameSessionMeta();
        meta.NextGame(call, klopfer);
        var log = new GameLog(call, initialHands, EMPTY_HISTORY, meta);
        log.Turns[0] = Turn.InitFirstTurn(firstPlayerId, call);
        return log;
    }

    public static GameLog FromCompletedGame(
            GameCall call, ReadOnlySpan<Hand> initialHands,
            ReadOnlySpan<Turn> history, GameSessionMeta? meta = null)
        => new GameLog(call, initialHands, history, meta ?? new GameSessionMeta() { Call = call });

    private GameLog(
        GameCall call,
        ReadOnlySpan<Hand> initialHands,
        ReadOnlySpan<Turn> history,
        GameSessionMeta meta)
    {
        Call = call;
        Meta = meta;
        InitialHands = new Hand[4];
        Turns = new Turn[8];
        for (int i = 0; i < 4; i++)
            InitialHands[i] = initialHands[i];
        for (int i = 0; i < 8; i++)
            Turns[i] = history[i];
    }

    public GameCall Call;
    public Turn[] Turns;
    public Hand[] InitialHands;
    public GameSessionMeta Meta;

    public int TurnCount => (int)Math.Ceiling((double)CardCount / 4);

    public int CardCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                int c = Turns[i].CardsCount;
                if (c > 0)
                    count += c;
                else
                    break;
            }
            return count;
        }
    }

    public Turn NextCard(Card card)
    {
        int t_id = CardCount / 4;
        Turns[t_id] = Turns[t_id].NextCard(card);
        return Turns[t_id];
    }

    public IEnumerable<GameAction> UnrollActions()
    {
        var turnCache = new Card[4];

        foreach (var turn in Turns)
        {
            int p_id = turn.FirstDrawingPlayerId;
            turn.CopyCards(turnCache);

            for (int i = 0; i < 4; i++)
            {
                var card = turnCache[p_id];
                var action = new GameAction() {
                    PlayerId = (byte)p_id,
                    CardPlayed = card
                };
                yield return action;
                p_id = (p_id + 1) % 4;
            }
        }
    }

    public IEnumerable<Hand> UnrollHands()
    {
        var hands = InitialHands.ToArray();
        foreach (var action in UnrollActions())
        {
            yield return hands[action.PlayerId];
            hands[action.PlayerId] = hands[action.PlayerId].Discard(action.CardPlayed);
        }
        yield return hands[0];
    }

    public IEnumerable<int[]> UnrollAugen()
    {
        var augen = new int[4];
        foreach (var turn in Turns)
        {
            yield return augen;
            augen[turn.WinnerId] += turn.Augen;
        }
        yield return augen;
    }
}

public struct GameAction
{
    public Card CardPlayed { get; set; }
    public byte PlayerId { get; set; }

    public override string ToString()
        => $"player {PlayerId} played {CardPlayed}";
}
