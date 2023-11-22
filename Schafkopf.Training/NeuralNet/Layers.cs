namespace Schafkopf.Training;

public class LayerCache
{
    public Matrix2D Input;
    public Matrix2D Output;
    public Matrix2D DeltasIn; // info: incoming backwards
    public Matrix2D DeltasOut;
    public Matrix2D Gradients;

    public void ApplyInput(Matrix2D input)
    {
        if (input.NumCols != Input.NumCols)
            throw new ArgumentException("Dimensions don't match!");
        if (input.NumRows > Input.NumRows)
            throw new ArgumentException("Cache size is insufficient!");

        unsafe { Input.Data = input.Data; }
    }
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
        Weights = Matrix2D.RandNorm(InputDims, OutputDims, 0.0, 0.1);
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
            Matrix2D.CopyData(trainParams[0], Weights);
            Matrix2D.CopyData(trainParams[1], Biases);
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
            Gradients = Matrix2D.Null()
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

public class SoftmaxLayer : ILayer
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
            Gradients = Matrix2D.Null()
        };
    }

    public void Forward()
    {
        Matrix2D.ElemClip(Cache.Input, -10, 10, Cache.Output);
        Matrix2D.ElemExp(Cache.Output, Cache.Output);

        Matrix2D expSums;
        unsafe { expSums = Matrix2D.FromRawPointers(
            Cache.Input.NumRows, 1, Cache.Input.Cache, null); }
        Matrix2D.RowSum(Cache.Output, expSums);

        Matrix2D.BatchAdd(expSums, 1e-8, expSums);
        Matrix2D.RowDiv(Cache.Output, expSums, Cache.Output);
    }

    public void Backward()
    {
        // info: expecting one-hot encoded labels as incoming deltas
        Matrix2D.ElemSub(Cache.Output, Cache.DeltasIn, Cache.DeltasOut);
    }

    public void ApplyGrads()
    {
        // info: layer has no trainable params
    }
}
