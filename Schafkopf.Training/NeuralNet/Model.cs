namespace Schafkopf.Training;

public class FFModel
{
    public FFModel(IList<ILayer> layers)
    {
        Layers = layers;
        GradsTape = new List<Matrix2D>();
    }

    public IList<ILayer> Layers { get; private set; }
    public IList<Matrix2D> GradsTape { get; private set; }

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

    public Matrix2D PredictBatch(Matrix2D input)
    {
        Matrix2D.CopyData(input, Layers.First().Cache.Input);
        foreach (var layer in Layers)
            layer.Forward();
        return Layers.Last().Cache.Output;
    }

    public void TrainBatch(Matrix2D x, Matrix2D y, ILoss lossFunc, IOptimizer opt)
    {
        Matrix2D.CopyData(x, Layers.First().Cache.Input);
        foreach (var layer in Layers)
            layer.Forward();

        var lastLayerCache = Layers.Last().Cache;
        lossFunc.LossDeltas(lastLayerCache.Output, y, lastLayerCache.DeltasIn);

        foreach (var layer in Layers.Reverse())
            layer.Backward();

        opt.AdjustGrads(GradsTape);
        foreach (var layer in Layers)
            layer.ApplyGrads();
    }
}
