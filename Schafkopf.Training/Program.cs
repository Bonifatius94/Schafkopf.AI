namespace Schafkopf.Training;

public class Program
{
    private static FlatFeatureDataset createDataset(int trainSize, int testSize)
    {
        var rng = new Random();
        Func<int, Func<double, double>, (double, double)[]> sample =
            (size, func) => Enumerable.Range(0, size)
                .Select(i => (double)rng.NextDouble() * 20 - 10)
                .Select(x => ((double)x, (double)func(x)))
                .ToArray();

        var trueFunc = (double x) => (double)Math.Sin(x);
        var trainData = sample(trainSize, trueFunc);
        var testData = sample(testSize, trueFunc);

        var trainX = Matrix2D.FromData(trainSize, 1, trainData.Select(x => x.Item1).ToArray());
        var trainY = Matrix2D.FromData(trainSize, 1, trainData.Select(x => x.Item2).ToArray());
        var testX = Matrix2D.FromData(testSize, 1, testData.Select(x => x.Item1).ToArray());
        var testY = Matrix2D.FromData(testSize, 1, testData.Select(x => x.Item2).ToArray());
        return new FlatFeatureDataset(trainX, trainY, testX, testY);
    }

    private static FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1),
        });

    public static void Main(string[] args)
    {
        int batchSize = 64;
        var model = createModel();
        var dataset = createDataset(trainSize: 10_000, testSize: 1_000);
        var optimizer = new AdamOpt(learnRate: 0.001);
        var lossFunc = new MeanSquaredError();

        var session = new SupervisedTrainingSession(
            model, optimizer, lossFunc, dataset, batchSize);
        Console.WriteLine("Training started!");
        Console.WriteLine($"loss before: {session.Eval()}");
        session.Train(100, true, (ep, l) => Console.WriteLine($"loss ep. {ep}: {l}"));
        Console.WriteLine("Training finished!");
    }
}
