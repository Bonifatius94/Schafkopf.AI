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

    public int Multipliers => Klopfer + (IsKontraCalled ? 1 : 0)
        + (IsReCalled ? 1 : 0) + (Call.IsTout ? 1 : 0);
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
        Hands = InitialHands.Select(
            h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
    }

    public GameCall Call;
    public Turn[] Turns;
    public Hand[] InitialHands;
    public Hand[] Hands;
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

    public ReadOnlySpan<int> CallerIds => Call.CallerIds;
    public ReadOnlySpan<int> OpponentIds => Call.OpponentIds;
    public bool IsKontraCalled => Meta.IsKontraCalled;
    public int Multipliers => Meta.Multipliers;

    public void Kontra() => Meta.Kontra();
    public void Re() => Meta.Re();

    public Turn CurrentTurn => Turns[CardCount / 4];
    public int DrawingPlayerId => CurrentTurn.DrawingPlayerId;
    public Hand HandOfDrawingPlayer => Hands[DrawingPlayerId];

    public Turn NextCard(Card card)
    {
        int p_id = DrawingPlayerId;
        Hands[p_id] = Hands[p_id].Discard(card);

        int t_id = CardCount / 4;
        Turns[t_id] = Turns[t_id].NextCard(card);
        if (CardCount % 4 == 0 && t_id > 0 && t_id < 7)
            return Turns[t_id+1] = Turn.InitNextTurn(Turns[t_id]);
        else
            return Turns[t_id];
    }
}

public struct GameAction : IEquatable<GameAction>
{
    public Card CardPlayed { get; set; }
    public byte PlayerId { get; set; }

    public bool Equals(GameAction other)
        => CardPlayed == other.CardPlayed && PlayerId == other.PlayerId;

    public override int GetHashCode()
        => CardPlayed.Id << 8 | PlayerId;

    public override string ToString()
        => $"player {PlayerId} played {CardPlayed}";
}
