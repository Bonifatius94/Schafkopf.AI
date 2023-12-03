using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class FeatureVectorTests
{
    [Fact]
    public void Test_CanSerializeCompleteGame()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var history = generateHistoryWithCall(call);

        var newExp = () => new SarsExp() { StateBefore = new GameState() };
        var states = Enumerable.Range(0, 32).Select(i => newExp()).ToArray();
        serializer.SerializeSarsExps(history, states);

        Assert.True(true); // serialization does not throw exception
    }

    [Fact]
    public void Test_CanSerializeLiveGame()
    {
        var serializer = new GameStateSerializer();
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var completeGame = generateHistoryWithCall(call);
        var actions = completeGame.UnrollActions().ToArray();

        int kommtRaus = completeGame.Turns[0].FirstDrawingPlayerId;
        var liveGame = GameLog.NewLiveGame(
            call, completeGame.InitialHands, kommtRaus);

        foreach (var action in actions)
        {
            serializer.SerializeState(liveGame);
            liveGame.NextCard(action.CardPlayed);
        }

        Assert.True(true); // serialization does not throw exception
    }

    #region HistoryGenerator

    private GameLog playRandomGame(GameCall call, Hand[] initialHands)
    {
        var gameRules = new GameRules();
        var liveGame = GameLog.NewLiveGame(call, initialHands, 0);
        var cardsCache = new Card[8];

        foreach (var _ in Enumerable.Range(0, 32))
            liveGame.NextCard(gameRules.PossibleCards(liveGame, cardsCache)[0]);

        return liveGame;
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
            possCalls = callGen.AllPossibleCalls(
                0, initialHands, GameCall.Weiter()).ToArray();
            possCalls.Contains(expCall);
        } while (!possCalls.Contains(expCall));

        return playRandomGame(expCall, initialHands);
    }

    #endregion HistoryGenerator

    // [Fact(Skip = "code is not ready yet")]
    // public void Test_CanEncodeSauspielCall()
    // {
    //     // TODO: transform this into a theory with multiple calls

    //     var serializer = new GameStateSerializer();
    //     var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
    //     var history = generateHistoryWithCall(call);

    //     var states = serializer.NewBuffer();
    //     serializer.Serialize(history, states);

    //     Assert.True(states.All(x => x.State[0] == 0.25));
    //     Assert.True(states.All(x => x.State[1] == 0));
    //     Assert.True(states.All(x => x.State[2] == 0));
    //     Assert.True(states.All(x => x.State[3] == 0.25));
    //     Assert.True(states.All(x => x.State[4] == 0.25));
    //     Assert.True(states.All(x => x.State[5] == 0));
    // }

    // private void assertValidHandEncoding(GameState state, Hand hand)
    // {
    //     int p = 6;
    //     var cards = hand.ToArray();

    //     for (int i = 0; i < cards.Length; i++)
    //     {
    //         Assert.Equal((double)cards[i].Type / 8, state.State[p++]);
    //         Assert.Equal((double)cards[i].Color / 4, state.State[p++]);
    //     }

    //     for (int i = cards.Length; i < 8; i++)
    //     {
    //         Assert.Equal(-1, state.State[p++]);
    //         Assert.Equal(-1, state.State[p++]);
    //     }
    // }

    // [Fact(Skip = "code is not ready yet")]
    // public void Test_CanEncodeHands()
    // {
    //     var serializer = new GameStateSerializer();
    //     var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
    //     var history = generateHistoryWithCall(call);

    //     var states = serializer.NewBuffer();
    //     serializer.Serialize(history, states);

    //     foreach ((var hand, var state) in history.UnrollHands().Zip(states))
    //         assertValidHandEncoding(state, hand);
    // }

    // private void assertValidTurnHistory(
    //     GameState state, ReadOnlySpan<GameAction> history, int t)
    // {
    //     int p = 22;

    //     for (int i = 0; i < t; i++)
    //     {
    //         var cardPlayed = history[i].CardPlayed;
    //         Assert.Equal((double)cardPlayed.Type / 8, state.State[p++]);
    //         Assert.Equal((double)cardPlayed.Color / 4, state.State[p++]);
    //     }

    //     for (int i = t; i < 32; i++)
    //     {
    //         Assert.Equal(-1, state.State[p++]);
    //         Assert.Equal(-1, state.State[p++]);
    //     }
    // }

    // [Fact(Skip = "code is not ready yet")]
    // public void Test_CanEncodeTurnHistory()
    // {
    //     var serializer = new GameStateSerializer();
    //     var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
    //     var history = generateHistoryWithCall(call);
    //     var allActions = history.UnrollActions().ToArray();

    //     var states = serializer.NewBuffer();
    //     serializer.Serialize(history, states);

    //     foreach ((int t, var state) in Enumerable.Range(0, 33).Zip(states))
    //         assertValidTurnHistory(state, allActions, t);
    // }

    // private void assertValidAugen(GameState state, int[] augen)
    // {
    //     for (int i = 0; i < 4; i++)
    //         Assert.Equal((double)augen[i] / 120, state.State[i+86]);
    // }

    // [Fact(Skip = "code is not ready yet")]
    // public void Test_CanEncodeAugen()
    // {
    //     var serializer = new GameStateSerializer();
    //     var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
    //     var history = generateHistoryWithCall(call);
    //     var allAugen = history.UnrollAugen().Select(x => x.ToArray()).ToArray();

    //     var states = serializer.NewBuffer();
    //     serializer.Serialize(history, states);

    //     foreach ((int t, var state) in Enumerable.Range(0, 33).Zip(states))
    //         assertValidAugen(state, allAugen[t / 4]);
    // }
}
