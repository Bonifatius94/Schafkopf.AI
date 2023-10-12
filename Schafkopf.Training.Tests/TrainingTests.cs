namespace Schafkopf.Training.Tests;

public class RegressionTrainingTest
{
    private FlatFeatureDataset createDataset(int trainSize, int testSize)
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

        return new FlatFeatureDataset() {
            TrainX = trainX, TrainY = trainY,
            TestX = testX, TestY = testY
        };
    }

    private FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1),
        });

    [Fact(Skip="code is not ready")]
    public void Test_CanPredictSinus()
    {
        int batchSize = 64;
        var model = createModel();
        var dataset = createDataset(trainSize: 10_000, testSize: batchSize);
        var optimizer = new AdamOpt(learnRate: 0.01);
        var lossFunc = new MeanSquaredError();
        var losses = new List<double>();

        var session = new SupervisedTrainingSession();
        session.Compile(model, optimizer, lossFunc, dataset, batchSize);
        session.Train(10, false, (ep, l) => losses.Add(l));
        var testPred = model.Predict(dataset.TestX);
        double testLoss = lossFunc.Loss(testPred, dataset.TestY);

        Assert.True(losses.Any(l => l < 0.001));
        Assert.True(testLoss < 0.001);
    }
}
