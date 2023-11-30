namespace Schafkopf.Training;

public class Program
{
    public static void Main(string[] args)
    {
        var config = new PPOTrainingSettings();
        var session = new PPOTrainingSession();
        session.Train(config);
    }
}
