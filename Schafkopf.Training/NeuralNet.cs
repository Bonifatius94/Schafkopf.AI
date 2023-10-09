namespace Schafkopf.Training;

public class SupervisedTrainingSession
{
    private FFModel model;
    private IOptimizer optimizer;
    private ILoss loss;
    private FlatFeatureDataset dataset;
    private int batchSize;

    public void Compile(
        FFModel model, IOptimizer optimizer, ILoss loss,
        FlatFeatureDataset dataset, int batchSize = 64)
    {
        this.model = model;
        this.optimizer = optimizer;
        this.loss = loss;
        this.dataset = dataset;
        this.batchSize = batchSize;

        int inputDims = dataset.TrainX.NumCols;
        model.Compile(batchSize, inputDims);
        optimizer.Compile(model.GradsTape);
    }

    public void Train(
        int epochs, bool shuffle = true,
        Action<float>? lossLogger = null)
    {
        int numTrainExamples = dataset.TrainX.NumRows;
        int numTrainBatches = numTrainExamples / batchSize;
        var perm = Perm.Identity(numTrainExamples);

        var trainX = new Matrix2D(batchSize, dataset.TrainX.NumCols, dataset.TrainX.Data);
        var trainY = new Matrix2D(batchSize, dataset.TrainY.NumCols, dataset.TrainY.Data);

        var pred = model.Layers.Last().Cache.Output;
        var deltas = model.Layers.Last().Cache.DeltasIn;

        for (int ep = 0; ep < epochs; ep++)
        {
            if (shuffle)
            {
                Perm.Permutate(perm);
                Matrix2D.ShuffleRows(dataset.TrainX, perm);
                Matrix2D.ShuffleRows(dataset.TrainY, perm);
            }

            int offset = 0;
            for (int i = 0; i < numTrainBatches; i++)
            {
                trainX.Data = dataset.TrainX.Data[offset..(offset + batchSize)];
                trainY.Data = dataset.TrainY.Data[offset..(offset + batchSize)];
                offset += batchSize;

                // info: functions use pre-allocated caches
                model.Predict(trainX);
                loss.LossDeltas(pred, trainY, deltas);
                model.Fit(optimizer);

                lossLogger?.Invoke(loss.Loss(pred, trainY));
            }
        }
    }
}

public class FlatFeatureDataset
{
    public Matrix2D TrainX;
    public Matrix2D TrainY;
    public Matrix2D TestX;
    public Matrix2D TestY;
}

public class FFModel
{
    public FFModel(IList<ILayer> layers)
    {
        Layers = layers;
    }

    public IList<ILayer> Layers;
    public IList<Matrix2D> GradsTape;

    public void Compile(int batchSize, int inputDims)
    {
        GradsTape = new List<Matrix2D>();
        var input = new Matrix2D(batchSize, inputDims);
        var deltaOut = new Matrix2D(batchSize, inputDims);

        foreach (var layer in Layers)
        {
            layer.Compile(inputDims);
            inputDims = layer.OutputDims;
        }

        foreach (var layer in Layers)
        {
            layer.CompileCache(input, deltaOut);
            GradsTape.Add(layer.Cache.Gradients);
            input = layer.Cache.Output;
            deltaOut = layer.Cache.DeltasIn;
        }
    }

    public Matrix2D Predict(Matrix2D input)
    {
        // TODO: handle case where the amount of input rows
        //       is greater than the batch size of layer caches

        var firstLayerCache = Layers.First().Cache;
        var origInput = firstLayerCache.Input;
        firstLayerCache.Input = input;

        foreach (var layer in Layers)
            layer.Forward();

        firstLayerCache.Input = origInput;
        return Layers.Last().Cache.Output;
    }

    public void Fit(IOptimizer opt)
    {
        foreach (var layer in Layers.Reverse())
            layer.Backward();

        opt.AdjustGrads(GradsTape);

        foreach (var layer in Layers)
            layer.ApplyGrads();
    }
}

public interface ILoss
{
    float Loss(Matrix2D pred, Matrix2D target);
    void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas);
}

