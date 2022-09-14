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

        int klopfer = askForKlopfer();
        var call = makeCalls(klopfer);

        var initialHands = deck.InitialHands(call);
        int kommtRaus = table.FirstDrawingPlayerId;
        var history = new GameHistory(call, initialHands, kommtRaus);

        if (call.Mode == GameMode.Weiter)
            goto Leave;

        foreach (var player in table.PlayersInDrawingOrder())
            player.NewGame(initialHands[player.Id]);
        history = playGameUntilEnd(history);

     Leave:
        table.Shift();
        return history;
    }

    #region Call

    private GameCall makeCalls(int klopfer)
    {
        int pos = 0;
        var call = GameCall.Weiter();

        foreach (var player in table.PlayersInDrawingOrder())
        {
            var hand = deck.HandOfPlayer(player.Id);
            var possibleCalls = callGen.AllPossibleCalls(player.Id, deck, call);
            var nextCall = player.MakeCall(possibleCalls, pos++, hand, klopfer);
            if (nextCall.Mode == GameMode.Weiter)
                continue;
            call = nextCall;
        }

        return call;
    }

    private int askForKlopfer()
    {
        var drawingOrder = table.PlayersInDrawingOrder().ToArray();
        var handsInDrawingOrder = drawingOrder
            .Select(p => deck.HandOfPlayer(p.Id))
            .ToArray();

        int klopfer = Enumerable.Range(0, 4)
            .Select(pos => (
                    Player: drawingOrder[pos],
                    Pos: pos,
                    FirstFourCards: handsInDrawingOrder[pos].Cards.Take(4)
                ))
            .Where(x => x.Player.IsKlopfer(x.Pos, x.FirstFourCards))
            .Count();
        return klopfer;
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
        foreach (int round in Enumerable.Range(0, 8))
        {
            var players = table.PlayersInDrawingOrder(history.KommtRaus);
            foreach (var player in players)
            {
                if (history.CanKontraRe())
                    askForKontraRe(history);

                var possibleCards = player.Hand.Cards
                    .Where(card => validator.CanPlayCard(
                        history.Call, card, history.CurrentTurn, player.Hand))
                    .ToArray();

                var card = player.ChooseCard(history, possibleCards);
                player.Discard(card);
                history.NextCard(card);
            }

            history.NextTurn();
        }

        return history;
    }
}
