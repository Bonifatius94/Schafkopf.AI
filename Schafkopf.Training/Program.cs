namespace Schafkopf.Training;

public class Program
{
    public static void Main(string[] args)
    {
        var config = new PPOTrainingSettings();
        var session = new SchafkopfPPOTrainingSession();
        session.Train(config);
    }
}
