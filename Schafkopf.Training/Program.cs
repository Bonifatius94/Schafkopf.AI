namespace Schafkopf.Training;

public class Program
{
    private static FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(200),
            new ReLULayer(),
            new DenseLayer(200),
            new ReLULayer(),
            new DenseLayer(10),
            new SoftmaxLayer(),
        });

    public static void Main(string[] args)
    {
        var model = createModel();
        var dataset = MnistDataset.LoadMnist();
        var optimizer = new AdamOpt(learnRate: 0.002);
        var lossFunc = new CrossEntropyLoss();
        var accMetric = new CategoricalAccuracy();

        var session = new SupervisedTrainingSession(
            model, optimizer, lossFunc, dataset);
        Console.WriteLine("Training started!");
        Console.WriteLine($"loss before: loss={session.Eval()}, acc={accMetric.Eval(model, dataset)}");
        session.Train(5, true, (ep, l) => Console.WriteLine(
            $"loss ep. {ep}: loss={l}, acc={accMetric.Eval(model, dataset)}"));
        Console.WriteLine("Training finished!");
    }
}

class MnistDataset
{
    #region Constants

    private const string mnistFolder = "mnist_data";
    private const string mnistBaseUrl
        = "https://github.com/apaz-cli/MNIST-dataloader-for-C/raw/master/data/";
    private static readonly List<string> mnistFiles = new List<string>() {
        "train-images.idx3-ubyte", "train-labels.idx1-ubyte",
        "t10k-images.idx3-ubyte", "t10k-labels.idx1-ubyte"
    };

    #endregion Constants

    public static FlatFeatureDataset LoadMnist()
    {
        var minstDir = Path.Combine(Environment.CurrentDirectory, mnistFolder);
        if (!Path.Exists(minstDir))
        {
            Directory.CreateDirectory(minstDir);
            var downloadFunc = (string file)
                => Task.Run(() => downloadFile(mnistBaseUrl, file, minstDir));
            var downloadTasks = mnistFiles.Select(f => downloadFunc(f)).ToArray();
            Task.WaitAll(downloadTasks);
        }

        return new FlatFeatureDataset(
            loadImages(Path.Combine(minstDir, mnistFiles[0])),
            loadLabels(Path.Combine(minstDir, mnistFiles[1])),
            loadImages(Path.Combine(minstDir, mnistFiles[2])),
            loadLabels(Path.Combine(minstDir, mnistFiles[3])));
    }

    private static async Task downloadFile(string baseUrl, string file, string targetDir)
    {
        using (var client = new HttpClient())
        {
            var bytes = await client.GetByteArrayAsync($"{baseUrl}/{file}");
            string outFile = Path.Combine(targetDir, file);
            using (var stream = File.Open(outFile, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
                writer.Write(bytes);
        }
    }

    private static Matrix2D loadImages(string file)
    {
        using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            int magicNumber = (int)revEnd((uint)reader.ReadInt32());
            int numImages = (int)revEnd((uint)reader.ReadInt32());
            int imageHeight = (int)revEnd((uint)reader.ReadInt32());
            int imageWidth = (int)revEnd((uint)reader.ReadInt32());

            int imgLen = imageHeight * imageWidth;
            var imgData = Matrix2D.Zeros(numImages, imgLen);

            int p = 0;
            for (int i = 0; i < numImages; i++)
            {
                var imgBuf = reader.ReadBytes(imgLen);
                for (int j = 0; j < imgBuf.Length; j++)
                    unsafe { imgData.Data[p++] = imgBuf[j] / 127.5 - 1.0; }
            }

            return imgData;
        }
    }

    private static Matrix2D loadLabels(string file)
    {
        using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            int magicNumber = (int)revEnd((uint)reader.ReadInt32());
            int numImages = (int)revEnd((uint)reader.ReadInt32());
            const int numClasses = 10;

            var labelData = Matrix2D.Zeros(numImages, numClasses);

            for (int i = 0; i < numImages; i++)
            {
                byte label = reader.ReadByte();
                unsafe { labelData.Data[i * numClasses + label] = 1; }
            }

            return labelData;
        }
    }

    private static uint revEnd(uint orig)
    {
        return ((orig & 0x000000FF) << 24) | ((orig & 0x0000FF00) <<  8)
            | ((orig & 0x00FF0000) >>  8) | ((orig & 0xFF000000) >> 24);
    }
}
