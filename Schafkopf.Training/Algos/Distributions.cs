namespace Schafkopf.Training;

public class UniformDistribution
{
    public UniformDistribution(int? seed = null)
        => rng = seed != null ? new Random(seed.Value) : new Random();

    private Random rng;

    public int Sample(ReadOnlySpan<double> probs)
    {
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

    public int Sample(int numClasses) => rng.Next(0, numClasses);
}
