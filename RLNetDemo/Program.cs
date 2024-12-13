using RLNetDemo;
using Schafkopf.Training;

public class Program
{
    public static void Main(string[] args)
    {
        var config = new PPOTrainingSettings() {
            NumStateDims = 4,
            NumActionDims = 2,
            TotalSteps = 2_000_000,
            StepsPerUpdate = 2048
        };
        var model = new PPOModel(config);

        var adapter = new CartPolePPOAdapter(config);
        var rolloutBuffer = new PPORolloutBuffer<CartPoleState, CartPoleAction>(config, adapter);
        var expCollector = new SingleAgentExpCollector<CartPoleState, CartPoleAction>(
            config, adapter, () => new CartPoleEnv()
        );

        var benchmark = new CartPoleBenchmark(config, () => new CartPoleEnv());

        Console.WriteLine("benchmark untrained model");
        var res = benchmark.Benchmark(model, 1_000);
        Console.WriteLine($"avg. rewards: {res.AvgEpRewards}, avg. steps: {res.AvgEpSteps}");

        for (int ep = 0; ep < config.NumTrainings; ep++)
        {
            Console.WriteLine($"starting episode {ep+1}/{config.NumTrainings}");
            Console.WriteLine("collect rollout buffer");
            expCollector.Collect(rolloutBuffer, model);
            Console.WriteLine("train on rollout buffer");
            model.Train(rolloutBuffer);
            Console.WriteLine("benchmark model");
            res = benchmark.Benchmark(model, 1_000);
            Console.WriteLine($"avg. rewards: {res.AvgEpRewards}, avg. steps: {res.AvgEpSteps}");
            Console.WriteLine("===============================");
        }
    }
}