public class MeanSquaredError : ILoss
{
    public float Loss(Matrix2D pred, Matrix2D target)
    {
        float sum = 0f;
        for (int i = 0; i < pred.NumRows * pred.NumCols; i++)
        {
            float diff = pred.Data[i] - target.Data[i];
            sum += diff * diff;
        }
        return sum / pred.NumRows;
    }

    public void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas)
    {
        Matrix2D.ElemSub(pred, target, deltas);
        Matrix2D.BatchDiv(deltas, pred.NumRows, deltas);
    }
}

public interface IOptimizer
{
    void AdjustGrads(IList<Matrix2D> grads);
    void Compile(IList<Matrix2D> grads);
}

public class NaiveSGDOpt : IOptimizer
{
    public NaiveSGDOpt(float learnRate)
    {
        this.learnRate = learnRate;
    }

    private float learnRate;

    public void AdjustGrads(IList<Matrix2D> grads)
    {
        foreach (var g in grads)
            if (g != null)
                Matrix2D.BatchMul(g, learnRate, g);
    }

    public void Compile(IList<Matrix2D> grads)
    {
        // info: nothing to initialize
    }
}

public class AdamOpt : IOptimizer
{
    public AdamOpt(
        float learnRate, float beta1 = 0.9f,
        float beta2 = 0.999f, float epsilon = 1e-8f)
    {
        this.learnRate = learnRate;
        this.beta1 = beta1;
        this.beta2 = beta2;
        this.epsilon = epsilon;
    }

    private float learnRate = 0.001f;
    private float beta1 = 0.9f;
    private float beta2 = 0.999f;
    private float epsilon = 1e-8f;

    private float beta1t = 1.0f;
    private float beta2t = 1.0f;
    private IList<Matrix2D> ms;
    private IList<Matrix2D> vs;
    private IList<Matrix2D> mTemps;
    private IList<Matrix2D> vTemps;

    public void Compile(IList<Matrix2D> allGrads)
    {
        beta1t = 1.0f;
        beta2t = 1.0f;
        ms = new List<Matrix2D>();
        vs = new List<Matrix2D>();
        mTemps = new List<Matrix2D>();
        vTemps = new List<Matrix2D>();

        foreach (var grads in allGrads)
        {
            var hasGrads = grads != null;
            ms.Add(hasGrads ? new Matrix2D(grads.NumRows, grads.NumCols): null);
            vs.Add(hasGrads ? new Matrix2D(grads.NumRows, grads.NumCols): null);
            mTemps.Add(hasGrads ? new Matrix2D(grads.NumRows, grads.NumCols): null);
            vTemps.Add(hasGrads ? new Matrix2D(grads.NumRows, grads.NumCols): null);
        }
    }

    public void AdjustGrads(IList<Matrix2D> allGrads)
    {
        for (int i = 0; i < allGrads.Count; i++)
        {
            if (allGrads[i] == null)
                continue;

            var grads = allGrads[i];
            var m = ms[i];
            var v = vs[i];
            var mTemp = mTemps[i];
            var vTemp = vTemps[i];

            Matrix2D.BatchMul(m, beta1, m);
            Matrix2D.BatchMul(grads, 1 - beta1, mTemp);
            Matrix2D.ElemAdd(m, mTemp, m);

            Matrix2D.BatchMul(v, beta2, v);
            Matrix2D.ElemMul(grads, grads, vTemp);
            Matrix2D.BatchMul(vTemp, 1 - beta2, vTemp);
            Matrix2D.ElemAdd(v, vTemp, v);

            beta1t *= beta1;
            beta2t *= beta2;

            Matrix2D.BatchDiv(m, 1 - beta1t, mTemp);
            Matrix2D.BatchDiv(v, 1 - beta2t, vTemp);

            Matrix2D.BatchMul(mTemp, learnRate, mTemp);
            Matrix2D.ElemSqrt(vTemp, vTemp);
            Matrix2D.BatchAdd(vTemp, epsilon, vTemp);
            Matrix2D.ElemDiv(mTemp, vTemp, grads);
        }
    }
}

public class LayerCache
{
    public Matrix2D Input;
    public Matrix2D Output;
    public Matrix2D DeltasIn; // info: incoming backwards
    public Matrix2D DeltasOut;
    public Matrix2D Gradients;
}

