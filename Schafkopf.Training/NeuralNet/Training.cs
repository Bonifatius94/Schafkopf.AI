namespace Schafkopf.Training;

public class SupervisedTrainingSession
{
    public SupervisedTrainingSession(
        FFModel model, IOptimizer optimizer, ILoss lossFunc,
        FlatFeatureDataset dataset, int batchSize = 64)
    {
        this.model = model;
        this.optimizer = optimizer;
        this.lossFunc = lossFunc;
        this.dataset = dataset;
        this.batchSize = batchSize;

        int inputDims = dataset.TrainX.NumCols;
        model.Compile(batchSize, inputDims);
        optimizer.Compile(model.GradsTape);
    }

    private FFModel model;
    private IOptimizer optimizer;
    private ILoss lossFunc;
    private FlatFeatureDataset dataset;
    private int batchSize;

    public void Train(
        int epochs, bool shuffle = true,
        Action<int, double>? lossLogger = null)
    {
        int numExamples = dataset.TrainX.NumRows;
        int numBatches = numExamples / batchSize;
        var perm = Perm.Identity(numExamples);

        Matrix2D x, y;
        unsafe
        {
            x = Matrix2D.FromRawPointers(
                batchSize, dataset.TrainX.NumCols, dataset.TrainX.Data, null);
            y = Matrix2D.FromRawPointers(
                batchSize, dataset.TrainY.NumCols, dataset.TrainY.Data, null);
        }

        for (int ep = 0; ep < epochs; ep++)
        {
            unsafe
            {
                x.Data = dataset.TrainX.Data;
                y.Data = dataset.TrainY.Data;
            }

            if (shuffle)
                dataset.Shuffle(perm);

            for (int i = 0; i < numBatches; i++)
            {
                model.TrainBatch(x, y, lossFunc, optimizer);

                unsafe
                {
                    x.Data += batchSize * x.NumCols;
                    y.Data += batchSize * y.NumCols;
                }
            }

            lossLogger?.Invoke(ep + 1, Eval());
        }
    }

    public double Eval()
    {
        int numExamples = dataset.TestX.NumRows;
        int numBatches = numExamples / batchSize;
        double lossSum = 0.0;

        Matrix2D x, y;
        unsafe
        {
            x = Matrix2D.FromRawPointers(
                batchSize, dataset.TestX.NumCols, dataset.TestX.Data, null);
            y = Matrix2D.FromRawPointers(
                batchSize, dataset.TestY.NumCols, dataset.TestY.Data, null);
        }

        for (int i = 0; i < numBatches; i++)
        {
            var pred = model.PredictBatch(x);
            lossSum += lossFunc.Loss(pred, y);

            unsafe
            {
                x.Data += batchSize * x.NumCols;
                y.Data += batchSize * y.NumCols;
            }
        }

        return lossSum / numBatches;
    }
}

public class FlatFeatureDataset
{
    public FlatFeatureDataset(
        Matrix2D trainX, Matrix2D trainY,
        Matrix2D testX, Matrix2D testY)
    {
        TrainX = trainX;
        TrainY = trainY;
        TestX = testX;
        TestY = testY;
    }

    public Matrix2D TrainX { get; private set; }
    public Matrix2D TrainY { get; private set; }
    public Matrix2D TestX { get; private set; }
    public Matrix2D TestY { get; private set; }

    public void Shuffle(int[]? permCache = null)
    {
        var perm = permCache ?? Perm.Identity(TrainX.NumRows);
        Perm.Permutate(perm);
        Matrix2D.ShuffleRows(TrainX, perm);
        Matrix2D.ShuffleRows(TrainY, perm);
    }
}
