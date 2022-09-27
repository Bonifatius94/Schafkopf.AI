namespace Schafkopf.Lib;

public class GameLog : IEnumerable<Turn>
{
    public GameLog(
        GameCall call,
        Hand[] initialHands, // ordered by player id
        int kommtRaus)
    {
        Call = call;
        this.initialHands = initialHands;
        turns = new Turn[8];
        turns[0] = Turn.InitFirstTurn((byte)kommtRaus, call);
        TurnCount = 1;
        this.KommtRaus = kommtRaus;
        scores = new int[4];
    }

    public GameCall Call { get; private set; }
    public int KommtRaus { get; private set; }

    #region Turns

    public int TurnCount { get; private set; }
    private Turn[] turns;
    private Hand[] initialHands;

    public int CardsPlayed => (TurnCount - 1) * 4 + CurrentTurn.CardsCount;
    public Turn CurrentTurn => turns[TurnCount - 1];
    public IReadOnlyList<Turn> Turns => turns[0..TurnCount];
    public IReadOnlyList<Hand> InitialHands => initialHands;

    public IEnumerator<Turn> GetEnumerator()
    {
        // replay already enumerated turns
        for (int i = 0; i < TurnCount; i++)
            yield return turns[i];

        // yield new turns for player interaction
        for (int i = TurnCount; i < 8; i++)
            yield return nextTurn();

        // cache the final augen scores for eval
        updateScore(CurrentTurn.WinnerId, CurrentTurn.Augen);
    }

    private Turn nextTurn()
    {
        var lastTurn = CurrentTurn;
        var winnerId = lastTurn.WinnerId;
        updateScore(winnerId, lastTurn.Augen);
        KommtRaus = winnerId;
        var nextTurn = Turn.InitNextTurn(lastTurn);
        turns[TurnCount++] = nextTurn;
        return nextTurn;
    }

    private void updateScore(int winnerId, int augen)
        => scores[winnerId] += augen;

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Turn NextCard(Card card)
    {
        var turnWithCardApplied = CurrentTurn.NextCard(card);
        turns[TurnCount - 1] = turnWithCardApplied;
        return turnWithCardApplied;
    }

    #endregion Turns

    #region Scores

    private int[] scores;
    public ReadOnlySpan<int> Scores => scores;

    #endregion Scores

    #region Klopfer/Kontra/Re

    public bool CanKontraRe
        => TurnCount == 1 && CurrentTurn.CardsCount <= 1;

    private int klopfer = 0;
    public bool IsKontraCalled { get; private set; } = false;
    public bool IsReCalled { get; private set; } = false;

    public void CallKontra()
        => IsKontraCalled = true;

    public void CallRe()
        => IsReCalled = true;

    public int Multipliers => klopfer +
        (IsKontraCalled ? 1 : 0) + (IsReCalled ? 1 : 0);

    #endregion Klopfer/Kontra/Re

    #region Caller/Opponent

    // TODO: move caller / opponent ids into the game call

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

    #endregion Caller/Opponent

    public GameResult ToPlayerResult(int playerId)
        => new GameResult(this, playerId, new GameScoreEvaluation(this));
}
