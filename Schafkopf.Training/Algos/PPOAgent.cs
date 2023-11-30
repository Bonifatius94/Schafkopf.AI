
namespace Schafkopf.Training;

public class PPOTrainingSettings
{
    public int TotalSteps = 10_000_000;
    public double LearnRate = 3e-4;
    public double RewardDiscount = 0.99;
    public double GAEDiscount = 0.95;
    public double ProbClip = 0.2;
    public double ValueClip = 0.2;
    public double VFCoef = 0.5;
    public double EntCoef = 0.01;
    public bool NormAdvantages = true;
    public bool ClipValues = true;
    public int BatchSize = 64;
    public int NumEnvs = 32;
    public int NumStateDims = 90;
    public int NumActionDims = 32;
    public int StepsPerUpdate = 512;
    public int UpdateEpochs = 10;
    public int NumModelSnapshots = 20;

    public int TrainSteps => TotalSteps / NumEnvs;
    public int ModelSnapshotInterval => TrainSteps / NumModelSnapshots;
    public int NumTrainings => TrainSteps / StepsPerUpdate;
}

public class PPOTrainingSession
{
    public PPOModel Train(PPOTrainingSettings config)
    {
        var model = new PPOModel(config);
        var rollout = new PPORolloutBuffer(config);
        var exps = new CardPickerExpCollector(model);
        var benchmark = new RandomPlayBenchmark();
        var agent = new PPOAgent(model);

        for (int ep = 0; ep < config.NumTrainings; ep++)
        {
            exps.Collect(rollout);
            model.Train(rollout);

            if ((ep + 1) % 10 == 0)
            {
                double winRate = benchmark.Benchmark(agent);
                Console.WriteLine($"epoch {ep}: win rate vs. random agents is {winRate}");
            }
        }

        return model;
    }
}

public class PPOAgent : ISchafkopfAIAgent
{
    public PPOAgent(PPOModel model)
    {
        this.model = model;
    }

    private PPOModel model;
    private HeuristicAgent heuristicAgent = new HeuristicAgent();
    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private PossibleCardPicker sampler = new PossibleCardPicker();
    private UniformDistribution uniform = new UniformDistribution();

    private Matrix2D s0 = Matrix2D.Zeros(1, 90);
    private Matrix2D piOh = Matrix2D.Zeros(1, 32);
    private Matrix2D V = Matrix2D.Zeros(1, 1);

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var state = stateSerializer.SerializeState(log);
        state.ExportFeatures(s0.SliceRowsRaw(0, 1));
        model.Predict(s0, piOh, V);
        var predDist = piOh.SliceRowsRaw(0, 1);
        var card = new Card((byte)uniform.Sample(predDist));
        return sampler.PickCard(possibleCards, predDist, card);
    }

    public bool CallKontra(GameLog log) => heuristicAgent.CallKontra(log);
    public bool CallRe(GameLog log) => heuristicAgent.CallRe(log);
    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => heuristicAgent.IsKlopfer(position, firstFourCards);
    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => heuristicAgent.MakeCall(possibleCalls, position, hand, klopfer);
    public void OnGameFinished(GameLog final) => heuristicAgent.OnGameFinished(final);
}

