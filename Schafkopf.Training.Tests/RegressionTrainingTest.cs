namespace Schafkopf.Training.Tests;

public class RegressionTrainingTest
{
    private FlatFeatureDataset createDataset(int trainSize, int testSize)
    {
        var rng = new Random();
        Func<int, Func<float, float>, (float, float)[]> sample =
            (size, func) => Enumerable.Range(0, size)
                .Select(i => (float)rng.NextDouble() * 20 - 10)
                .Select(x => ((float)x, (float)func(x)))
                .ToArray();

        var trueFunc = (float x) => (float)Math.Sin(x);
        var trainData = sample(trainSize, trueFunc);
        var testData = sample(testSize, trueFunc);

        var trainX = new Matrix2D(trainSize, 1, trainData.Select(x => x.Item1).ToArray());
        var trainY = new Matrix2D(trainSize, 1, trainData.Select(x => x.Item2).ToArray());
        var testX = new Matrix2D(testSize, 1, testData.Select(x => x.Item1).ToArray());
        var testY = new Matrix2D(testSize, 1, testData.Select(x => x.Item2).ToArray());

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

    [Fact]
    public void Test_CanPredictSinus()
    {
        int batchSize = 64;
        var model = createModel();
        var dataset = createDataset(trainSize: 10_000, testSize: batchSize);
        var optimizer = new AdamOpt();
        var lossFunc = new MeanSquaredError();

        var session = new SupervisedTrainingSession();
        session.Compile(model, optimizer, lossFunc, dataset, batchSize);
        session.Train(10, false);
        var testPred = model.Predict(dataset.TestX);
        float testLoss = lossFunc.Loss(testPred, dataset.TestY);

        Assert.True(testLoss < 0.001);
    }
}
