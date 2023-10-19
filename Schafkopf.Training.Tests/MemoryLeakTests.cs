namespace Schafkopf.Training.Tests;

public class MemoryLeakTests
{
    [Fact]
    public void Test_CanTrainOnBatchWithoutLeaks()
    {
        var trainBatch = Matrix2D.RandNorm(64, 10, 0.0, 0.1);
        var model = new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1),
        });

        model.Compile(64, 10);
        for (int i = 0; i < 10_000; i++)
            model.PredictBatch(trainBatch);

        Assert.True(true);
    }

    [Fact]
    public void Test_CanMatMulWithoutLeaks()
    {
        var a = Matrix2D.RandNorm(64, 10, 0.0, 0.1);
        var b = Matrix2D.RandNorm(10, 64, 0.0, 0.1);
        var res = Matrix2D.Zeros(64, 64);

        for (int i = 0; i < 10_000; i++)
            Matrix2D.Matmul(a, b, res);

        Assert.True(true);
    }

    [Fact]
    public void Test_CanTransposeWithoutLeaks()
    {
        var a = Matrix2D.RandNorm(64, 64, 0.0, 0.1);

        unsafe
        {
            var res = Matrix2D.FromRawPointers(64, 64, a.Cache, null);
            for (int i = 0; i < 10_000; i++)
                Matrix2D.Transpose(a, res);
        }

        Assert.True(true);
    }

    [Fact]
    public void Test_CanDotProdWithoutLeaks()
    {
        var a = Matrix2D.RandNorm(1, 64, 0.0, 0.1);
        var b = Matrix2D.RandNorm(1, 64, 0.0, 0.1);

        for (int i = 0; i < 10_000; i++)
            Matrix2D.DotProd(a, b);

        Assert.True(true);
    }
}
