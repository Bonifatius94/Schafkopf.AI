namespace Schafkopf.Lib;

public class GameHistory
{
    public GameHistory(GameCall call)
    {
        this.call = call;
        eval = new TurnEvaluator(call);
    }

    public GameCall call;
    private readonly TurnEvaluator eval;

    private List<Turn> turns = new List<Turn>();
    public IReadOnlyList<Turn> Turns => turns;

    public void Append(Turn turn)
        => turns.Add(turn);

    #region Score

    private Dictionary<int, int> scoreByPlayer
        => turns.GroupBy(t => eval.WinnerId(t))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Augen).Sum());

    private IEnumerable<int> callerIds
        => call.Mode == GameMode.Sauspiel
            ? new List<int>() { call.CallingPlayerId, call.PartnerPlayerId }
            : new List<int>() { call.CallingPlayerId };

    private IEnumerable<int> opponentIds
        => Enumerable.Range(0, 4).Except(callerIds).ToList();

    public int ScoreCaller => callerIds.Select(x => scoreByPlayer[x]).Sum();
    public int ScoreOpponents => opponentIds.Select(x => scoreByPlayer[x]).Sum();

    // TODO: add conversion to a player-specific view (for simpler training)

    #endregion Score
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
