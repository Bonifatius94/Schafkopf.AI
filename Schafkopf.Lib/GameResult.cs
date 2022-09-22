namespace Schafkopf.Lib;

public class GameResult
{
    public GameResult(
        GameLog log,
        int playerId,
        GameScoreEvaluation eval)
    {
        this.playerId = playerId;
        Log = log;
        Reward = computeReward(log, playerId, eval);
    }

    private int playerId;
    public GameLog Log { get; private set; }
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
        GameLog log, int playerId, GameScoreEvaluation eval)
    {
        double baseCharge = baseChargeOfGame[log.Call.Mode]
            + (eval.Laufende >= 3 ? eval.Laufende * chargePerLaufendem : 0)
            + (eval.IsSchneider ? additionalChargeSchneider : 0)
            + (eval.IsSchwarz ? additionalChargeSchwarz : 0);
        double gameCost = baseCharge * (1 << log.Multipliers);

        bool isPlayer = log.CallerIds.Contains(playerId);
        int numMates = isPlayer
            ? log.CallerIds.Count()
            : log.OpponentIds.Count();

        bool isPlayerWinner =
            (isPlayer && eval.DidCallerWin) ||
            (!isPlayer && !eval.DidCallerWin);
        return (isPlayerWinner ? 1.0 : -1.0) * gameCost / numMates;
    }

    #endregion RewardComputation
}

public class GameScoreEvaluation
{
    public GameScoreEvaluation(GameLog log)
    {
        ScoreCaller = log.CallerIds
            .Select(id => log.Scores[id]).Sum();
        ScoreOpponents = 120 - ScoreCaller;

        DidCallerWin = (ScoreCaller >= 61 && !log.Call.IsTout)
            || (log.Call.IsTout && ScoreCaller == 120);
        IsSchneider = ScoreCaller > 90 || ScoreOpponents >= 90;
        IsSchwarz = ScoreCaller == 0 || ScoreCaller == 120;
        Laufende = laufende(log);
    }

    public int ScoreCaller { get; private set; }
    public int ScoreOpponents { get; private set; }

    public bool DidCallerWin { get; private set; }
    public bool IsSchneider { get; private set; }
    public bool IsSchwarz { get; private set; }
    public int Laufende { get; private set; }

    private static IEnumerable<Card> allTrumpfDesc(GameMode mode, CardColor trumpf)
        => (mode == GameMode.Wenz)
            ? new List<Card>() {
                new Card(CardType.Unter, CardColor.Eichel, true, true),
                new Card(CardType.Unter, CardColor.Gras, true, true),
                new Card(CardType.Unter, CardColor.Herz, true, true),
                new Card(CardType.Unter, CardColor.Schell, true, true),
            }
            : new List<Card>() {
                new Card(CardType.Ober, CardColor.Eichel, true, true),
                new Card(CardType.Ober, CardColor.Gras, true, true),
                new Card(CardType.Ober, CardColor.Herz, true, true),
                new Card(CardType.Ober, CardColor.Schell, true, true),
                new Card(CardType.Unter, CardColor.Eichel, true, true),
                new Card(CardType.Unter, CardColor.Gras, true, true),
                new Card(CardType.Unter, CardColor.Herz, true, true),
                new Card(CardType.Unter, CardColor.Schell, true, true),
                new Card(CardType.Sau, trumpf, true, true),
                new Card(CardType.Zehn, trumpf, true, true),
                new Card(CardType.Koenig, trumpf, true, true),
                new Card(CardType.Neun, trumpf, true, true),
                new Card(CardType.Acht, trumpf, true, true),
                new Card(CardType.Sieben, trumpf, true, true),
            };

    private static readonly (GameMode, CardColor)[] calls =
        new (GameMode, CardColor)[] {
            (GameMode.Sauspiel, CardColor.Herz),
            (GameMode.Wenz, CardColor.Schell),
            (GameMode.Solo, CardColor.Schell),
            (GameMode.Solo, CardColor.Herz),
            (GameMode.Solo, CardColor.Gras),
            (GameMode.Solo, CardColor.Eichel),
        };

    private static readonly Dictionary<(GameMode, CardColor), Card[]> trumpfByMode =
        calls.ToDictionary(x => x, x => allTrumpfDesc(x.Item1, x.Item2).ToArray());

    private static int laufende(GameLog log)
    {
        var comp = new CardComparer(log.Call.Mode, log.Call.Trumpf);
        var allTrumpfDesc = trumpfByMode[(log.Call.Mode, log.Call.Trumpf)];

        bool callersHaveHighest = log.CallerIds
            .Any(id => log.InitialHands[id].HasCard(allTrumpfDesc[0]));
        var playerIds = callersHaveHighest ? log.CallerIds : log.OpponentIds;

        int laufende = 1;
        foreach (var laufender in allTrumpfDesc.Skip(1))
        {
            bool haveLaufenden = playerIds.Any(id =>
                log.InitialHands[id].HasCard(laufender));

            if (haveLaufenden)
                laufende++;
            else
                break;
        }

        return laufende;
    }
}
