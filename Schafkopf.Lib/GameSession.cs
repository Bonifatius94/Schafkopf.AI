namespace Schafkopf.Lib;

public class GameSession
{
    public GameSession(Table table, CardsDeck deck)
    {
        this.table = table;
        this.deck = deck;
    }

    private CardsDeck deck;
    private Table table;

    private static readonly GameRules gameRules = new GameRules();
    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    private Hand[] initialHandsCache = new Hand[4];

    public GameLog ProcessGame() // TODO: pass history, reduce alloc
    {
        deck.Shuffle();

        deck.InitialHands(initialHandsCache);
        int klopfer = askForKlopfer(initialHandsCache);
        var call = makeCalls(klopfer, initialHandsCache);

        GameLog history;
        if (call.Mode == GameMode.Weiter)
            history = GameLog.NewLiveGame(call, initialHandsCache, table.FirstDrawingPlayerId);
        else
            history = playGame(call, initialHandsCache, klopfer);

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

    private Card[] firstFourCache = new Card[4];

    private int askForKlopfer(Hand[] initialHands)
    {
        int pos = 0;
        int klopfer = 0;
        foreach (var player in table.PlayersInDrawingOrder())
        {
            initialHands[player.Id].FirstFour(firstFourCache);
            if (player.IsKlopfer(pos++, firstFourCache))
                klopfer++;
        }
        return klopfer;
    }

    private void askForKontraRe(GameLog log)
    {
        var meta = log.Meta;
        if (!meta.IsKontraCalled && table.PlayersById(meta.OpponentIds).ToArray().Any(o => o.CallKontra(log)))
            meta.Kontra();

        if (meta.IsKontraCalled && table.PlayersById(meta.CallerIds).ToArray().Any(c => c.CallRe(log)))
            meta.Re();
    }

    #endregion Call

    private Hand[] handsWithMeta = new Hand[4];
    private Card[] possCardsCache = new Card[8];

    private GameLog playGame(GameCall call, Hand[] initialHands, int klopfer)
    {
        deck.InitialHands(call, initialHands);
        int kommtRaus = table.FirstDrawingPlayerId;

        for (int i = 0; i < 4; i++)
            handsWithMeta[i] = initialHands[i].CacheTrumpf(call.IsTrumpf);

        int p_id = kommtRaus;
        var log = GameLog.NewLiveGame(call, initialHands, p_id, klopfer);
        var turn = log.Turns[0];
        for (int t_id = 0; t_id < 7; t_id++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (t_id == 0 && i <= 1)
                    askForKontraRe(log);

                var player = table.Players[p_id];
                var hand = handsWithMeta[p_id];
                var possCards = gameRules.PossibleCards(call, turn, hand, possCardsCache);
                var cardToPlay = player.ChooseCard(log, possCards);
                turn = turn.NextCard(cardToPlay);
                handsWithMeta[p_id] = hand.Discard(cardToPlay);
                p_id = (p_id + 1) % 4;
            }
            log.Turns[t_id] = turn;
            p_id = turn.WinnerId;
            turn = Turn.InitNextTurn(turn);
        }

        for (int i = 0; i < 4; i++)
        {
            var card = handsWithMeta[p_id].First();
            turn = turn.NextCard(card);
            p_id = (p_id + 1) % 4;
        }
        log.Turns[7] = turn;

        return log;
    }
}