public class PPOModel
{
    public PPOModel(PPOTrainingSettings config)
    {
        this.config = config;

        valueFunc = new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(1)
        });

        strategy = new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(config.NumActionDims),
            new SoftmaxLayer()
        });

        valueFunc.Compile(config.BatchSize, config.NumStateDims);
        strategy.Compile(config.BatchSize, config.NumStateDims);
        featureCache = Matrix2D.Zeros(config.BatchSize, config.NumStateDims);
        strategyOpt = new AdamOpt(config.LearnRate);
        valueFuncOpt = new AdamOpt(config.LearnRate);
    }

    private PPOTrainingSettings config;
    private FFModel valueFunc;
    private FFModel strategy;
    private IOptimizer strategyOpt;
    private IOptimizer valueFuncOpt;

    private Matrix2D featureCache;

    public void Predict(Matrix2D s0, Matrix2D outPiOnehot, Matrix2D outV)
    {
        var predPi = strategy.PredictBatch(s0);
        var predV = valueFunc.PredictBatch(s0);
        Matrix2D.CopyData(predPi, outPiOnehot);
        Matrix2D.CopyData(predV, outV);
    }

    public void Train(PPORolloutBuffer memory)
    {
        var batches = memory.SampleDataset(
            config.BatchSize, config.UpdateEpochs);

        foreach (var batch in batches)
            updateModels(batch);
    }

    private void updateModels(PPOTrainBatch batch)
    {
        // update strategy pi(s)
        var predPi = strategy.PredictBatch(batch.StatesBefore);
        var policyDeltas = strategy.Layers.Last().Cache.DeltasIn;
        computePolicyDeltas(batch, predPi, policyDeltas);
        strategy.FitBatch(policyDeltas, strategyOpt);

        // update baseline V(s)
        var predV = valueFunc.PredictBatch(batch.StatesBefore);
        var valueDeltas = valueFunc.Layers.Last().Cache.DeltasIn;
        computeValueDeltas(batch, predV, valueDeltas);
        valueFunc.FitBatch(valueDeltas, valueFuncOpt);
    }

    private void computePolicyDeltas(
        PPOTrainBatch batch, Matrix2D predPi, Matrix2D policyDeltas)
    {
        var normAdvantages = Matrix2D.Zeros(batch.Size, 1);
        var policyRatios = Matrix2D.Zeros(batch.Size, 1);
        var derPolicyRatios = Matrix2D.Zeros(batch.Size, 1);
        var newProbs = Matrix2D.Zeros(batch.Size, 1);
        var derNewProbs = Matrix2D.Zeros(batch.Size, 1);
        var clipMask = Matrix2D.Zeros(batch.Size, 1);
        var policyDeltasSparse = Matrix2D.Zeros(batch.Size, 1);

        var advantages = batch.Advantages;
        if (config.NormAdvantages)
        {
            double mean = Matrix2D.Mean(batch.Advantages);
            double stdDev = Matrix2D.StdDev(batch.Advantages);
            Matrix2D.BatchSub(batch.Advantages, mean, normAdvantages);
            Matrix2D.BatchDiv(batch.Advantages, stdDev, normAdvantages);
            advantages = normAdvantages;
        }

        var onehots = onehotIndices(batch.Actions, 32).Zip(Enumerable.Range(0, 32));
        foreach ((int p, int i) in onehots)
            unsafe { newProbs.Data[i] = predPi.Data[p]; }
        Matrix2D.BatchAdd(batch.OldProbs, 1e-8, policyRatios);
        Matrix2D.ElemDiv(policyRatios, newProbs, policyRatios);
        Matrix2D.BatchOneOver(derNewProbs, derPolicyRatios);
        Matrix2D.ElemMul(policyRatios, derPolicyRatios, derPolicyRatios);

        Matrix2D.ElemGeq(policyRatios, 1 + config.ProbClip, clipMask);
        Matrix2D.ElemLeq(policyRatios, 1 - config.ProbClip, clipMask);
        Matrix2D.ElemNeq(clipMask, 1, clipMask);

        Matrix2D.ElemMul(clipMask, derPolicyRatios, policyDeltasSparse);
        Matrix2D.ElemMul(policyDeltasSparse, advantages, policyDeltasSparse);
        Matrix2D.BatchMul(policyDeltasSparse, -1, policyDeltasSparse);

        Matrix2D.BatchMul(policyDeltas, 0, policyDeltas);
        foreach ((int p, int i) in onehots)
            unsafe { policyDeltas.Data[p] = policyDeltasSparse.Data[i]; }
    }

    private void computeValueDeltas(
        PPOTrainBatch batch, Matrix2D predV, Matrix2D valueDeltas)
    {
        var mse = new MeanSquaredError();
        var valueDeltasSparse = Matrix2D.Zeros(batch.Size, 1);
        mse.LossDeltas(predV, batch.Returns, valueDeltasSparse);

        // TODO: add value clipping

        Matrix2D.BatchMul(valueDeltas, 0, valueDeltas);
        var onehots = onehotIndices(batch.Actions, 32).Zip(Enumerable.Range(0, 32));
        foreach ((int p, int i) in onehots)
            unsafe { valueDeltas.Data[p] = valueDeltasSparse.Data[i]; }
    }

    private IEnumerable<int> onehotIndices(Matrix2D sparseClassIds, int numClasses)
    {
        for (int i = 0; i < sparseClassIds.NumRows; i++)
            yield return i * numClasses + (int)sparseClassIds.At(0, i);
    }
}

public class PossibleCardPicker
{
    private UniformDistribution uniform = new UniformDistribution();

    public Card PickCard(
            ReadOnlySpan<Card> possibleCards,
            ReadOnlySpan<double> predPi,
            Card sampledCard)
        => canPlaySampledCard(possibleCards, sampledCard) ? sampledCard
            : possibleCards[uniform.Sample(normProbDist(predPi, possibleCards))];

