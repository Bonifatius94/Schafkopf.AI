using System.Runtime.InteropServices;
using System.Text;

namespace Schafkopf.Training;

public unsafe class Matrix2D : IEquatable<Matrix2D>
{
    public static Matrix2D Zeros(int numRows, int numCols, bool hasCache = true)
        => FromData(numRows, numCols, new double[numRows * numCols], hasCache);

    public static Matrix2D RandNorm(
            int numRows, int numCols, double mu, double sig, bool hasCache = true)
        => FromData(numRows, numCols, hasCache: hasCache,
                data: Enumerable.Range(0, numRows * numCols)
                    .Select(i => RandNormal.Next(mu, sig)).ToArray());

    public static Matrix2D FromData(
        int numRows, int numCols, ReadOnlySpan<double> data, bool hasCache = true)
    {
        int size = sizeof(double) * numRows * numCols;
        var newData = Marshal.AllocHGlobal(size);
        var newCache = hasCache ? Marshal.AllocHGlobal(size) : (IntPtr)null;

        var dataBuf = (double*)newData.ToPointer();
        for (int i = 0; i < numRows * numCols; i++)
            dataBuf[i] = data[i];

        return new Matrix2D(numRows, numCols, newData, newCache, true);
    }

    // info: memory needs to be from the unmanaged heap,
    //       otherwise the .NET runtime will invalidate the pointers
    public static Matrix2D FromRawPointers(
            int numRows, int numCols, double* data, double* cache)
        => new Matrix2D(numRows, numCols, (IntPtr)data, (IntPtr)cache, false);

    public static Matrix2D SliceRows(Matrix2D orig, int rowid, int length)
        => FromRawPointers(length, orig.NumCols, orig.Data + rowid * orig.NumCols, orig.Cache);

    private static readonly Matrix2D NULL = new Matrix2D(0, 0, (IntPtr)null, (IntPtr)null, false);
    public static Matrix2D Null() => NULL;

    public static void CopyData(Matrix2D src, Matrix2D dest)
    {
        if (src.NumRows != dest.NumRows || src.NumCols != dest.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < src.NumRows * src.NumCols; i++)
            dest.Data[i] = src.Data[i];
    }

    public double At(int row, int col)
        => Data[row * NumCols + col];

    private Matrix2D(
        int numRows, int numCols,
        IntPtr origData, IntPtr origCache, bool ownsData)
    {
        NumRows = numRows;
        NumCols = numCols;
        Data = (double*)origData.ToPointer();
        Cache = (double*)origCache.ToPointer();
        this.origData = origData;
        this.origCache = origCache;
        this.ownsData = ownsData;
    }

    ~Matrix2D()
    {
        if (!ownsData)
            return;

        Marshal.FreeHGlobal(origData);
        if (origCache.ToPointer() != null)
            Marshal.FreeHGlobal(origCache);
    }

    private IntPtr origData;
    private IntPtr origCache;
    private bool ownsData;

    public int NumRows;
    public int NumCols;
    public double* Data;
    public double* Cache;

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

        var rowCache = Matrix2D.FromRawPointers(l, m, a.Cache, null);
        var colCache = Matrix2D.FromRawPointers(n, m, b.Cache, null);
        if (!a_normal)
            Transpose(a, rowCache);
        if (b_normal)
            Transpose(b, colCache);

        rowCache.NumRows = 1;
        colCache.NumRows = 1;
        var rowData = !a_normal ? a.Cache : a.Data;
        var colData = b_normal ? b.Cache : b.Data;

