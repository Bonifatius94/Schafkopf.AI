using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class CardPickerEnvTests
{
    [Fact]
    public void Test_CanPlayGame()
    {
        var rules = new GameRules();
        var cardCache = new Card[8];
        var rng = new Random();

        var env = new CardPickerEnv();
        var state = env.Reset();
        foreach (int i in Enumerable.Range(0, 32))
        {
            var possActions = rules.PossibleCards(state, cardCache);
            var action = possActions[rng.Next(possActions.Length)];
            (state, var __, var ___) = env.Step(action);
            Assert.Equal(i+1, state.CardCount);
        }

        Assert.Equal(32, state.CardCount); // assert that no exception occurred
    }

    [Fact]
    public void Test_CanPlayConsequtiveGames()
    {
        var rules = new GameRules();
        var cardCache = new Card[8];
        var rng = new Random();
        var env = new CardPickerEnv();

        foreach (int _ in Enumerable.Range(0, 1000))
        {
            var state = env.Reset();
            foreach (int i in Enumerable.Range(0, 32))
            {
                var possActions = rules.PossibleCards(state, cardCache);
                var action = possActions[rng.Next(possActions.Length)];
                (state, var __, var ___) = env.Step(action);
                Assert.Equal(i+1, state.CardCount);
            }

            Assert.Equal(32, state.CardCount); // assert that no exception occurred
        }
    }
}

public class MultiAgentCardPickerEnvTests
{
    [Fact(Skip="not ready for testing yet")]
    public void Test_CanPlayGame()
    {
        var env = new MultiAgentCardPickerEnv();

        var tasks = Enumerable.Range(0, 4)
            .Select(i => Task.Run(() => playGame(i, env))).ToArray();
        Task.WaitAll(tasks, 1000);

        var finalStates = tasks.Select(x => x.Result);
        Assert.True(finalStates.All(s => s.CardCount == 32));
    }

    [Fact(Skip="not ready for testing yet")]
    public void Test_CanPlayConsequtiveGames()
    {
        var env = new MultiAgentCardPickerEnv();

        var tasks = Enumerable.Range(0, 4)
            .Select(i => Task.Run(() => {
                for (int j = 0; j < 100; j++)
                {
                    var finalState = playGame(i, env);
                    Assert.Equal(32, finalState.CardCount);
                }
            })).ToArray();
        Task.WaitAll(tasks, 10_000);

        Assert.True(tasks.All(s => s.Status == TaskStatus.RanToCompletion));
    }

    private static readonly Random rng = new Random();

    private GameLog playGame(int playerId, MultiAgentCardPickerEnv env)
    {
        var cache = new Card[8];
        var rules = new GameRules();
        var pickCard = (GameLog s) => {
            var possCards = rules.PossibleCards(s, cache);
            return possCards[rng.Next(possCards.Length)];
        };

        env.Register(playerId);
        var state = env.Reset();
        for (int i = 0; i < 8; i++)
            (state, var reward, var isTerm) = env.Step(pickCard(state));

        return state;
    }
}
