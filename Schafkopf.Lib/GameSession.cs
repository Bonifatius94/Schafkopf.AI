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

    public int ScoreCaller => CallerIds.Select(x => scoreByPlayer[x]).Sum();
    public int ScoreOpponents => OpponentIds.Select(x => scoreByPlayer[x]).Sum();

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
    private const double chargeSchwarz = 10;

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
        bool isOpponent = history.OpponentIds.Contains(playerId);
        int numMates = isPlayer
            ? history.CallerIds.Count()
            : history.OpponentIds.Count();

        bool isPlayerWinner =
            (isPlayer && history.DidCallerWin) ||
            (isOpponent && !history.DidCallerWin);
        return (isPlayerWinner ? 1.0 : -1.0) * gameCost / numMates;
    }

    #endregion RewardComputation
}

public class GameSession
{
    public GameSession(
        ISchafkopfPlayer p1,
        ISchafkopfPlayer p2,
        ISchafkopfPlayer p3,
        ISchafkopfPlayer p4)
    {
        players = new ISchafkopfPlayer[] { p1, p2, p3, p4 };
        deck = new CardsDeck();
        firstDrawingPlayerId = 0;
    }

    private ISchafkopfPlayer[] players;
    private CardsDeck deck;
    private int firstDrawingPlayerId;

    public GameHistory PlayGameUntilEnd(GameCall call)
    {
        foreach (int i in Enumerable.Range(0, 4))
            players[i].NewGame(deck.HandOfPlayer(i));

        var history = new GameHistory(call);
        var eval = new TurnEvaluator(call);
        var validator = new DrawValidatorFactory().Create(call.Mode);
        int beginningPlayerOfTurn = firstDrawingPlayerId;

        foreach (int round in Enumerable.Range(0, 8))
        {
            Console.WriteLine($"starting round {round}");
            var turn = Turn.NewTurn((byte)beginningPlayerOfTurn);

            foreach (var player in playersInDrawingOrder(beginningPlayerOfTurn))
            {
                Card card;

                // TODO: this seems to loop infinitely
                while (true)
                {
                    card = player.ChooseCard(turn);
                    bool valid = !validator.IsValid(call, card, turn, player.Hand);
                    if (valid) break;
                    player.OnInvalidCardPicked(card);
                }

                player.Hand.Discard(card);
                turn = turn.NextCard(card);
            }

            history.Append(turn);
            beginningPlayerOfTurn = eval.WinnerId(turn);
        }

        firstDrawingPlayerId = (byte)nextPlayerId(firstDrawingPlayerId);
        return history;
    }

    private IEnumerable<ISchafkopfPlayer> playersInDrawingOrder(int beginningPlayer)
    {
        yield return players[beginningPlayer];
        yield return players[(++beginningPlayer % 4)];
        yield return players[(++beginningPlayer % 4)];
        yield return players[(++beginningPlayer % 4)];
    }

    private int nextPlayerId(int playerId)
        => ++playerId % 4;
}
