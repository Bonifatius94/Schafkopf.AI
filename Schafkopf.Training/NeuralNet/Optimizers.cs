namespace Schafkopf.Training;

public interface IOptimizer
{
    void AdjustGrads(IList<Matrix2D> grads);
    void Compile(IList<Matrix2D> grads);
}

public class NaiveSGDOpt : IOptimizer
{
    public NaiveSGDOpt(double learnRate)
    {
        this.learnRate = learnRate;
    }

    private double learnRate;

    public void AdjustGrads(IList<Matrix2D> grads)
    {
        foreach (var g in grads)
            if (g.IsNull)
                Matrix2D.BatchMul(g, learnRate, g);
    }

    public void Compile(IList<Matrix2D> grads)
    {
        // info: nothing to initialize
    }
}

public class AdamOpt : IOptimizer
{
    public AdamOpt(
        double learnRate, double beta1 = 0.9,
        double beta2 = 0.999, double epsilon = 1e-8)
    {
        this.learnRate = learnRate;
        this.beta1 = beta1;
        this.beta2 = beta2;
        this.epsilon = epsilon;
    }

    private double learnRate = 0.001;
    private double beta1 = 0.9;
    private double beta2 = 0.999;
    private double epsilon = 1e-8;

    private double beta1t = 1.0;
    private double beta2t = 1.0;
    private IList<Matrix2D> ms;
    private IList<Matrix2D> vs;
    private IList<Matrix2D> mTemps;
    private IList<Matrix2D> vTemps;

    public void Compile(IList<Matrix2D> allGrads)
    {
        beta1t = 1.0;
        beta2t = 1.0;
        ms = new List<Matrix2D>();
        vs = new List<Matrix2D>();
        mTemps = new List<Matrix2D>();
        vTemps = new List<Matrix2D>();

        foreach (var grads in allGrads)
        {
            var hasGrads = !grads.IsNull;
            ms.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols) : Matrix2D.Null());
            vs.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols) : Matrix2D.Null());
            mTemps.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols) : Matrix2D.Null());
            vTemps.Add(hasGrads ? Matrix2D.Zeros(grads.NumRows, grads.NumCols) : Matrix2D.Null());
        }
    }

    public void AdjustGrads(IList<Matrix2D> allGrads)
    {
        for (int i = 0; i < allGrads.Count; i++)
        {
            if (allGrads[i].IsNull)
                continue;

            var grads = allGrads[i];
            var m = ms[i];
            var v = vs[i];
            var mTemp = mTemps[i];
            var vTemp = vTemps[i];

            Matrix2D.BatchMul(m, beta1, m);
            Matrix2D.BatchMul(grads, 1 - beta1, mTemp);
            Matrix2D.ElemAdd(m, mTemp, m);

            Matrix2D.BatchMul(v, beta2, v);
            Matrix2D.ElemMul(grads, grads, vTemp);
            Matrix2D.BatchMul(vTemp, 1 - beta2, vTemp);
            Matrix2D.ElemAdd(v, vTemp, v);

            beta1t *= beta1;
            beta2t *= beta2;

            Matrix2D.BatchDiv(m, 1 - beta1t, mTemp);
            Matrix2D.BatchDiv(v, 1 - beta2t, vTemp);

            Matrix2D.BatchMul(mTemp, learnRate, mTemp);
            Matrix2D.ElemSqrt(vTemp, vTemp);
            Matrix2D.BatchAdd(vTemp, epsilon, vTemp);
            Matrix2D.ElemDiv(mTemp, vTemp, grads);
        }
    }
}
