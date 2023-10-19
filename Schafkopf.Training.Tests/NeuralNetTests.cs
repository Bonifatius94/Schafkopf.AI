namespace Schafkopf.Training.Tests;

public class MeanSquaredErrorTest
{
    [Fact]
    public void Test_CanComputeLoss()
    {
        var lossFunc = new MeanSquaredError();
        var pred = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var truthSame = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var truth1Dist = Matrix2D.FromData(2, 3, new double[] { 1, 2, 3, 4, 5, 6 });

        double lossSameData = lossFunc.Loss(pred, truthSame);
        double lossDistData = lossFunc.Loss(pred, truth1Dist);

        Assert.Equal(0, lossSameData);
        Assert.Equal(3, lossDistData);
    }

    [Fact]
    public void Test_CanComputeDeltas()
    {
        var lossFunc = new MeanSquaredError();
        var pred = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var truth = Matrix2D.FromData(2, 3, new double[] { 1, 2, 3, 4, 5, 6 });

        var deltas = Matrix2D.Zeros(2, 3);
        lossFunc.LossDeltas(pred, truth, deltas);

        var exp = Matrix2D.FromData(2, 3,
            new double[] { -0.5, -0.5, -0.5, -0.5, -0.5, -0.5 });
        Assert.Equal(exp, deltas);
    }
}

public class DenseLayerTests
{
    [Fact]
    public void Test_CanInitWeightsAndBiases()
    {
        const int batchSize = 2;
        const int inputDims = 3;
        const int outputDims = 1;
        var input = Matrix2D.FromData(batchSize, inputDims, new double[] { 0, 1, 2, 3, 4, 5 });
        var deltasOut = Matrix2D.Zeros(batchSize, inputDims);

        var dense = new DenseLayer(outputDims);
        dense.Compile(inputDims);
        dense.CompileCache(input, deltasOut);

        Assert.Equal(inputDims, dense.Weights.NumRows);
        Assert.Equal(outputDims, dense.Weights.NumCols);
        Assert.Equal(1, dense.Biases.NumRows);
        Assert.Equal(outputDims, dense.Biases.NumCols);
    }

    [Fact]
    public void Test_CanInitLayerCache()
    {
        const int batchSize = 2;
        const int inputDims = 3;
        const int outputDims = 1;
        var input = Matrix2D.FromData(batchSize, inputDims, new double[] { 0, 1, 2, 3, 4, 5 });
        var deltasOut = Matrix2D.Zeros(batchSize, inputDims);

        var dense = new DenseLayer(outputDims);
        dense.Compile(inputDims);
        dense.CompileCache(input, deltasOut);

        Assert.Equal(batchSize, dense.Cache.Input.NumRows);
        Assert.Equal(inputDims, dense.Cache.Input.NumCols);
        Assert.Equal(batchSize, dense.Cache.Output.NumRows);
        Assert.Equal(outputDims, dense.Cache.Output.NumCols);
        Assert.Equal(batchSize, dense.Cache.DeltasIn.NumRows);
        Assert.Equal(outputDims, dense.Cache.DeltasIn.NumCols);
        Assert.Equal(batchSize, dense.Cache.DeltasOut.NumRows);
        Assert.Equal(inputDims, dense.Cache.DeltasOut.NumCols);
        Assert.Equal(1, dense.Cache.Gradients.NumRows);
        Assert.Equal((inputDims + 1) * outputDims, dense.Cache.Gradients.NumCols);
    }

    [Fact]
    public void Test_CanProcessForwardPass()
    {
        var input = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var weights = Matrix2D.FromData(3, 1, new double[] { 0, 1, 2 });
        var biases = Matrix2D.FromData(1, 1, new double[] { 1 });
        var deltasOut = Matrix2D.Zeros(2, 3);
        var dense = new DenseLayer(1);
        dense.Compile(3);
        dense.CompileCache(input, deltasOut);
        dense.Load(new Matrix2D[] { weights, biases });

        dense.Forward();
        var pred = dense.Cache.Output;

        var exp = Matrix2D.FromData(2, 1, new double[] { 5 + 1, 14 + 1 });
        Assert.Equal(exp, pred);
    }

    [Fact]
    public void Test_CanProcessBackwardPass()
    {
        var input = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var yTrue = Matrix2D.FromData(2, 1, new double[] { 7, 14 });
        var weights = Matrix2D.FromData(3, 1, new double[] { 0, 1, 2 });
        var biases = Matrix2D.FromData(1, 1, new double[] { 1 });
        var deltasOut = Matrix2D.Zeros(2, 3);
        var dense = new DenseLayer(1);
        dense.Compile(3);
        dense.CompileCache(input, deltasOut);
        dense.Load(new Matrix2D[] { weights, biases });
        var loss = new MeanSquaredError();

        dense.Forward();
        loss.LossDeltas(dense.Cache.Output, yTrue, dense.Cache.DeltasIn);
        dense.Backward();

        // input^T: [[0, 3],
        //           [1, 4],
        //           [2, 5]]

        // deltas:  [[-0.5],
        //           [ 0.5]]

        var expWeightGrads = Matrix2D.FromData(3, 1, new double[] { 1.5, 1.5, 1.5 });
        Assert.Equal(expWeightGrads, dense.WeightGrads);
        var expBiasGrads = Matrix2D.FromData(1, 1, new double[] { 0 });
        Assert.Equal(expBiasGrads, dense.BiasGrads);
        var expDeltasOut = Matrix2D.FromData(2, 3, new double[] { 0, -0.5, -1, 0, 0.5, 1 });
        Assert.Equal(expDeltasOut, dense.Cache.DeltasOut);
    }

