namespace Schafkopf.Training;

public class Program
{
    private static FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1),
        });

    public static void Main(string[] args)
    {
        var model = createModel();
        var dataset = SupervisedSchafkopfDataset.GenerateDataset(
            trainSize: 1_000_000, testSize: 10_000);
        var optimizer = new AdamOpt(learnRate: 0.002);
        var lossFunc = new MeanSquaredError();

        var session = new SupervisedTrainingSession(
            model, optimizer, lossFunc, dataset);
        Console.WriteLine("Training started!");
        Console.WriteLine($"loss before: loss={session.Eval()}");
        session.Train(5, true, (ep, l) => Console.WriteLine($"loss ep. {ep}: loss={l}"));
        Console.WriteLine("Training finished!");
    }
}
