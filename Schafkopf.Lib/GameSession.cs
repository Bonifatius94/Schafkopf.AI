namespace Schafkopf.Lib;

public interface ISchafkopfPlayer
{
    Hand Hand { get; }

    void NewGame(GameCall call, Hand hand);
    void OnInvalidCardPicked(Card card);
    Card ChooseCard(Turn currentTurn);
    void OnGameFinished(GameResult result);
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
        table.SupplyHands(call, deck);

        var initialHands = Enumerable.Range(0, 4)
            .Select(i => (i, deck.HandOfPlayerWithMeta(i, call)));
        var history = new GameHistory(call, initialHands.ToList());
        var validator = new DrawValidator();
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
                    bool valid = !validator.CanPlayCard(call, card, turn, player.Hand);
                    if (valid) break;
                    player.OnInvalidCardPicked(card);
                }

                var newHand = player.Hand.Discard(card);
                // TODO: deal with immutability of hand
                turn = turn.NextCard(card);
            }

            history.Append(turn);
            kommtRaus = turn.WinnerId(call);
        }

        table.Shift();
        return history;
    }
}