public interface ILayer
{
    LayerCache Cache { get; }
    int InputDims { get; }
    int OutputDims { get; }

    void Compile(int inputDims);
    void CompileCache(Matrix2D inputs, Matrix2D deltasOut);

    void Forward();
    void Backward();
    void ApplyGrads();
}

public class DenseLayer : ILayer
{
    public DenseLayer(int outputDims)
    {
        OutputDims = outputDims;
    }

    public LayerCache Cache { get; private set; }

    public int InputDims { get; private set; }
    public int OutputDims { get; private set; }
    public Matrix2D Weights;
    public Matrix2D Biases;
    public Matrix2D WeightGrads { get; private set; }
    public Matrix2D BiasGrads { get; private set; }

    public void Compile(int inputDims)
    {
        InputDims = inputDims;
        Weights = Matrix2D.RandNorm(InputDims, OutputDims, 0.0f, 0.1f);
        Biases = Matrix2D.Zeros(1, OutputDims);
    }

    public void CompileCache(Matrix2D inputs, Matrix2D deltasOut)
    {
        int batchSize = inputs.NumRows;
        Cache = new LayerCache() {
            Input = inputs,
            Output = new Matrix2D(batchSize, OutputDims),
            DeltasIn = new Matrix2D(batchSize, OutputDims),
            DeltasOut = deltasOut,
            Gradients = new Matrix2D(1, (InputDims + 1) * OutputDims)
        };

        int bound = InputDims * OutputDims;
        var weightGradsArr = new ArraySegment<float>(
            Cache.Gradients.Data, 0, bound);
        var biasGradsArr = new ArraySegment<float>(
            Cache.Gradients.Data, bound, Cache.Gradients.Data.Length - bound);
        WeightGrads = new Matrix2D(InputDims, OutputDims, weightGradsArr.Array);
        BiasGrads = new Matrix2D(1, OutputDims, biasGradsArr.Array);
    }

    public void Forward()
    {
        Matrix2D.Matmul(Cache.Input, Weights, Cache.Output);
        Matrix2D.RowAdd(Cache.Output, Biases, Cache.Output);
    }

    public void Backward()
    {
        Matrix2D.Matmul(Cache.Input, Cache.DeltasIn, WeightGrads, MatmulFlags.TN);
        Matrix2D.ColMean(Cache.DeltasIn, BiasGrads);
        Matrix2D.Matmul(Cache.DeltasIn, Weights, Cache.DeltasOut, MatmulFlags.NT);
    }

    public void ApplyGrads()
    {
        Matrix2D.ElemSub(Weights, WeightGrads, Weights);
        Matrix2D.ElemSub(Biases, BiasGrads, Biases);
    }

    public void Load(IList<Matrix2D> trainParams)
    {
        var newWeights = trainParams[0];
        var newBiases = trainParams[1];

        if (Weights.NumRows != newWeights.NumRows ||
                Biases.NumCols != newBiases.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        Weights.Data = (float[])newWeights.Data.Clone();
        Biases.Data = (float[])newBiases.Data.Clone();
    }
}

public class ReLULayer : ILayer
{
    public LayerCache Cache { get; private set; }

    public int InputDims { get; private set; }

    public int OutputDims { get; private set; }

    public void Compile(int inputDims)
    {
        InputDims = inputDims;
        OutputDims = inputDims;
    }

    public void CompileCache(Matrix2D inputs, Matrix2D deltasOut)
    {
        int batchSize = inputs.NumRows;
        Cache = new LayerCache() {
            Input = inputs,
            Output = new Matrix2D(batchSize, OutputDims),
            DeltasIn = new Matrix2D(batchSize, OutputDims),
            DeltasOut = deltasOut,
            Gradients = null
        };
    }

    public void Forward()
    {
        Matrix2D.ElemMax(Cache.Input, 0, Cache.Output);
    }

    public void Backward()
    {
        Matrix2D.ElemGeq(Cache.Input, 0, Cache.DeltasOut);
        Matrix2D.ElemMul(Cache.DeltasOut, Cache.DeltasIn, Cache.DeltasOut);
    }

    public void ApplyGrads()
    {
        // info: layer has no trainable params
    }
}
