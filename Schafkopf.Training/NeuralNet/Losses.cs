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
