namespace Schafkopf.Lib;

public class GameHistory
{
    public GameHistory(GameCall call, IReadOnlyList<(int, Hand)> initialHands)
    {
        Call = call;
        this.initialHands = initialHands
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    public GameCall Call { get; private set; }

    #region Turns

    private List<Turn> turns = new List<Turn>();
    private IReadOnlyDictionary<int, Hand> initialHands;
    public IReadOnlyList<Turn> Turns => turns;

    public void Append(Turn turn)
        => turns.Add(turn);

    #endregion Turns

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
            .GroupBy(t => t.WinnerId(Call))
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Augen).Sum());

    public int ScoreCaller => CallerIds.Select(id => scoreByPlayer[id]).Sum();
    public int ScoreOpponents => OpponentIds.Select(id => scoreByPlayer[id]).Sum();

    #endregion Score

    public bool DidCallerWin => ScoreCaller > ScoreOpponents;
    public bool IsSchneider => ScoreCaller > 90 || ScoreOpponents >= 90;
    public bool IsSchwarz => Math.Abs(ScoreCaller - ScoreOpponents) == 120;

    public int Laufende
    {
        get
        {
            var comp = new CardComparer(Call.Mode);
            var allCardsOrdered = Enumerable.Range(0, 4)
                .SelectMany(id => initialHands[id].Cards)
                .Where(c => c.IsTrumpf)
                .OrderByDescending(x => x, comp)
                .ToList();
            var callerCardsOrdered = callerIds()
                .SelectMany(id => initialHands[id].Cards)
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

public class GameResult
{
    public GameResult(GameHistory history, int playerId)
    {
        this.playerId = playerId;
        History = history;
        Reward = computeReward(history, playerId);
    }

    private int playerId;
    public GameHistory History { get; private set; }
    public double Reward { get; private set; }

    #region RewardComputation

    private static readonly Dictionary<GameMode, double> baseChargeOfGame =
        new Dictionary<GameMode, double>() {
            { GameMode.Sauspiel, 10 },
            { GameMode.Solo, 50 },
            { GameMode.Wenz, 50 },
        };

    private const double chargePerLaufendem = 10;
    private const double additionalChargeSchneider = 10;
    private const double additionalChargeSchwarz = 20;

    private static double computeReward(GameHistory history, int playerId)
    {
        int klopfer = 0; // TODO: include a game mechanism for collecting klopfer

        double baseCharge = baseChargeOfGame[history.Call.Mode]
            + (history.Laufende >= 3 ? history.Laufende * chargePerLaufendem : 0)
            + (history.IsSchneider ? additionalChargeSchneider : 0)
            + (history.IsSchwarz ? additionalChargeSchwarz : 0);
        double gameCost = baseCharge * (1 + klopfer);

        bool isPlayer = history.CallerIds.Contains(playerId);
        int numMates = isPlayer
            ? history.CallerIds.Count()
            : history.OpponentIds.Count();

        bool isPlayerWinner =
            (isPlayer && history.DidCallerWin) ||
            (!isPlayer && !history.DidCallerWin);
        return (isPlayerWinner ? 1.0 : -1.0) * gameCost / numMates;
    }

    #endregion RewardComputation
}