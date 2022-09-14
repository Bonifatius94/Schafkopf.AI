namespace Schafkopf.Lib;

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
        double baseCharge = baseChargeOfGame[history.Call.Mode]
            + (history.Laufende >= 3 ? history.Laufende * chargePerLaufendem : 0)
            + (history.IsSchneider ? additionalChargeSchneider : 0)
            + (history.IsSchwarz ? additionalChargeSchwarz : 0);
        double gameCost = baseCharge * (1 << history.Multipliers);

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