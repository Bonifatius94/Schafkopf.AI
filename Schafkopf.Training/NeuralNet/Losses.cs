namespace Schafkopf.Training;

public interface ILoss
{
    double Loss(Matrix2D pred, Matrix2D target);
    void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas);
}

public class MeanSquaredError : ILoss
{
    public double Loss(Matrix2D pred, Matrix2D target)
    {
        double sum = 0;
        unsafe
        {
            for (int i = 0; i < pred.NumRows * pred.NumCols; i++)
            {
                double diff = pred.Data[i] - target.Data[i];
                sum += diff * diff;
            }
        }
        return sum / pred.NumRows;
    }

    public void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas)
    {
        Matrix2D.ElemSub(pred, target, deltas);
        Matrix2D.BatchDiv(deltas, pred.NumRows, deltas);
    }
}

public class CrossEntropyLoss : ILoss
{
    public double Loss(Matrix2D pred, Matrix2D target)
    {
        // info: assuming one-hot encoded labels

        Matrix2D cache;
        unsafe { cache = Matrix2D.FromRawPointers(
            pred.NumRows, pred.NumCols, pred.Cache, null); }

        Matrix2D.BatchAdd(pred, 1e-8, cache);
        Matrix2D.ElemLog(cache, cache);
        Matrix2D.ElemMul(target, cache, cache);
        return -1.0 * Matrix2D.ReduceMean(cache);
    }

    public void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas)
    {
        // info: assuming one-hot encoded labels
        Matrix2D.CopyData(target, deltas);
    }
}

public class SparseCrossEntropyLoss : ILoss
{
    public double Loss(Matrix2D pred, Matrix2D target)
    {
        double logSum = 0;
        unsafe
        {
            for (int r = 0; r < pred.NumRows; r++)
            {
                int label = (int)target.Data[r];
                double prob = pred.Data[r * pred.NumCols + label];
                logSum += Math.Log(prob + 1e-8);
            }
        }
        return logSum / (pred.NumRows * pred.NumCols);
    }

    public void LossDeltas(Matrix2D pred, Matrix2D target, Matrix2D deltas)
    {
        unsafe
        {
            for (int i = 0; i < deltas.NumRows * deltas.NumCols; i++)
                deltas.Data[i] = 0;

            for (int r = 0; r < deltas.NumRows; r++)
                deltas.Data[r * deltas.NumCols + (int)target.Data[r]] = 1;
        }
    }
}