    public Card PickCard(ReadOnlySpan<Card> possibleCards, ReadOnlySpan<double> predPi)
        => possibleCards[uniform.Sample(normProbDist(predPi, possibleCards))];

    private bool canPlaySampledCard(
        ReadOnlySpan<Card> possibleCards, Card sampledCard)
    {
        foreach (var card in possibleCards)
            if (card == sampledCard)
                return true;
        return false;
    }

    private double[] probDistCache = new double[8];
    private ReadOnlySpan<double> normProbDist(
        ReadOnlySpan<double> probDistAll, ReadOnlySpan<Card> possibleCards)
    {
        double probSum = 0;
        for (int i = 0; i < possibleCards.Length; i++)
            probDistCache[i] = probDistAll[possibleCards[i].Id & Card.ORIG_CARD_MASK];
        for (int i = 0; i < possibleCards.Length; i++)
            probSum += probDistCache[i];
        double scale = 1 / probSum;
        for (int i = 0; i < possibleCards.Length; i++)
            probDistCache[i] *= scale;

        return probDistCache.AsSpan().Slice(0, possibleCards.Length);
    }
}

public struct PPOTrainBatch
{
    public PPOTrainBatch(int size, int numStateDims)
    {
        Size = size;
        StatesBefore = Matrix2D.Zeros(size, numStateDims);
        Actions = Matrix2D.Zeros(size, 1);
        Rewards = Matrix2D.Zeros(size, 1);
        Terminals = Matrix2D.Zeros(size, 1);
        Returns = Matrix2D.Zeros(size, 1);
        Advantages = Matrix2D.Zeros(size, 1);
        OldProbs = Matrix2D.Zeros(size, 1);
        OldBaselines = Matrix2D.Zeros(size, 1);
    }

    public int Size;
    public Matrix2D StatesBefore;
    public Matrix2D Actions;
    public Matrix2D Rewards;
    public Matrix2D Terminals;
    public Matrix2D Returns;
    public Matrix2D Advantages;
    public Matrix2D OldProbs;
    public Matrix2D OldBaselines;

    public void Shuffle(int[] permCache)
    {
        var perm = permCache ?? Perm.Identity(Size);
        Perm.Permutate(perm);

        Matrix2D.ShuffleRows(StatesBefore, perm);
        Matrix2D.ShuffleRows(Actions, perm);
        Matrix2D.ShuffleRows(Rewards, perm);
        Matrix2D.ShuffleRows(Terminals, perm);
        Matrix2D.ShuffleRows(Returns, perm);
        Matrix2D.ShuffleRows(Advantages, perm);
        Matrix2D.ShuffleRows(OldProbs, perm);
        Matrix2D.ShuffleRows(OldBaselines, perm);
    }

    public IEnumerable<PPOTrainBatch> SampleBatched(int batchSize)
    {
        int p = 0;
        int numBatches = Size / batchSize;
        var trainBuf = new PPOTrainBatch() { Size = batchSize };
        for (int i = 0; i < numBatches; i++)
        {
            trainBuf.StatesBefore = StatesBefore.SliceRows(p, batchSize);
            trainBuf.Actions = Actions.SliceRows(p, batchSize);
            trainBuf.Rewards = Rewards.SliceRows(p, batchSize);
            trainBuf.Terminals = Terminals.SliceRows(p, batchSize);
            trainBuf.Returns = Returns.SliceRows(p, batchSize);
            trainBuf.Advantages = Advantages.SliceRows(p, batchSize);
            trainBuf.OldProbs = OldProbs.SliceRows(p, batchSize);
            trainBuf.OldBaselines = OldBaselines.SliceRows(p, batchSize);
            yield return trainBuf;
            p += batchSize;
        }
    }

    public PPOTrainBatch SliceRows(int rowid, int length)
        => new PPOTrainBatch {
            Size = length,
            StatesBefore = StatesBefore.SliceRows(rowid, length),
            Actions = Actions.SliceRows(rowid, length),
            Rewards = Rewards.SliceRows(rowid, length),
            Terminals = Terminals.SliceRows(rowid, length),
            Returns = Returns.SliceRows(rowid, length),
            Advantages = Advantages.SliceRows(rowid, length),
            OldProbs = OldProbs.SliceRows(rowid, length),
            OldBaselines = OldBaselines.SliceRows(rowid, length)
        };
}

