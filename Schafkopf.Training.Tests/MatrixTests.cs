namespace Schafkopf.Training.Tests;

public class MatrixTests
{
    [Fact]
    public void Test_CanInitFromData()
    {
        var data = new float[] {
            0, 1, 2,
            3, 4, 5
        };
        var a = Matrix2D.FromData(2, 3, data);

        unsafe
        {
            fixed (float* p = &data[0])
                for (int i = 0; i < 6; i++)
                    Assert.Equal(a.Data[i], p[i]);
        }
    }

    [Fact]
    public void Test_CanMatmul()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
                0, 1, 2,
                3, 4, 5
            },
            true);
        var b = Matrix2D.FromData(3, 4, new float[] {
            0,  1,  2,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
        });
        var res = Matrix2D.Zeros(2, 4);

        Matrix2D.Matmul(a, b, res);

        var exp = Matrix2D.FromData(2, 4, new float[] {
            20, 23, 26, 29,
            56, 68, 80, 92
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanRowAdd()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(1, 3, new float[] {
            0, 1, 2
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.RowAdd(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 2, 4,
            3, 5, 7
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanColMean()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            4, 3, 2
        });
        var res = Matrix2D.Zeros(1, 3);

        Matrix2D.ColMean(a, res);

        var exp = Matrix2D.FromData(1, 3, new float[] {
            2, 2, 2
        });
        Assert.Equal(exp, res);
    }

    #region Elementwise

    [Fact]
    public void Test_CanElemAdd()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new float[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemAdd(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            1, 3, 5,
            7, 9, 11
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemSub()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new float[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemSub(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            -1, -1, -1,
            -1, -1, -1
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemMul()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var b = Matrix2D.FromData(2, 3, new float[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemMul(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
             0,  2,  6,
            12, 20, 30
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemDiv()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 2, 4,
            6, 8, 10
        });
        var b = Matrix2D.FromData(2, 3, new float[] {
            2, 2, 2,
            2, 2, 2
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemDiv(a, b, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemMax()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemMax(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            2, 2, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemGeq()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemGeq(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 0, 1,
            1, 1, 1
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanElemSqrt()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 4,
            9, 16, 25
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.ElemSqrt(a, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    #endregion Elementwise

    #region Batched

    [Fact]
    public void Test_CanBatchAdd()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchAdd(a, 1, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            1, 2, 3,
            4, 5, 6
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchSub()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            1, 2, 3,
            4, 5, 6
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchSub(a, 1, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchMul()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchMul(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 2, 4,
            6, 8, 10
        });
        Assert.Equal(exp, res);
    }

    [Fact]
    public void Test_CanBatchDiv()
    {
        var a = Matrix2D.FromData(2, 3, new float[] {
            0, 2, 4,
            6, 8, 10
        });
        var res = Matrix2D.Zeros(2, 3);

        Matrix2D.BatchDiv(a, 2, res);

        var exp = Matrix2D.FromData(2, 3, new float[] {
            0, 1, 2,
            3, 4, 5
        });
        Assert.Equal(exp, res);
    }

    #endregion Batched
}
