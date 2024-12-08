using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class PPODatasetTests
{
    [Fact(Skip="still figuring out the issue")]
    public void Test_CanCollectSingleGame()
    {
        var env = new MultiAgentCardPickerEnv();
        var model = new PPOModel(new PPOTrainingSettings() { BatchSize = 1 });
        var vecAgent = new VectorizedCardPickerAgent(model, 1);
        var agents = Enumerable.Range(0, 4)
            .Select(i => new AsyncCardPickerAgent(vecAgent)).ToArray();

        var collectTasks = Enumerable.Range(0, 4)
            .Select(i => Task.Run(
                () => agents[i].PlaySteps(i % 4, env, 8).ToArray()))
            .ToArray();
        Task.WaitAll(collectTasks, 1_000);

        Assert.True(collectTasks.All(t => t.Status == TaskStatus.RanToCompletion));
        var results = collectTasks.Select(t => t.Result);
        var cardsPlayed = results.SelectMany(x => x.Select(y => y.Action)).ToHashSet();
        Assert.Equal(32, cardsPlayed.Count);
    }
}

public class BatchedPredictionTests
{
    [Fact(Skip="not ready for testing yet")]
    public void Test_CanPredictSingleStep_WhenUsingSingleAgent()
    {
        var env = new CardPickerEnv();
        var stateEnc = new GameStateSerializer();
        var model = new PPOModel(new PPOTrainingSettings() { BatchSize = 1 });
        var vecAgent = new VectorizedCardPickerAgent(model, 1);

        var state = env.Reset();
        var possCards = possibleCards(state);
        var task = Task.Run(() => {
            vecAgent.Register(0);
            var s0 = stateEnc.SerializeState(state);
            return vecAgent.Predict(s0, possCards);
        });
        task.Wait(1_000);

        Assert.True(task.Status == TaskStatus.RanToCompletion);
        Assert.Contains(task.Result.Item1, possCards);
    }

    [Fact(Skip="not ready for testing yet")]
    public void Test_CanPredictSingleStep_WhenUsingMultipleAgents()
    {
        var envs = Enumerable.Range(0, 4).Select(i => new CardPickerEnv()).ToArray();
        var stateEnc = new GameStateSerializer();
        var model = new PPOModel(new PPOTrainingSettings() { BatchSize = 4 });
        var vecAgent = new VectorizedCardPickerAgent(model, 4);

        var states = Enumerable.Range(0, 4).Select(i => envs[i].Reset()).ToArray();
        var possCards = Enumerable.Range(0, 4).Select(i => possibleCards(states[i])).ToArray();
        var tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() => {
            vecAgent.Register(i);
            var s0 = stateEnc.SerializeState(states[i]);
            return vecAgent.Predict(s0, possCards[i]);
        })).ToArray();
        Task.WaitAll(tasks, 1_000);

        Assert.True(tasks.All(t => t.Status == TaskStatus.RanToCompletion));
        Assert.True(Enumerable.Range(0, 4).All(i =>
            possCards[i].Contains(tasks[i].Result.Item1)));
    }

    [Fact(Skip="not ready for testing yet")]
    public void Test_CanPredictMultipleSteps_WhenUsingMultipleAgents()
    {
        var envs = Enumerable.Range(0, 4).Select(i => new CardPickerEnv()).ToArray();
        var stateEnc = new GameStateSerializer();
        var model = new PPOModel(new PPOTrainingSettings() { BatchSize = 4 });
        var vecAgent = new VectorizedCardPickerAgent(model, 4);
        var possCardsCache = Enumerable.Range(0, 4).Select(i =>
            Enumerable.Range(0, 8).Select(j => new Card[8]).ToArray()).ToArray();

        var tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() => {
            var results = new Card[8];
            vecAgent.Register(i);
            var state = envs[i].Reset();
            for (int j = 0; j < 8; j++)
            {
                var possCards = possibleCards(state);
                var s0 = stateEnc.SerializeState(state);
                (var a0, var pi, var V) = vecAgent.Predict(s0, possCards);
                (state, var r, var t) = envs[i].Step(a0);
                results[j] = a0;
                possCards.CopyTo(possCardsCache[i][j], 0);
            }
            return results;
        })).ToArray();
        Task.WaitAll(tasks, 1_000);

        Assert.True(tasks.All(t => t.Status == TaskStatus.RanToCompletion));
        Assert.True(Enumerable.Range(0, 4).All(i =>
            Enumerable.Range(0, 8).All(j => possCardsCache[i][j].Contains(tasks[i].Result[j]))));
    }

    private Card[] possibleCards(GameLog state)
        => new GameRules().PossibleCards(state, new Card[8]).ToArray();
}
