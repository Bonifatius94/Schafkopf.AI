namespace Schafkopf.Training;

public class FFModel
{
    public FFModel(IList<ILayer> layers)
    {
        Layers = layers;
        GradsTape = new List<Matrix2D>();
    }

    public int BatchSize { get; private set; }
    public IList<ILayer> Layers { get; private set; }
    public IList<Matrix2D> GradsTape { get; private set; }

    public void Compile(int batchSize, int inputDims)
    {
        foreach (var layer in Layers)
        {
            layer.Compile(inputDims);
            inputDims = layer.OutputDims;
        }

        RecompileCache(batchSize);
    }

    public void RecompileCache(int batchSize)
    {
        BatchSize = batchSize;
        GradsTape = new List<Matrix2D>();
        int inputDims = Layers.First().InputDims;
        var input = Matrix2D.Zeros(batchSize, inputDims);
        var deltaOut = Matrix2D.Zeros(batchSize, inputDims);

        foreach (var layer in Layers)
        {
            layer.CompileCache(input, deltaOut);
            GradsTape.Add(layer.Cache.Gradients);
            input = layer.Cache.Output;
            deltaOut = layer.Cache.DeltasIn;
        }
    }

    public void TrainBatch(Matrix2D x, Matrix2D y, ILoss lossFunc, IOptimizer opt)
    {
        var deltas = Layers.Last().Cache.DeltasIn;
        var pred = PredictBatch(x);
        lossFunc.LossDeltas(pred, y, deltas);
        FitBatch(deltas, opt);
    }

    public Matrix2D PredictBatch(Matrix2D input)
    {
        Matrix2D.CopyData(input, Layers.First().Cache.Input);
        foreach (var layer in Layers)
            layer.Forward();
        return Layers.Last().Cache.Output;
    }

    public void FitBatch(Matrix2D deltas, IOptimizer opt)
    {
        var modelDeltas = Layers.Last().Cache.DeltasIn;
        Matrix2D.CopyData(deltas, modelDeltas);

        foreach (var layer in Layers.Reverse())
            layer.Backward();

        opt.AdjustGrads(GradsTape);
        foreach (var layer in Layers)
            layer.ApplyGrads();
    }
}