        int p = 0;
        rowCache.Data = rowData;
        for (int i = 0; i < l; i++)
        {
            colCache.Data = colData;
            for (int j = 0; j < n; j++)
            {
                res.Data[p++] = DotProd(rowCache, colCache);
                colCache.Data += m;
            }
            rowCache.Data += m;
        }
    }

    public static double DotProd(Matrix2D v1, Matrix2D v2)
    {
        if (v1.NumRows != 1 || v2.NumRows != 1 || v1.NumCols != v2.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        // TODO: optimize with SIMD
        double sum = 0;
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
            res.Data[c] = 0;

        int p = 0;
        for (int r = 0; r < a.NumRows; r++)
            for (int c = 0; c < a.NumCols; c++)
                res.Data[c] += a.Data[p++];

        for (int c = 0; c < a.NumCols; c++)
            res.Data[c] /= a.NumRows;
    }

    public static void RowSum(Matrix2D a, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || res.NumCols != 1)
            throw new ArgumentException("Invalid matrix shapes!");

        int o = 0;
        for (int r = 0; r < a.NumRows; r++)
        {
            res.Data[r] = 0;
            for (int c = 0; c < a.NumCols; c++)
                res.Data[r] += a.Data[o+c];
            o += a.NumCols;
        }
    }

    public static void RowDiv(Matrix2D a, Matrix2D col, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols ||
                col.NumRows != a.NumRows || col.NumCols != 1)
            throw new ArgumentException("Invalid matrix shapes!");

        int p = 0;
        for (int r = 0; r < a.NumRows; r++)
        {
            for (int c = 0; c < a.NumCols; c++)
            {
                res.Data[p] = a.Data[p] / col.Data[r];
                p++;
            }
        }
    }

    public static void RowArgmax(Matrix2D a, Matrix2D res)
    {
        if (res.NumRows != a.NumRows || res.NumCols != 1)
            throw new ArgumentException("Invalid matrix shapes!");

        int p = 0;
        for (int r = 0; r < a.NumRows; r++)
        {
            int argmax = 0;
            double max = a.Data[p++];
            for (int c = 1; c < a.NumCols; c++)
            {
                if (a.Data[p] > max)
                {
                    max = a.Data[p];
                    argmax = c;
                }
                p++;
            }
            res.Data[r] = argmax;
        }
    }

    public static double ReduceMean(Matrix2D a)
    {
        double sum = 0;
        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            sum += a.Data[i];
        return sum / (a.NumRows * a.NumCols);
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

    public static void ElemMax(Matrix2D a, double comp, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] >= comp ? a.Data[i] : comp;
    }

    public static void ElemMin(Matrix2D a, double comp, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] <= comp ? a.Data[i] : comp;
    }

    public static void ElemClip(Matrix2D a, double min, double max, Matrix2D res)
    {
        Matrix2D.ElemMax(a, min, res);
        Matrix2D.ElemMin(res, max, a);
    }

    public static void ElemGeq(Matrix2D a, double comp, Matrix2D res)
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
            res.Data[i] = (double)Math.Sqrt(a.Data[i]);
    }

    public static void ElemExp(Matrix2D a, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = (double)Math.Exp(a.Data[i]);
    }

    public static void ElemLog(Matrix2D a, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = (double)Math.Log(a.Data[i]);
    }

    public static void BatchAdd(Matrix2D a, double b, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] + b;
    }

    public static void BatchSub(Matrix2D a, double b, Matrix2D res)
    {
        BatchAdd(a, -b, res);
    }

    public static void BatchMul(Matrix2D a, double b, Matrix2D res)
    {
        if (a.NumRows != res.NumRows || a.NumCols != res.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            res.Data[i] = a.Data[i] * b;
    }

    public static void BatchDiv(Matrix2D a, double b, Matrix2D res)
    {
        BatchMul(a, 1 / b, res);
    }

    public static void ShuffleRows(Matrix2D a, int[] perm)
    {
        for (int i = 0; i < a.NumRows * a.NumCols; i++)
            a.Cache[i] = a.Data[i];

        for (int i = 0; i < a.NumRows; i++)
            for (int j = 0; j < a.NumCols; j++)
                a.Data[i * a.NumCols + j] = a.Cache[perm[i] * a.NumCols + j];
    }

    public bool IsNull => Data == null;

    public bool Equals(Matrix2D? other)
    {
        if (other == null || other.IsNull)
            return IsNull;
        else if (IsNull)
            return false;

        if (NumRows != other.NumRows || NumCols != other.NumCols)
            return false;

        if (NumRows <= 0 || NumCols <= 0 ||
                other.NumRows <= 0 || other.NumCols <= 0)
            return false;

        for (int i = 0; i < NumRows * NumCols; i++)
            if (other.Data[i] != Data[i])
                return false;

        return true;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        int p = 0;
        for (int row = 0; row < NumRows; row++)
        {
            for (int col = 0; col < NumCols; col++)
                builder.Append($"{ Data[p++] } ");

            if (row < NumRows - 1)
                builder.Append("\n");
        }

        return builder.ToString();
    }
}

public enum MatmulFlags { NN, TN, NT, TT }

public static class Perm
{
    private static readonly Random globalRng = new Random();

    public static int[] Identity(int size)
        => Enumerable.Range(0, size).ToArray();

    public static void Permutate(int[] perm, int? seed = null)
    {
        var rng = seed != null ? new Random(seed.Value) : globalRng;
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

    public static double Next(double center, double std_dev, double eps = 1.19e-07)
    {
        const double TWO_PI = 2 * Math.PI;

        double u1, u2;
        do { u1 = rng.NextDouble(); }
        while (u1 <= eps);
        u2 = rng.NextDouble();

        double mag = std_dev * Math.Sqrt(-2 * Math.Min(Math.Log(u1 + 1e-8), 0));
        if (rng.NextDouble() > 0.5)
            return mag * Math.Cos(TWO_PI * u2) + center;
        else
            return mag * Math.Sin(TWO_PI * u2) + center;
    }
}
