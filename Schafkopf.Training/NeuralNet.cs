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
    public NaiveSGDOpt(float learnRate = 0.001f)
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
        float learnRate = 0.001f, float beta1 = 0.9f,
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
    }

    public void Forward()
    {
        Matrix2D.Matmul(Cache.Input, Weights, Cache.Output);
        Matrix2D.RowAdd(Cache.Output, Biases, Cache.Output);
    }

    public void Backward()
    {
        int bound = InputDims * OutputDims;
        var weightGrads = new Matrix2D(InputDims, OutputDims, Cache.Gradients.Data[..bound]);
        var biasGrads = new Matrix2D(1, OutputDims, Cache.Gradients.Data[bound..]);

        Matrix2D.Matmul(Cache.Input, Cache.DeltasIn, weightGrads, MatmulFlags.TN);
        Matrix2D.ColMean(Cache.DeltasIn, biasGrads);
        Matrix2D.Matmul(Cache.DeltasIn, Weights, Cache.DeltasOut, MatmulFlags.NT);
    }

    public void ApplyGrads()
    {
        int bound = InputDims * OutputDims;
        var weightGrads = new Matrix2D(InputDims, OutputDims, Cache.Gradients.Data[..bound]);
        var biasGrads = new Matrix2D(1, OutputDims, Cache.Gradients.Data[bound..]);

        Matrix2D.ElemSub(Weights, weightGrads, Weights);
        Matrix2D.ElemSub(Biases, biasGrads, Biases);
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

public class Matrix2D
{
    public static Matrix2D Zeros(int numRows, int numCols)
        => new Matrix2D(numRows, numCols);

    public static Matrix2D RandNorm(int numRows, int numCols, float mu, float sig)
        => new Matrix2D(numRows, numCols,
            Enumerable.Range(0, numRows * numCols)
                .Select(i => (float)RandNormal.Next(mu, sig)).ToArray());

    public Matrix2D(
        int numRows, int numCols,
        float[]? data = null, float[]? cache = null)
    {
        NumRows = numRows;
        NumCols = numCols;
        Data = data ?? new float[NumRows * NumCols];
        Cache = cache ?? new float[NumRows * NumCols];
    }

    public int NumRows;
    public int NumCols;
    public float[] Data;
    public float [] Cache;

    public static void Matmul(
        Matrix2D a, Matrix2D b, Matrix2D res,
        MatmulFlags flags = MatmulFlags.NN)
    {
        bool a_normal = flags == MatmulFlags.NN || flags == MatmulFlags.NT;
        bool b_normal = flags == MatmulFlags.NN || flags == MatmulFlags.TN;

        // info: assuming shapes (l, m) x (m, n) -> (l, n)
        int l = a_normal ? a.NumRows : a.NumCols;
        int m = a_normal ? a.NumCols : a.NumRows;
        int m2 = b_normal ? b.NumRows : b.NumCols;
        int n = b_normal ? b.NumCols : b.NumRows;
        if (res.NumRows != l || res.NumCols != n || m != m2)
            throw new ArgumentException("Invalid matrix shapes!");

        var rowCache = new Matrix2D(l, m, a.Cache, Array.Empty<float>());
        var colCache = new Matrix2D(n, m, b.Cache, Array.Empty<float>());

        if (!a_normal)
            Transpose(a, rowCache);
        else
            rowCache.Data = a.Data;

        if (b_normal)
            Transpose(b, colCache);
        else
            colCache.Data = b.Data;

        rowCache.NumRows = 1;
        colCache.NumRows = 1;

        int r = 0, p = 0, c;
        for (int i = 0; i < l; i++)
        {
            c = 0;
            for (int j = 0; j < n; j++)
            {
                rowCache.Data = a.Cache[r..(r+m)];
                colCache.Data = b.Cache[c..(c+m)];
                res.Data[p++] = DotProd(rowCache, colCache);
                c += m;
            }
            r += m;
        }
    }

    public static float DotProd(Matrix2D v1, Matrix2D v2)
    {
        if (v1.NumRows != 1 || v2.NumRows != 1 || v1.NumCols != v2.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        float sum = 0f;
        for (int i = 0; i < v1.NumCols; i++)
            sum += v1.Data[i] * v2.Data[i];
        return sum;
    }

    public static void Transpose(Matrix2D a, Matrix2D res)
    {
        if (a.NumRows != res.NumCols || a.NumCols != res.NumRows)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows; i++)
            for (int j = 0; j < a.NumCols; j++)
                res.Data[j * a.NumRows + i] = a.Data[i * a.NumCols + j];
    }

    public static void RowAdd(Matrix2D a, Matrix2D row, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols ||
                a.NumCols != row.NumCols || row.NumRows != 1)
            throw new ArgumentException("Invalid matrix shapes!");

        int o = 0;
        for (int r = 0; r < a.NumRows; r++)
        {
            for (int c = 0; c < a.NumCols; c++)
                res.Data[o+c] = a.Data[o+c] + row.Data[c];
            o += a.NumCols;
        }
    }

    public static void ColMean(Matrix2D a, Matrix2D res)
    {
        if (a.NumCols != res.NumCols || res.NumRows != 1)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int c = 0; c < a.NumCols; c++)
        {
            float sum = 0;
            for (int r = 0; r < a.NumRows; r++)
                sum += a.Data[r * a.NumCols + c];
            res.Data[c] = sum / a.NumRows;
        }
    }

    public static void ElemAdd(Matrix2D a, Matrix2D b, Matrix2D res)
    {
        if (a.NumRows != b.NumRows || a.NumCols != b.NumCols ||
                a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] + b.Data[i];
    }

    public static void ElemSub(Matrix2D a, Matrix2D b, Matrix2D res)
    {
        if (a.NumRows != b.NumRows || a.NumCols != b.NumCols ||
                a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] - b.Data[i];
    }

    public static void ElemMul(Matrix2D a, Matrix2D b, Matrix2D res)
    {
        if (a.NumRows != b.NumRows || a.NumCols != b.NumCols ||
                a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] * b.Data[i];
    }

    public static void ElemDiv(Matrix2D a, Matrix2D b, Matrix2D res)
    {
        if (a.NumRows != b.NumRows || a.NumCols != b.NumCols ||
                a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] / b.Data[i];
    }

    public static void ElemMax(Matrix2D a, float comp, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] >= comp ? a.Data[i] : comp;
    }

    public static void ElemGeq(Matrix2D a, float comp, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] >= comp ? 1 : 0;
    }

    public static void ElemSqrt(Matrix2D a, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = (float)Math.Sqrt(a.Data[i]);
    }

    public static void BatchAdd(Matrix2D a, float b, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] + b;
    }

    public static void BatchSub(Matrix2D a, float b, Matrix2D res)
    {
        BatchAdd(a, -b, res);
    }

    public static void BatchMul(Matrix2D a, float b, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] * b;
    }

    public static void BatchDiv(Matrix2D a, float b, Matrix2D res)
    {
        BatchMul(a, 1 / b, res);
    }

    public static void ShuffleRows(Matrix2D a, int[] perm)
    {
        // TODO: implement logic
        throw new NotImplementedException();
    }
}

public enum MatmulFlags { NN, TN, NT, TT }

public static class Perm
{
    private static readonly Random rng = new Random();

    public static int[] Identity(int size)
        => Enumerable.Range(0, size).ToArray();

    public static void Permutate(int[] perm)
    {
        for (int i = 0; i < perm.Length - 1; i++)
        {
            int j = rng.Next(i, perm.Length);
            if (i == j) continue;
            var temp = perm[i];
            perm[i] = perm[j];
            perm[j] = temp;
        }
    }
}

public static class RandNormal
{
    private static readonly Random rng = new Random();

    public static double Next(double center, double std_dev)
    {
        const double EPSILON = 1.19e-07;
        const double TWO_PI = 2.0 * Math.PI;

        double u1, u2;
        do { u1 = rng.NextDouble(); }
        while (u1 <= EPSILON);
        u2 = rng.NextDouble();

        double mag = std_dev * Math.Sqrt(-2.0 * Math.Log(u1));
        if (rng.NextDouble() > 0.5)
            return mag * Math.Cos(TWO_PI * u2) + center;
        else
            return mag * Math.Sin(TWO_PI * u2) + center;
    }
}
