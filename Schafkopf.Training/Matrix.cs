namespace Schafkopf.Training;

public unsafe class Matrix2D : IEquatable<Matrix2D>
{
    public static Matrix2D Zeros(int numRows, int numCols, bool hasCache = true)
        => FromData(numRows, numCols, new double[numRows * numCols], hasCache);

    public static Matrix2D RandNorm(int numRows, int numCols, double mu, double sig, bool hasCache = true)
        => FromData(numRows, numCols, hasCache: hasCache,
                data: Enumerable.Range(0, numRows * numCols)
                    .Select(i => RandNormal.Next(mu, sig)).ToArray());

    public static Matrix2D FromData(int numRows, int numCols, double[] data, bool hasCache = true)
    {
        var cache = hasCache ? new double[numRows * numCols] : new double[1];
        fixed (double* d = &data[0])
        fixed (double* c = &cache[0])
            return new Matrix2D(numRows, numCols, d, c);
    }

    public static Matrix2D FromRawPointers(int numRows, int numCols, double* data, double* cache)
        => new Matrix2D(numRows, numCols, data, cache);

    private Matrix2D(
        int numRows, int numCols,
        double* data, double* cache)
    {
        NumRows = numRows;
        NumCols = numCols;
        Data = data;
        Cache = cache;
    }

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

        var rowCache = new Matrix2D(l, m, a.Cache, null);
        var colCache = new Matrix2D(n, m, b.Cache, null);

        if (!a_normal)
            Transpose(a, rowCache);
        else
            rowCache.Data = a.Data;

        if (b_normal)
            Transpose(b, colCache);
        else
            colCache.Data = b.Data;

        rowCache.NumRows = 1;
        colCache.NumRows = 1;
        var rowData = rowCache.Data;
        var colData = colCache.Data;

        int r = 0, p = 0, c;
        for (int i = 0; i < l; i++)
        {
            c = 0;
            for (int j = 0; j < n; j++)
            {
                rowCache.Data = rowData + r;
                colCache.Data = colData + c;
                res.Data[p++] = DotProd(rowCache, colCache);
                c += m;
            }
            r += m;
        }
    }

    public static double DotProd(Matrix2D v1, Matrix2D v2)
    {
        if (v1.NumRows != 1 || v2.NumRows != 1 || v1.NumCols != v2.NumCols)
            throw new ArgumentException("Invalid matrix shapes!");

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
        {
            double sum = 0;
            for (int r = 0; r < a.NumRows; r++)
                sum += a.Data[r * a.NumCols + c];
            res.Data[c] = sum / a.NumRows;
        }
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
        // TODO: implement logic
        throw new NotImplementedException();
    }

    public bool Equals(Matrix2D? other)
    {
        if(other == null)
            return false;

        if (NumRows != other.NumRows || NumCols != other.NumCols)
            return false;

        for (int i = 0; i < NumRows * NumCols; i++)
            if (other.Data[i] != Data[i])
                return false;

        return true;
    }
}

public enum MatmulFlags { NN, TN, NT, TT }

public static class Perm
{
    private static readonly Random rng = new Random();

    public static int[] Identity(int size)
        => Enumerable.Range(0, size).ToArray();

    public static void Permutate(int[] perm)
    {
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
