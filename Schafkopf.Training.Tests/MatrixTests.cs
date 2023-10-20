namespace Schafkopf.Training.Tests;

public class MatrixTests
{
    [Fact]
    public void Test_CanInitFromData()
    {
        var data = new double[] {
            0, 1, 2,
            3, 4, 5
        };
        var a = Matrix2D.FromData(2, 3, data);

        unsafe
        {
            fixed (double* p = &data[0])
                for (int i = 0; i < 6; i++)
                    Assert.Equal(a.Data[i], p[i]);
        }
    }

    [Fact]
    public void Test_CanMatmulNN()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(3, 4, new double[] {
            0,  1,  2,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
        });
        var res = Matrix2D.Zeros(2, 4);

        Matrix2D.Matmul(a, b, res);

        var exp = Matrix2D.FromData(2, 4, new double[] {
            20, 23, 26, 29,
            56, 68, 80, 92
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanMatmulNT()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(4, 3, new double[] {
            0,  4,  8,
            1,  5,  9,
            2,  6, 10,
            3,  7, 11
        });
        var res = Matrix2D.Zeros(2, 4);

        Matrix2D.Matmul(a, b, res, MatmulFlags.NT);

        var exp = Matrix2D.FromData(2, 4, new double[] {
            20, 23, 26, 29,
            56, 68, 80, 92
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanMatmulTN()
    {
        var a = Matrix2D.FromData(3, 2, new double[] {
            0, 3,
            1, 4,
            2, 5
        });
        var b = Matrix2D.FromData(3, 4, new double[] {
            0,  1,  2,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
        });
        var res = Matrix2D.Zeros(2, 4);

        Matrix2D.Matmul(a, b, res, MatmulFlags.TN);

        var exp = Matrix2D.FromData(2, 4, new double[] {
            20, 23, 26, 29,
            56, 68, 80, 92
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanMatmulTT()
    {
        var a = Matrix2D.FromData(3, 2, new double[] {
            0, 3,
            1, 4,
            2, 5
        });
        var b = Matrix2D.FromData(4, 3, new double[] {
            0,  4,  8,
            1,  5,  9,
            2,  6, 10,
            3,  7, 11
        });
        var res = Matrix2D.Zeros(2, 4);

        Matrix2D.Matmul(a, b, res, MatmulFlags.TT);

        var exp = Matrix2D.FromData(2, 4, new double[] {
            20, 23, 26, 29,
            56, 68, 80, 92
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanRowAdd()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(1, 3, new double[] {
            0, 1, 2
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.RowAdd(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            3, 5, 7
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanColMean()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            4, 3, 2
        });
        var res = Matrix2D.Zeros(1, 3);

        Matrix2D.ColMean(a, res);

        var exp = Matrix2D.FromData(1, 3, new double[] {
            2, 2, 2
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanRowSum()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 1);

        Matrix2D.RowSum(a, res);

        var exp = Matrix2D.FromData(2, 1, new double[] { 3, 12 });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanRowDiv()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            6, 8, 10
        });
        var col = Matrix2D.FromData(2, 1, new double[] { 1, 2 });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.RowDiv(a, col, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    #region Elementwise

    [Fact]
    public void Test_CanElemAdd()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new double[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemAdd(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            1, 3, 5,
            7, 9, 11
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemSub()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new double[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemSub(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            -1, -1, -1,
            -1, -1, -1
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemMul()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new double[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemMul(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
             0,  2,  6,
            12, 20, 30
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemDiv()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            6, 8, 10
        });
        var b = Matrix2D.FromData(2, 3, new double[] {
            2, 2, 2,
            2, 2, 2
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemDiv(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemMax()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemMax(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            2, 2, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemMin()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemMin(a, 3, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 3, 3
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemGeq()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemGeq(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 0, 1,
            1, 1, 1
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemSqrt()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 4,
            9, 16, 25
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemSqrt(a, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemExp()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 0,
            1, 0, 1
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemExp(a, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            1, Math.E, 1,
            Math.E, 1, Math.E
        });
        Assert.Equal(exp, res);
    }

    #endregion Elementwise

    #region Batched

    [Fact]
    public void Test_CanBatchAdd()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchAdd(a, 1, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            1, 2, 3,
            4, 5, 6
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchSub()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchSub(a, 1, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchMul()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchMul(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            6, 8, 10
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchDiv()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            6, 8, 10
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchDiv(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new double[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    #endregion Batched
}

public class RandNormalTest
{
    [Fact]
    public void Test_CanGenerateNormalDistribution()
    {
        const int sampleSize = 100_000;
        const double expMu = 1.0, expSigma = 0.1;
        Func<double> next = () => RandNormal.Next(expMu, expSigma);
        int failures = 0;

        for (int i = 0; i < 100; i++)
        {
            double p = 1.0 / sampleSize;
            var data = Enumerable.Range(0, sampleSize).Select(i => next()).ToArray();
            double mu = data.Select(x => x * p).Sum();
            double variance = data.Select(x => mu - x).Select(x => x * x * p).Sum();
            double sigma = Math.Sqrt(variance);

            bool isValidDist = Math.Abs(expMu - mu) < 1e-3
                && Math.Abs(expSigma - sigma) < 1e-3;
            failures += isValidDist ? 0 : 1;
        }

        Assert.True(failures <= 3);
    }
}

public class MatrixPrintTest
{
    [Fact]
    public void Test_CanConvertToText()
    {
        var a = Matrix2D.FromData(2, 3, new double[] {
            0, 2, 4,
            6, 8, 10
        });

        var text = a.ToString();

        var exp = "0 2 4 \n6 8 10 ";
        Assert.Equal(exp, text);
    }
}

public class MatrixPointerImmutabilityTests
{
    private unsafe IList<(long, long)> dumpPointers(IList<Matrix2D> origMatrices)
        => origMatrices.Select(m => ((long)m.Data, (long)m.Cache)).ToArray();

    private unsafe bool validatePointers(
            IList<Matrix2D> matrices, IList<(long, long)> dumpedPointers)
        => matrices.Zip(dumpedPointers)
            .All(x => (long)x.First.Data == x.Second.Item1
                && (long)x.First.Cache == x.Second.Item2);

    [Fact]
    public void Test_IsMatmulImmatable()
    {
        var a = Matrix2D.RandNorm(64, 10, 0, 0.1);
        var b = Matrix2D.RandNorm(10, 64, 0, 0.1);
        var res = Matrix2D.Zeros(64, 64);
        var p = dumpPointers(new Matrix2D[] { a, b, res });

        Matrix2D.Matmul(a, b, res);

        Assert.True(validatePointers(new Matrix2D[] { a, b, res }, p));
    }
}