public class PPORolloutBuffer
{
    public PPORolloutBuffer(PPOTrainingSettings config)
    {
        NumEnvs = config.NumEnvs;
        Steps = config.StepsPerUpdate;
        gamma = config.RewardDiscount;
        gaeGamma = config.GAEDiscount;

        // info: the cache stores an extra timestep at the end
        //       which facilitates proper GAE computation

        int size = Steps * NumEnvs;
        int sizeWithExtraStep = (Steps + 1) * NumEnvs;
        cache = new PPOTrainBatch(sizeWithExtraStep, config.NumStateDims);
        cacheWithoutLastStep = cache.SliceRows(0, size);
        cacheOnlyFirstStep = cache.SliceRows(0, NumEnvs);
        cacheOnlyLastStep = cache.SliceRows(size, NumEnvs);
        permCache = Perm.Identity(size);
    }

    public int NumEnvs;
    public int Steps;
    private double gamma;
    private double gaeGamma;
    private PPOTrainBatch cache;
    private PPOTrainBatch cacheWithoutLastStep;
    private PPOTrainBatch cacheOnlyFirstStep;
    private PPOTrainBatch cacheOnlyLastStep;
    private int[] permCache;

    public bool IsReadyForModelUpdate(int t) => t > 0 && t % Steps == 0;

    public PPOTrainBatch? SliceStep(int t)
    {
        int offset = IsReadyForModelUpdate(t)
            ? Steps * NumEnvs : (t % Steps) * NumEnvs;
        return (t > Steps) ? null : cache.SliceRows(offset, NumEnvs);
    }

    public IEnumerable<PPOTrainBatch> SampleDataset(int batchSize, int epochs = 1)
    {
        cacheGAE(cache);

        for (int i = 0; i < epochs; i++)
        {
            shuffleDataset();
            foreach(var batch in cacheWithoutLastStep.SampleBatched(batchSize))
                yield return batch;
        }

        copyOverlappingStep();
    }

    private void shuffleDataset()
    {
        Perm.Permutate(permCache);
        cacheWithoutLastStep.Shuffle(permCache);
    }

    private void cacheGAE(PPOTrainBatch cache)
    {
        var nonterm_t1 = Matrix2D.Zeros(1, NumEnvs);
        var lambda = Matrix2D.Zeros(1, NumEnvs);
        var delta = Matrix2D.Zeros(1, NumEnvs);

        for (int t = Steps - 1; t >= 0; t--)
        {
            var r_t0 = cache.Rewards.SliceRows(t, 1);
            var term_t1 = cache.Terminals.SliceRows(t+1, 1);
            var v_t0 = cache.OldBaselines.SliceRows(t, 1);
            var v_t1 = cache.OldBaselines.SliceRows(t+1, 1);
            var A_t0 = cache.Advantages.SliceRows(t, 1);
            var G_t0 = cache.Returns.SliceRows(t, 1);

            Matrix2D.BatchMul(term_t1, -1, nonterm_t1);
            Matrix2D.BatchAdd(nonterm_t1, 1, nonterm_t1);

            Matrix2D.ElemMul(v_t1, nonterm_t1, delta);
            Matrix2D.BatchMul(delta, gamma, delta);
            Matrix2D.ElemAdd(delta, r_t0, delta);
            Matrix2D.ElemSub(delta, v_t0, delta);

            Matrix2D.ElemMul(nonterm_t1, lambda, lambda);
            Matrix2D.BatchMul(lambda, gamma * gaeGamma, lambda);
            Matrix2D.ElemAdd(lambda, delta, lambda);

            Matrix2D.CopyData(lambda, A_t0);
            Matrix2D.ElemAdd(v_t0, A_t0, G_t0);
        }
    }

    private void copyOverlappingStep()
    {
        Matrix2D.CopyData(cacheOnlyLastStep.StatesBefore, cacheOnlyFirstStep.StatesBefore);
        Matrix2D.CopyData(cacheOnlyLastStep.Actions, cacheOnlyFirstStep.Actions);
        Matrix2D.CopyData(cacheOnlyLastStep.Rewards, cacheOnlyFirstStep.Rewards);
        Matrix2D.CopyData(cacheOnlyLastStep.Terminals, cacheOnlyFirstStep.Terminals);
        Matrix2D.CopyData(cacheOnlyLastStep.Returns, cacheOnlyFirstStep.Returns);
        Matrix2D.CopyData(cacheOnlyLastStep.Advantages, cacheOnlyFirstStep.Advantages);
        Matrix2D.CopyData(cacheOnlyLastStep.OldProbs, cacheOnlyFirstStep.OldProbs);
        Matrix2D.CopyData(cacheOnlyLastStep.OldBaselines, cacheOnlyFirstStep.OldBaselines);
    }
}
