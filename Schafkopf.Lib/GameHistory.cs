using System.Collections;

namespace Schafkopf.Lib;

public class GameHistory : IEnumerable<Turn>
{
    public GameHistory(
        GameCall call,
        Hand[] initialHands, // ordered by player id
        int kommtRaus)
    {
        Call = call;
        this.initialHands = initialHands;
        turns = new Turn[8];
        turns[0] = Turn.InitFirstTurn((byte)kommtRaus, call);
        turnCount = 1;
        this.KommtRaus = kommtRaus;
    }

    public GameCall Call { get; private set; }
    public int KommtRaus { get; private set; }

    #region Turns

    private int turnCount;
    private Turn[] turns;
    private Hand[] initialHands;

    public Turn CurrentTurn => turns[turnCount - 1];
    public IReadOnlyList<Turn> Turns => turns[0..turnCount];

    public IEnumerator<Turn> GetEnumerator()
    {
        yield return turns[0];
        for (int i = 0; i < 7; i++)
            yield return nextTurn();
    }

    private Turn nextTurn()
    {
        var lastTurn = CurrentTurn;
        KommtRaus = CurrentTurn.WinnerId;
        var nextTurn = Turn.InitNextTurn(lastTurn);
        turns[turnCount++] = nextTurn;
        return nextTurn;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Turn NextCard(Card card)
    {
        var turnWithCardApplied = CurrentTurn.NextCard(card);
        turns[turnCount - 1] = turnWithCardApplied;
        return turnWithCardApplied;
    }

    #endregion Turns

    #region Klopfer/Kontra/Re

    public bool CanKontraRe
        => turnCount == 1 && CurrentTurn.CardsCount <= 1;

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

    #region Score

    // TODO: implement this with less re-computation effort (e.g. use memoization)

    private Dictionary<int, int> scoreByPlayer
        => turns
            .GroupBy(t => t.WinnerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Augen).Sum());

    public int ScoreCaller => CallerIds.Select(id => scoreByPlayer[id]).Sum();
    public int ScoreOpponents => 120 - ScoreCaller;

    #endregion Score

    public bool DidCallerWin => ScoreCaller >= 61;
    public bool IsSchneider => ScoreCaller > 90 || ScoreOpponents >= 90;
    public bool IsSchwarz => ScoreCaller == 0 || ScoreCaller == 120;

    public int Laufende
    {
        get
        {
            var comp = new CardComparer(Call.Mode);
            var allCardsOrdered = Enumerable.Range(0, 4)
                .SelectMany(id => initialHands[id])
                .Where(c => c.IsTrumpf)
                .OrderByDescending(x => x, comp)
                .ToList();
            var callerCardsOrdered = callerIds()
                .SelectMany(id => initialHands[id])
                .OrderByDescending(x => x, comp)
                .ToList();

            return Enumerable.Range(0, Math.Min(
                    allCardsOrdered.Count, callerCardsOrdered.Count))
                .TakeWhile(i => allCardsOrdered[i] != callerCardsOrdered[i])
                .Last();
        }
    }

    public GameResult ToPlayerResult(int playerId)
        => new GameResult(this, playerId);
}
