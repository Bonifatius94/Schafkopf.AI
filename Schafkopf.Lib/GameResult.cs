namespace Schafkopf.Lib;

public class GameResult
{
    public GameResult(
        GameHistory history,
        int playerId,
        GameScoreEvaluation eval)
    {
        this.playerId = playerId;
        History = history;
        Reward = computeReward(history, playerId, eval);
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

    private static double computeReward(
        GameHistory history, int playerId, GameScoreEvaluation eval)
    {
        double baseCharge = baseChargeOfGame[history.Call.Mode]
            + (eval.Laufende >= 3 ? eval.Laufende * chargePerLaufendem : 0)
            + (eval.IsSchneider ? additionalChargeSchneider : 0)
            + (eval.IsSchwarz ? additionalChargeSchwarz : 0);
        double gameCost = baseCharge * (1 << history.Multipliers);

        bool isPlayer = history.CallerIds.Contains(playerId);
        int numMates = isPlayer
            ? history.CallerIds.Count()
            : history.OpponentIds.Count();

        bool isPlayerWinner =
            (isPlayer && eval.DidCallerWin) ||
            (!isPlayer && !eval.DidCallerWin);
        return (isPlayerWinner ? 1.0 : -1.0) * gameCost / numMates;
    }

    #endregion RewardComputation
}

public class GameScoreEvaluation
{
    public GameScoreEvaluation(GameHistory history)
    {
        scoreByPlayer = history.Turns
            .GroupBy(t => t.WinnerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Augen).Sum());

        ScoreCaller = history.CallerIds
            .Select(id => scoreByPlayer[id]).Sum();
        ScoreOpponents = 120 - ScoreCaller;

        DidCallerWin = (ScoreCaller >= 61 && !history.Call.IsTout)
            || (history.Call.IsTout && ScoreCaller == 120);
        IsSchneider = ScoreCaller > 90 || ScoreOpponents >= 90;
        IsSchwarz = ScoreCaller == 0 || ScoreCaller == 120;
        Laufende = laufende(history);
    }

    private Dictionary<int, int> scoreByPlayer;

    public int ScoreCaller { get; private set; }
    public int ScoreOpponents { get; private set; }

    public bool DidCallerWin { get; private set; }
    public bool IsSchneider { get; private set; }
    public bool IsSchwarz { get; private set; }
    public int Laufende { get; private set; }

    private static int laufende(GameHistory history)
    {
        // TODO: optimize this, seems very inefficient
        var comp = new CardComparer(history.Call.Mode);
        var allCardsOrdered = Enumerable.Range(0, 4)
            .SelectMany(id => history.InitialHands[id])
            .Where(c => c.IsTrumpf)
            .OrderByDescending(x => x, comp)
            .ToList();
        var callerCardsOrdered = history.CallerIds
            .SelectMany(id => history.InitialHands[id])
            .OrderByDescending(x => x, comp)
            .ToList();

        return Enumerable.Range(0, Math.Min(
                allCardsOrdered.Count, callerCardsOrdered.Count))
            .TakeWhile(i => allCardsOrdered[i] != callerCardsOrdered[i])
            .Last();
    }
}
