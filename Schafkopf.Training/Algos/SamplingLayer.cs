using Schafkopf.Training;

namespace BackpropNet;

public interface ISampler : ILayer
{
    void Seed(int seed);
    Matrix2D FetchSelectionProbs();
}

public class UniformSamplingLayer : ISampler
{
    public UniformSamplingLayer(bool sparse = true, int seed = 0)
    {
        this.sparse = sparse;
        Seed(seed);
    }

    public LayerCache Cache { get; private set; }
    public int InputDims { get; private set; }
    public int OutputDims { get; private set; }

    private bool sparse;
    private Random Rng;
    private Matrix2D SelectionProbs;

    public void Compile(int inputDims)
    {
        InputDims = inputDims;
        OutputDims = sparse ? 1 : inputDims;
    }

    public void CompileCache(Matrix2D inputs, Matrix2D deltasOut)
    {
        if (InputDims != inputs.NumCols)
            throw new ArgumentException("Expected different amount of input dims!");

        int batchSize = inputs.NumRows;
        Cache = new LayerCache() {
            Input = inputs,
            Output = Matrix2D.Zeros(batchSize, OutputDims),
            DeltasIn = Matrix2D.Zeros(batchSize, OutputDims),
            DeltasOut = deltasOut,
            Gradients = Matrix2D.Null(),
        };

        SelectionProbs = Matrix2D.Zeros(batchSize, 1);
    }

    public void Seed(int seed)
        => Rng = new Random(seed);

    public Matrix2D FetchSelectionProbs()
        => SelectionProbs;

    public void Forward()
    {
        int batchSize = Cache.Input.NumRows;
        int numClasses = Cache.Input.NumCols;
        bool sparse = Cache.Output.NumCols != numClasses;

        var selProbs = SelectionProbs.SliceRowsRaw(0, batchSize);
        var output = Cache.Output.SliceRowsRaw(0, batchSize);
        int offset = 0;

        for (int i = 0; i < batchSize; i++)
        {
            var probDist = Cache.Input.SliceRowsRaw(i, 1);
            var idx = probDist.Sample(Rng);
            selProbs[i] = probDist[idx];
            if (sparse)
                output[offset++] = idx;
            else
                for (int j = 0; j < numClasses; j++)
                    output[offset++] = j == idx ? 1 : 0;
        }
    }

    public void Backward()
    {
        int batchSize = Cache.Input.NumRows;
        int numClasses = Cache.Input.NumCols;
        bool sparse = Cache.Output.NumCols != numClasses;

        var output = Cache.Output.SliceRowsRaw(0, batchSize);
        var deltasIn = Cache.DeltasIn.SliceRowsRaw(0, batchSize);
        var deltasOut = Cache.DeltasOut.SliceRowsRaw(0, batchSize);
        int offset = 0;

        if (sparse)
            for (int i = 0; i < batchSize; i++)
                for (int j = 0; j < numClasses; j++)
                    deltasOut[offset++] = output[i] == j ? deltasIn[i] : 0;
        else
            Matrix2D.CopyData(Cache.DeltasIn, Cache.DeltasOut);
    }

    public void ApplyGrads()
    {
        // info: layer isn't trainable
    }
}
