namespace Schafkopf.Training;

public class SupervisedTrainingSession
{
    private FFModel model;
    private IOptimizer optimizer;
    private ILoss lossFunc;
    private FlatFeatureDataset dataset;
    private int batchSize;

    public void Compile(
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

    public void Train(
        int epochs, bool shuffle = true,
        Action<int, double>? lossLogger = null)
    {
        int numExamples = dataset.TrainX.NumRows;
        int numBatches = numExamples / batchSize;
        var perm = Perm.Identity(numExamples);
        var x = Matrix2D.Zeros(batchSize, dataset.TrainX.NumCols);
        var y = Matrix2D.Zeros(batchSize, dataset.TrainY.NumCols);

        for (int ep = 0; ep < epochs; ep++)
        {
            if (shuffle)
            {
                Perm.Permutate(perm);
                Matrix2D.ShuffleRows(dataset.TrainX, perm);
                Matrix2D.ShuffleRows(dataset.TrainY, perm);
            }

            unsafe
            {
                x.Data = dataset.TrainX.Data;
                y.Data = dataset.TrainY.Data;
            }

            for (int i = 0; i < numBatches; i++)
            {
                unsafe
                {
                    x.Data += batchSize * x.NumCols;
                    y.Data += batchSize * y.NumCols;
                }

                model.TrainStep(x, y, lossFunc, optimizer);
            }

            lossLogger?.Invoke(ep + 1, Eval());
        }
    }

    public double Eval()
    {
        int numExamples = dataset.TestX.NumRows;
        int numBatches = numExamples / batchSize;
        var x = Matrix2D.Zeros(batchSize, dataset.TestX.NumCols);
        var y = Matrix2D.Zeros(batchSize, dataset.TestY.NumCols);
        double lossSum = 0.0;

        unsafe
        {
            x.Data = dataset.TestX.Data;
            y.Data = dataset.TestY.Data;
        }

        for (int i = 0; i < numBatches; i++)
        {
            unsafe
            {
                x.Data += batchSize * x.NumCols;
                y.Data += batchSize * y.NumCols;
            }

            var pred = model.Predict(x);
            lossSum += lossFunc.Loss(pred, y);
        }

        return lossSum / numBatches;
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
        var input = Matrix2D.Zeros(batchSize, inputDims);
        var deltaOut = Matrix2D.Zeros(batchSize, inputDims);

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

        unsafe
        {
            double* origData = firstLayerCache.Input.Data;
            firstLayerCache.Input.Data = input.Data;

            foreach (var layer in Layers)
                layer.Forward();

            firstLayerCache.Input.Data = origData;
        }

        return Layers.Last().Cache.Output;
    }

    public void TrainStep(Matrix2D x, Matrix2D y, ILoss lossFunc, IOptimizer opt)
    {
        var firstLayerCache = Layers.First().Cache;
        var lastLayerCache = Layers.Last().Cache;
        var origInput = firstLayerCache.Input;
        firstLayerCache.Input = x;

        foreach (var layer in Layers)
            layer.Forward();

        lossFunc.LossDeltas(lastLayerCache.Output, y, lastLayerCache.DeltasIn);

        foreach (var layer in Layers.Reverse())
            layer.Backward();

        opt.AdjustGrads(GradsTape);
        foreach (var layer in Layers)
            layer.ApplyGrads();

        firstLayerCache.Input = origInput;
    }
}

public interface ILoss
{
    double Loss(Matrix2D pred, Matrix2D target);
    void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas);
}

public class MeanSquaredError : ILoss
{
    public double Loss(Matrix2D pred, Matrix2D target)
    {
        double sum = 0;
        unsafe
        {
            for (int i = 0; i < pred.NumRows * pred.NumCols; i++)
            {
                double diff = pred.Data[i] - target.Data[i];
                sum += diff * diff;
            }
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
    public NaiveSGDOpt(double learnRate)
    {
        this.learnRate = learnRate;
    }

    private double learnRate;

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
        double learnRate, double beta1 = 0.9,
        double beta2 = 0.999, double epsilon = 1e-8)
    {
        this.learnRate = learnRate;
        this.beta1 = beta1;
        this.beta2 = beta2;
        this.epsilon = epsilon;
    }

    private double learnRate = 0.001;
    private double beta1 = 0.9;
    private double beta2 = 0.999;
    private double epsilon = 1e-8;

    private double beta1t = 1.0;
    private double beta2t = 1.0;
    private IList<Matrix2D> ms;
    private IList<Matrix2D> vs;
    private IList<Matrix2D> mTemps;
    private IList<Matrix2D> vTemps;

    public void Compile(IList<Matrix2D> allGrads)
    {
        beta1t = 1.0;
        beta2t = 1.0;
        ms = new List<Matrix2D>();
        vs = new List<Matrix2D>();
        mTemps = new List<Matrix2D>();
        vTemps = new List<Matrix2D>();

        foreach (var grads in allGrads)
        {
            var hasGrads = grads != null;
            ms.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols): null);
            vs.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols): null);
            mTemps.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols): null);
            vTemps.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols): null);
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
        Weights = Matrix2D.RandNorm(InputDims, OutputDims, 0.0, 0.01);
        Biases = Matrix2D.Zeros(1, OutputDims);
    }

    public void CompileCache(Matrix2D inputs, Matrix2D deltasOut)
    {
        int batchSize = inputs.NumRows;
        Cache = new LayerCache() {
            Input = inputs,
            Output = Matrix2D.Zeros(batchSize, OutputDims),
            DeltasIn = Matrix2D.Zeros(batchSize, OutputDims),
            DeltasOut = deltasOut,
            Gradients = Matrix2D.Zeros(1, (InputDims + 1) * OutputDims)
        };

        unsafe
        {
            var weightGrads = Cache.Gradients.Data;
            var biasGrads = Cache.Gradients.Data + InputDims * OutputDims;
            var cache = Cache.Gradients.Cache;
            WeightGrads = Matrix2D.FromRawPointers(InputDims, OutputDims, weightGrads, cache);
            BiasGrads = Matrix2D.FromRawPointers(1, OutputDims, biasGrads, cache);
        }
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

        unsafe
        {
            Weights.Data = trainParams[0].Data;
            Biases.Data = trainParams[1].Data;
        }
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
            Output = Matrix2D.Zeros(batchSize, OutputDims),
            DeltasIn = Matrix2D.Zeros(batchSize, OutputDims),
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
