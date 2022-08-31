namespace Schafkopf.Lib;

public class GameHistory
{
    public GameHistory(GameCall call)
    {
        Call = call;
        eval = new TurnEvaluator(call);
    }

    public GameCall Call { get; private set; }
    private readonly TurnEvaluator eval;

    #region Turns

    private List<Turn> turns = new List<Turn>();
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
            .GroupBy(t => eval.WinnerId(t))
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Augen).Sum());

    public int ScoreCaller => CallerIds.Select(id => scoreByPlayer[id]).Sum();
    public int ScoreOpponents => OpponentIds.Select(id => scoreByPlayer[id]).Sum();

    #endregion Score

    public bool DidCallerWin => ScoreCaller > ScoreOpponents;
    public bool IsSchneider => ScoreCaller > 90 || ScoreOpponents >= 90;
    public bool IsSchwarz => Math.Abs(ScoreCaller - ScoreOpponents) == 120;

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
        int klopfer = 0; // TODO: include a game mechanism for player interaction
        int laufende = 0; // TODO: find a way to compute this

        double baseCharge = baseChargeOfGame[history.Call.Mode]
            + (laufende * chargePerLaufendem)
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