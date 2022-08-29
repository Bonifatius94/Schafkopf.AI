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

    #region Score

    private Dictionary<int, int> scoreByPlayer
        => turns.GroupBy(t => eval.WinnerId(t))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Augen).Sum());

    public IEnumerable<int> CallerIds
        => Call.Mode == GameMode.Sauspiel
            ? new List<int>() { Call.CallingPlayerId, Call.PartnerPlayerId }
            : new List<int>() { Call.CallingPlayerId };

    public IEnumerable<int> OpponentIds
        => Enumerable.Range(0, 4).Except(CallerIds).ToList();

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
    private const double chargeSchneider = 10;
    private const double chargeSchwarz = 20;

    private static double computeReward(GameHistory history, int playerId)
    {
        int klopfer = 0; // TODO: include a game mechanism for player interaction
        int laufende = 0; // TODO: find a way to compute this

        double baseCharge = baseChargeOfGame[history.Call.Mode]
            + (laufende * chargePerLaufendem)
            + (history.IsSchneider ? chargeSchneider : 0)
            + (history.IsSchwarz ? chargeSchwarz : 0);
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