namespace Schafkopf.Training.Tests;

public class RegressionTrainingTest
{
    private FlatFeatureDataset createDataset(int trainSize, int testSize)
    {
        var rng = new Random();
        Func<int, Func<double, double>, (double, double)[]> sample =
            (size, func) => Enumerable.Range(0, size)
                .Select(i => (double)rng.NextDouble() * 20 - 10)
                .Select(x => ((double)x, (double)func(x)))
                .ToArray();

        var trueFunc = (double x) => (double)Math.Sin(x);
        var trainData = sample(trainSize, trueFunc);
        var testData = sample(testSize, trueFunc);

        var trainX = Matrix2D.FromData(trainSize, 1, trainData.Select(x => x.Item1).ToArray());
        var trainY = Matrix2D.FromData(trainSize, 1, trainData.Select(x => x.Item2).ToArray());
        var testX = Matrix2D.FromData(testSize, 1, testData.Select(x => x.Item1).ToArray());
        var testY = Matrix2D.FromData(testSize, 1, testData.Select(x => x.Item2).ToArray());
        return new FlatFeatureDataset(trainX, trainY, testX, testY);
    }

    private FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1),
        });

    [Fact]
    public void Test_CanPredictSinus()
    {
        int batchSize = 64;
        var model = createModel();
        var dataset = createDataset(trainSize: 10_000, testSize: 1_000);
        var optimizer = new AdamOpt(learnRate: 0.002);
        var lossFunc = new MeanSquaredError();

        var session = new SupervisedTrainingSession(
            model, optimizer, lossFunc, dataset, batchSize);
        session.Train(epochs: 20);
        double testLoss = session.Eval();

        Assert.True(testLoss < 0.01);
    }
}

public class ClassificationTrainingTest
{
    private FFModel createModel()
        => new FFModel(new ILayer[] {
            new DenseLayer(200),
            new ReLULayer(),
            new DenseLayer(200),
            new ReLULayer(),
            new DenseLayer(10),
            new SoftmaxLayer(),
        });

    [Fact]
    public void Test_CanPredictMnist()
    {
        var model = createModel();
        var dataset = MnistDataset.LoadMnist(10_000, 1_000);
        var optimizer = new AdamOpt(learnRate: 0.002);
        var lossFunc = new CrossEntropyLoss();
        var accMetric = new CategoricalAccuracy();

        var session = new SupervisedTrainingSession(
            model, optimizer, lossFunc, dataset);
        session.Train(epochs: 1);
        double accuracy = accMetric.Eval(model, dataset);

        Assert.True(accuracy > 0.8);
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

    public static FlatFeatureDataset LoadMnist(int trainSize, int testSize)
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
            loadImages(Path.Combine(minstDir, mnistFiles[0]), trainSize),
            loadLabels(Path.Combine(minstDir, mnistFiles[1]), trainSize),
            loadImages(Path.Combine(minstDir, mnistFiles[2]), testSize),
            loadLabels(Path.Combine(minstDir, mnistFiles[3]), testSize));
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

    private static Matrix2D loadImages(string file, int datasetSize)
    {
        using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            int magicNumber = (int)revEnd((uint)reader.ReadInt32());
            int numImages = (int)revEnd((uint)reader.ReadInt32());
            int imageHeight = (int)revEnd((uint)reader.ReadInt32());
            int imageWidth = (int)revEnd((uint)reader.ReadInt32());

            numImages = Math.Min(datasetSize, numImages);
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

    private static Matrix2D loadLabels(string file, int datasetSize)
    {
        using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            int magicNumber = (int)revEnd((uint)reader.ReadInt32());
            int numImages = (int)revEnd((uint)reader.ReadInt32());

            const int numClasses = 10;
            numImages = Math.Min(datasetSize, numImages);
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
