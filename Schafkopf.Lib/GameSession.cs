namespace Schafkopf.Lib;

public class GameSession
{
    public GameSession(GameTable table, CardsDeck deck)
    {
        this.table = table;
        this.deck = deck;
    }

    private CardsDeck deck;
    private GameTable table;

    private static readonly DrawValidator validator = new DrawValidator();
    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    public GameHistory ProcessGame()
    {
        deck.Shuffle();

        var initialHandsWithoutMeta = deck.InitialHands();
        int klopfer = askForKlopfer(initialHandsWithoutMeta);
        var call = makeCalls(klopfer, initialHandsWithoutMeta);

        if (call.Mode == GameMode.Weiter)
        {
            table.Shift();
            return new GameHistory(
                call, initialHandsWithoutMeta, table.FirstDrawingPlayerId);
        }

        var initialHands = deck.InitialHands(call);
        int kommtRaus = table.FirstDrawingPlayerId;
        var history = new GameHistory(call, initialHands, kommtRaus);

        foreach (var player in table.PlayersInDrawingOrder())
            player.NewGame(initialHands[player.Id]);
        history = playGameUntilEnd(history);

        table.Shift();
        return history;
    }

    #region Call

    private GameCall makeCalls(int klopfer, Hand[] initialHands)
    {
        int pos = 0;
        var call = GameCall.Weiter();

        foreach (var player in table.PlayersInDrawingOrder())
        {
            var hand = initialHands[player.Id];
            var possibleCalls = callGen.AllPossibleCalls(player.Id, initialHands, call);
            var nextCall = player.MakeCall(possibleCalls, pos++, hand, klopfer);
            if (nextCall.Mode == GameMode.Weiter)
                continue;
            call = nextCall;
        }

        return call;
    }

    private int askForKlopfer(Hand[] initialHands)
    {
        return Enumerable.Range(0, 4).Zip(table.PlayersInDrawingOrder())
            .Select(x => (Pos: x.First, Player: x.Second))
            .Where(x => x.Player.IsKlopfer(
                x.Pos, initialHands[x.Player.Id].Take(4)))
            .Count();
    }

    private void askForKontraRe(GameHistory history)
    {
        if (!history.IsKontraCalled)
        {
            var opponents = table.PlayersById(history.OpponentIds);
            bool kontraCalled = opponents.Any(p => p.CallKontra(history));
            if (kontraCalled)
                history.CallKontra();
        }

        if (history.IsKontraCalled)
        {
            var callers = table.PlayersById(history.CallerIds);
            bool reCalled = callers.Any(p => p.CallKontra(history));
            if (reCalled)
                history.CallRe();
        }
    }

    #endregion Call

    private GameHistory playGameUntilEnd(GameHistory history)
    {
        foreach (var newTurn in history)
        {
            var turn = newTurn;

            if (history.TurnCount == 8)
            {
                foreach (var player in
                    table.PlayersInDrawingOrder(history.KommtRaus))
                {
                    var lastCard = player.Hand.First();
                    turn = history.NextCard(lastCard);
                }
            }
            else
            {
                foreach (var player in
                    table.PlayersInDrawingOrder(history.KommtRaus))
                {
                    if (history.CanKontraRe)
                        askForKontraRe(history);

                    var possibleCards = player.Hand
                        .Where(card => validator.CanPlayCard(
                            history.Call, card, turn, player.Hand))
                        .ToArray();

                    var card = player.ChooseCard(history, possibleCards);
                    player.Discard(card);
                    turn = history.NextCard(card);
                }
            }
        }

        return history;
    }
}
