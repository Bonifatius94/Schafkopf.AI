using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class FeatureVectorTests
{
    #region HistoryGenerator

    private Turn[] playRandomGame(GameCall call, Hand[] initialHands)
    {
        var gameRules = new GameRules();
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
                var card = hand.Where(c => gameRules.CanPlayCard(call, c, turn, hand)).First();
                turn = turn.NextCard(card);
                handsWithMeta[p_id] = hand.Discard(card);
                p_id = (p_id + 1) % 4;
            }
            history[t_id] = turn;
            p_id = turn.WinnerId;
            turn = Turn.InitNextTurn(turn);
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

    private GameLog generateHistoryWithCall(GameCall expCall)
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

        return new GameLog() {
            Call = expCall,
            InitialHands = initialHands,
            Turns = history
        };
    }

    #endregion HistoryGenerator

    [Fact(Skip = "code is not ready yet")]
    public void Test_CanEncodeSauspielCall()
    {
        // TODO: transform this into a theory with multiple calls

        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);

        var states = serializer.NewBuffer();
        serializer.Serialize(history, states);

        Assert.True(states.All(x => x.State[0] == 0.25));
        Assert.True(states.All(x => x.State[1] == 0));
        Assert.True(states.All(x => x.State[2] == 0));
        Assert.True(states.All(x => x.State[3] == 0.25));
        Assert.True(states.All(x => x.State[4] == 0.25));
        Assert.True(states.All(x => x.State[5] == 0));
    }

    private void assertValidHandEncoding(GameState state, Hand hand)
    {
        int p = 6;
        var cards = hand.ToArray();

        for (int i = 0; i < cards.Length; i++)
        {
            Assert.Equal((double)cards[i].Type / 8, state.State[p++]);
            Assert.Equal((double)cards[i].Color / 4, state.State[p++]);
        }

        for (int i = cards.Length; i < 8; i++)
        {
            Assert.Equal(-1, state.State[p++]);
            Assert.Equal(-1, state.State[p++]);
        }
    }

    [Fact(Skip = "code is not ready yet")]
    public void Test_CanEncodeHands()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);

        var states = serializer.NewBuffer();
        serializer.Serialize(history, states);

        foreach ((var hand, var state) in history.UnrollHands().Zip(states))
            assertValidHandEncoding(state, hand);
    }

    private void assertValidTurnHistory(
        GameState state, ReadOnlySpan<GameAction> history, int t)
    {
        int p = 22;

        for (int i = 0; i < t; i++)
        {
            var cardPlayed = history[i].CardPlayed;
            Assert.Equal((double)cardPlayed.Type / 8, state.State[p++]);
            Assert.Equal((double)cardPlayed.Color / 4, state.State[p++]);
        }

        for (int i = t; i < 32; i++)
        {
            Assert.Equal(-1, state.State[p++]);
            Assert.Equal(-1, state.State[p++]);
        }
    }

    [Fact(Skip = "code is not ready yet")]
    public void Test_CanEncodeTurnHistory()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);
        var allActions = history.UnrollActions().ToArray();

        var states = serializer.NewBuffer();
        serializer.Serialize(history, states);

        foreach ((int t, var state) in Enumerable.Range(0, 33).Zip(states))
            assertValidTurnHistory(state, allActions, t);
    }

    private void assertValidAugen(GameState state, int[] augen)
    {
        for (int i = 0; i < 4; i++)
            Assert.Equal((double)augen[i] / 120, state.State[i+86]);
    }

    [Fact(Skip = "code is not ready yet")]
    public void Test_CanEncodeAugen()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);
        var allAugen = history.UnrollAugen().Select(x => x.ToArray()).ToArray();

        var states = serializer.NewBuffer();
        serializer.Serialize(history, states);

        foreach ((int t, var state) in Enumerable.Range(0, 33).Zip(states))
            assertValidAugen(state, allAugen[t / 4]);
    }
}
