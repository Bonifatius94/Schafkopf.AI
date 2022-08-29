namespace Schafkopf.Lib;

public class GameTable
{
    public GameTable(
        ISchafkopfPlayer p1,
        ISchafkopfPlayer p2,
        ISchafkopfPlayer p3,
        ISchafkopfPlayer p4)
    {
        players = new ISchafkopfPlayer[] { p1, p2, p3, p4 };
        FirstDrawingPlayerId = 0;
    }

    private ISchafkopfPlayer[] players;
    public int FirstDrawingPlayerId { get; private set; }

    public IEnumerable<ISchafkopfPlayer> PlayersInDrawingOrder()
        => PlayersInDrawingOrder(FirstDrawingPlayerId);

    public IEnumerable<ISchafkopfPlayer> PlayersInDrawingOrder(
        int beginningPlayerId)
    {
        yield return players[beginningPlayerId];
        yield return players[(++beginningPlayerId % 4)];
        yield return players[(++beginningPlayerId % 4)];
        yield return players[(++beginningPlayerId % 4)];
    }

    public void SupplyHands(CardsDeck deck)
    {
        // TODO: implement Klopfen here ...
        foreach (int id in Enumerable.Range(0, 4))
            players[id].NewGame(deck.HandOfPlayer(id));
    }

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) % 4;
}

public class GameSession
{
    public GameSession(GameTable table, CardsDeck deck)
    {
        this.table = table;
        this.deck = deck;
    }

    private CardsDeck deck;
    private GameTable table;

    public GameHistory PlayGameUntilEnd(GameCall call)
    {
        table.SupplyHands(deck);

        var history = new GameHistory(call);
        var eval = new TurnEvaluator(call);
        var validator = new DrawValidatorFactory().Create(call.Mode);
        int kommtRaus = table.FirstDrawingPlayerId;

        foreach (int round in Enumerable.Range(0, 8))
        {
            Console.WriteLine($"starting round {round}");
            var turn = Turn.NewTurn((byte)kommtRaus);

            foreach (var player in table.PlayersInDrawingOrder(kommtRaus))
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
            kommtRaus = eval.WinnerId(turn);
        }

        table.Shift();
        return history;
    }
}