    [Fact]
    public void Test_CanOptimizePrediction()
    {
        var input = Matrix2D.FromData(2, 3, new double[] { 0, 1, 2, 3, 4, 5 });
        var yTrue = Matrix2D.FromData(2, 1, new double[] { 7, 14 });
        var weights = Matrix2D.FromData(3, 1, new double[] { 0, 1, 2 });
        var biases = Matrix2D.FromData(1, 1, new double[] { 1 });
        var deltasOut = Matrix2D.Zeros(2, 3);
        var dense = new DenseLayer(1);
        dense.Compile(3);
        dense.CompileCache(input, deltasOut);
        dense.Load(new Matrix2D[] { weights, biases });
        var lossFunc = new MeanSquaredError();
        var opt = new AdamOpt(0.01);
        var modelGrads = new Matrix2D[] { dense.Cache.Gradients };
        opt.Compile(modelGrads);

        for (int i = 0; i < 1000; i++)
        {
            dense.Forward();
            lossFunc.LossDeltas(dense.Cache.Output, yTrue, dense.Cache.DeltasIn);
            dense.Backward();
            opt.AdjustGrads(modelGrads);
            dense.ApplyGrads();
        }

        double loss = lossFunc.Loss(dense.Cache.Output, yTrue);
        Assert.True(loss < 0.01);
    }
}

public class ReLULayerTests
{
    [Fact]
    public void Test_CanInitLayerCache()
    {
        const int batchSize = 2; const int dims = 3;
        var input = Matrix2D.FromData(batchSize, dims, new double[] { 0, 1, 2, 3, 4, 5 });
        var deltasOut = Matrix2D.Zeros(batchSize, dims);

        var relu = new ReLULayer();
        relu.Compile(3);
        relu.CompileCache(input, deltasOut);

        Assert.Equal(batchSize, relu.Cache.Input.NumRows);
        Assert.Equal(dims, relu.Cache.Input.NumCols);
        Assert.Equal(batchSize, relu.Cache.Output.NumRows);
        Assert.Equal(dims, relu.Cache.Output.NumCols);
        Assert.Equal(batchSize, relu.Cache.DeltasIn.NumRows);
        Assert.Equal(dims, relu.Cache.DeltasIn.NumCols);
        Assert.Equal(batchSize, relu.Cache.DeltasOut.NumRows);
        Assert.Equal(dims, relu.Cache.DeltasOut.NumCols);
        Assert.Equal(Matrix2D.Null(), relu.Cache.Gradients);
    }

    [Fact]
    public void Test_CanProcessForwardPass()
    {
        var input = Matrix2D.FromData(2, 3, new double[] { -2, -1, 0, 1, 2, 3 });
        var deltasOut = Matrix2D.Zeros(2, 3);
        var relu = new ReLULayer();
        relu.Compile(3);
        relu.CompileCache(input, deltasOut);

        relu.Forward();

        var expOutput = Matrix2D.FromData(2, 3, new double[] { 0, 0, 0, 1, 2, 3 });
        Assert.Equal(expOutput, relu.Cache.Output);
    }

    [Fact]
    public void Test_CanProcessBackwardPass()
    {
        var input = Matrix2D.FromData(2, 3, new double[] { -2, -1, 0, 1, 2, 3 });
        var deltasIn = Matrix2D.FromData(2, 3, new double[] { 1, 1, 1, 1, 1, 1 });
        var deltasOut = Matrix2D.Zeros(2, 3);
        var relu = new ReLULayer();
        relu.Compile(3);
        relu.CompileCache(input, deltasOut);

        relu.Forward();
        relu.Cache.DeltasIn = deltasIn;
        relu.Backward();

        var expDeltasOut = Matrix2D.FromData(2, 3, new double[] { 0, 0, 1, 1, 1, 1 });
        Assert.Equal(expDeltasOut, relu.Cache.DeltasOut);
    }
}

public class MultiLayerNetTests
{
    [Fact]
    public void Test_CanCompileNet()
    {
        const int batchSize = 32; const int inputDims = 10; const int outputDims = 1;
        var layers = new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(outputDims)
        };
        var model = new FFModel(layers);

        model.Compile(batchSize, inputDims);

        Assert.Equal(batchSize, layers[0].Cache.Input.NumRows);
        Assert.Equal(inputDims, layers[0].Cache.Input.NumCols);
        Assert.Equal(batchSize, layers[2].Cache.Output.NumRows);
        Assert.Equal(outputDims, layers[2].Cache.Output.NumCols);
    }

    [Fact]
    public void Test_CanComputeForwardPass()
    {
        var inputs = Matrix2D.RandNorm(4, 4, 0, 0.1);
        var weights = Matrix2D.FromData(4, 4, new double[] {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        });
        var biases = Matrix2D.Zeros(1, 4);
        var dense1 = new DenseLayer(4);
        var dense2 = new DenseLayer(4);
        var model = new FFModel(new ILayer[] { dense1, dense2 });
        model.Compile(4, 4);
        dense1.Load(new Matrix2D[] { weights, biases });
        dense2.Load(new Matrix2D[] { weights, biases });

        var pred = model.PredictBatch(inputs);

        Assert.Equal(inputs, pred);
    }
}
