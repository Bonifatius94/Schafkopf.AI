using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class FeatureVectorTests
{
    #region HistoryGenerator

    private Turn[] playRandomGame(GameCall call, Hand[] initialHands)
    {
        var cardRules = new DrawValidator();
        var handsWithMeta = initialHands
            .Select(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();

        int p_id = 0;
        var history = new Turn[8];
        var turn = Turn.InitFirstTurn(0, call);
        for (int t_id = 0; t_id < 7; t_id++)
        {
            for (int i = 0; i < 4; i++)
            {
                var hand = handsWithMeta[p_id];
                var card = hand.Where(c => cardRules.CanPlayCard(call, c, turn, hand)).First();
                turn = turn.NextCard(card);
                handsWithMeta[p_id] = hand.Discard(card);
                p_id = (p_id + 1) % 4;
            }
            history[t_id] = turn;
            turn = Turn.InitNextTurn(turn);
            p_id = turn.WinnerId;
        }

        for (int i = 0; i < 4; i++)
        {
            var card = handsWithMeta[p_id].First();
            turn = turn.NextCard(card);
            p_id = (p_id + 1) % 4;
        }
        history[7] = turn;

        return history;
    }

    private GameHistory generateHistoryWithCall(GameCall expCall)
    {
        var deck = new CardsDeck();
        var callGen = new GameCallGenerator();
        GameCall[] possCalls;
        Hand[] initialHands;

        do {
            deck.Shuffle();
            initialHands = deck.ToArray();
            possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter()).ToArray();
            possCalls.Contains(expCall);
        } while (!possCalls.Contains(expCall));

        var history = playRandomGame(expCall, initialHands);

        return new GameHistory() {
            Call = expCall,
            InitialHands = initialHands,
            History = history
        };
    }

    #endregion HistoryGenerator

    [Fact]
    public void Test_CanEncodeSauspielCall()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);

        var states = Enumerable.Range(0, 33).Select(x => new GameState()).ToArray();
        serializer.Serialize(history, states);

        Assert.True(states.All(x => x.State[0] == 0.25));
        Assert.True(states.All(x => x.State[1] == 0));
        Assert.True(states.All(x => x.State[2] == 0));
        Assert.True(states.All(x => x.State[3] == 0.25));
        Assert.True(states.All(x => x.State[4] == 0.25));
        Assert.True(states.All(x => x.State[5] == 0));
    }
}
