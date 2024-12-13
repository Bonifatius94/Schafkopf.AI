namespace Schafkopf.Training;

public static class UniformDistribution
{
    private static Random rng = new Random();

    public static int Sample(this Span<double> probs, Random? rng = null)
    {
        rng = rng ?? UniformDistribution.rng;
        double p = rng.NextDouble();
        double sum = 0;
        for (int i = 0; i < probs.Length - 1; i++)
        {
            sum += probs[i];
            if (p < sum)
                return i;
        }
        return probs.Length - 1;
    }

    public static int Sample(this ReadOnlySpan<double> probs, Random? rng = null)
    {
        rng = rng ?? UniformDistribution.rng;
        double p = rng.NextDouble();
        double sum = 0;
        for (int i = 0; i < probs.Length - 1; i++)
        {
            sum += probs[i];
            if (p < sum)
                return i;
        }
        return probs.Length - 1;
    }

    public static int Sample(int numClasses, Random? rng)
        => (rng ?? UniformDistribution.rng).Next(0, numClasses);
}

public static class NormalDistribution
{
    private static Random rng = new Random();

    public static double Sample(
        (double, double) mu_sigma, Random? rng = null, double eps = 1.19e-07)
    {
        rng = rng ?? NormalDistribution.rng;
        (var mu, var sigma) = mu_sigma;
        return Sample(mu, sigma, rng, eps);
    }

    public static double Sample(
        double mu, double sigma, Random? rng = null, double eps = 1.19e-07)
    {
        const double TWO_PI = 2 * Math.PI;
        rng = rng ?? NormalDistribution.rng;

        double u1, u2;
        do { u1 = rng.NextDouble(); } while (u1 <= eps);
        u2 = rng.NextDouble();

        double mag = sigma * Math.Sqrt(-2 * Math.Min(Math.Log(u1 + 1e-8), 0));
        if (rng.NextDouble() > 0.5)
            return mag * Math.Cos(TWO_PI * u2) + mu;
        else
            return mag * Math.Sin(TWO_PI * u2) + mu;
    }
}
