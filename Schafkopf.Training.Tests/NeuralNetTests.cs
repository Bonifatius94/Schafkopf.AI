namespace Schafkopf.Training.Tests;

public class MeanSquaredErrorTest
{
    [Fact]
    public void Test_CanComputeLoss()
    {
        var lossFunc = new MeanSquaredError();
        var pred = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
        var truthSame = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
        var truth1Dist = new Matrix2D(2, 3, new float[] { 1, 2, 3, 4, 5, 6 });

        float lossSameData = lossFunc.Loss(pred, truthSame);
        float lossDistData = lossFunc.Loss(pred, truth1Dist);

        Assert.Equal(0, lossSameData);
        Assert.Equal(3, lossDistData);
    }

    [Fact]
    public void Test_CanComputeDeltas()
    {
        var lossFunc = new MeanSquaredError();
        var pred = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
        var truth = new Matrix2D(2, 3, new float[] { 1, 2, 3, 4, 5, 6 });

        var deltas = new Matrix2D(2, 3);
        lossFunc.LossDeltas(pred, truth, deltas);

        var exp = new float[] { -0.5f, -0.5f, -0.5f, -0.5f, -0.5f, -0.5f };
        Assert.Equal(exp, deltas.Data);
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
        var input = new Matrix2D(batchSize, inputDims, new float[] { 0, 1, 2, 3, 4, 5 });
        var deltasOut = new Matrix2D(batchSize, inputDims);

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
        var input = new Matrix2D(batchSize, inputDims, new float[] { 0, 1, 2, 3, 4, 5 });
        var deltasOut = new Matrix2D(batchSize, inputDims);

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
        var input = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
        var weights = new Matrix2D(3, 1, new float[] { 0, 1, 2 });
        var biases = new Matrix2D(1, 1, new float[] { 1 });
        var deltasOut = new Matrix2D(2, 3);
        var dense = new DenseLayer(1);
        dense.Compile(3);
        dense.CompileCache(input, deltasOut);
        dense.Load(new Matrix2D[] { weights, biases });

        dense.Forward();
        var pred = dense.Cache.Output;

        var exp = new float[] { 5 + 1, 14 + 1 };
        Assert.Equal(exp, pred.Data);
    }

    [Fact]
    public void Test_CanProcessBackwardPass()
    {
        var input = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
        var yTrue = new Matrix2D(2, 1, new float[] { 7, 14 });
        var weights = new Matrix2D(3, 1, new float[] { 0, 1, 2 });
        var biases = new Matrix2D(1, 1, new float[] { 1 });
        var deltasOut = new Matrix2D(2, 3);
        var dense = new DenseLayer(1);
        dense.Compile(3);
        dense.CompileCache(input, deltasOut);
        dense.Load(new Matrix2D[] { weights, biases });
        var loss = new MeanSquaredError();

        dense.Forward();
        loss.LossDeltas(dense.Cache.Output, yTrue, dense.Cache.DeltasIn);
        dense.Backward();

        // input:   [[0, 1, 2],
        //           [3, 4, 5]]

        // input^T: [[0, 3],
        //           [1, 4],
        //           [2, 5]]

        // deltas:  [[-0.5],
        //           [ 0.5]]

        int bound = dense.InputDims * dense.OutputDims;
        var expWeightGrads = new float[] { 0, 1.5f, 1.5f };
        Assert.Equal(expWeightGrads, dense.Cache.Gradients.Data[..bound]);
        var expBiasGrads = new float[] { 0 };
        Assert.Equal(expBiasGrads, dense.Cache.Gradients.Data[bound..]);
        var expDeltasOut = new float[] { 0, -0.5f, -1, 0, 0.5f, 1 };
        Assert.Equal(expDeltasOut, dense.Cache.DeltasOut.Data);
    }

    // [Fact]
    // public void Test_CanApplyGradients()
    // {
    //     var input = new Matrix2D(2, 3, new float[] { 0, 1, 2, 3, 4, 5 });
    //     var yTrue = new Matrix2D(2, 1, new float[] { 7, 14 });
    //     var weights = new Matrix2D(3, 1, new float[] { 0, 1, 2 });
    //     var biases = new Matrix2D(1, 1, new float[] { 1 });
    //     var deltasOut = new Matrix2D(2, 3);
    //     var dense = new DenseLayer(1);
    //     dense.Compile(3);
    //     dense.CompileCache(input, deltasOut);
    //     dense.Load(new Matrix2D[] { weights, biases });
    //     var loss = new MeanSquaredError();

    //     dense.Forward();
    //     loss.LossDeltas(dense.Cache.Output, yTrue, dense.Cache.DeltasIn);
    //     dense.Backward();
    //     dense.ApplyGrads();

    //     var exp = new float[] { 0, -0.5f, -1, 0, 0.5f, 1 };
    //     Assert.Equal(exp, dense.Cache.DeltasOut.Data);
    // }
}
