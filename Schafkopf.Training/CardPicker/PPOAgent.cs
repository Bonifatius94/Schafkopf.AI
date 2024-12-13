namespace Schafkopf.Training;

// public class CardPickerPPOAdapter : IPPOAdapter<GameState, Card>
// {
//     public CardPickerPPOAdapter(PPOTrainingSettings config)
//         => actionsCache = Enumerable.Range(0, config.NumEnvs)
//             .Select(i => new Card(0)).ToArray();

//     private readonly Card[] actionsCache;

//     public void EncodeAction(Card a0, Matrix2D buf)
//     {
//         throw new NotImplementedException();
//     }

//     public void EncodeState(GameState s0, Matrix2D buf)
//     {
//         s0.ExportFeatures(buf.SliceRowsRaw(0, 1));
//     }

//     public IList<Card> SampleActions(Matrix2D pi)
//     {
//         for (int i = 0; i < pi.NumRows; i++)
//             actionsCache[i].Id = (int)pi.At(i, 0);
//         return actionsCache;
//     }
// }

public class SchafkopfPPOTrainingSession
{
    public PPOModel Train(PPOTrainingSettings config)
    {
        // TODO: implement training procedure
        throw new NotImplementedException();
    }
}

// public class SchafkopfPPOTrainingSession
// {
//     public PPOModel Train(PPOTrainingSettings config)
//     {
//         var model = new PPOModel(config);
//         var rollout = new PPORolloutBuffer<GameState, Card>(
//             config,

//         );
//         var exps = new CardPickerExpCollector();
//         var benchmark = new RandomPlayBenchmark();
//         var agent = new SchafkopfPPOAgent(model);

//         for (int ep = 0; ep < config.NumTrainings; ep++)
//         {
//             Console.WriteLine($"epoch {ep+1}");
//             exps.Collect(rollout, model);
//             model.Train(rollout);

//             model.RecompileCache(batchSize: 1);
//             double winRate = benchmark.Benchmark(agent);
//             model.RecompileCache(batchSize: config.BatchSize);

//             Console.WriteLine($"win rate vs. random agents: {winRate}");
//             Console.WriteLine("--------------------------------------");
//         }

//         return model;
//     }
// }

public class SchafkopfPPOAgent : ISchafkopfAIAgent
{
    public SchafkopfPPOAgent(PPOModel model)
    {
        this.model = model;
    }

    private PPOModel model;
    private HeuristicAgent heuristicAgent = new HeuristicAgent();
    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private PossibleCardPicker sampler = new PossibleCardPicker();

    private Matrix2D s0 = Matrix2D.Zeros(1, 90);
    private Matrix2D pi = Matrix2D.Zeros(1, 1);
    private Matrix2D piProbs = Matrix2D.Zeros(1, 1);
    private Matrix2D V = Matrix2D.Zeros(1, 1);

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var state = stateSerializer.SerializeState(log);
        state.ExportFeatures(s0.SliceRowsRaw(0, 1));
        model.Predict(s0, pi, piProbs, V);
        var idx = (int)pi.At(0, 0);
        return possibleCards[idx];
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

public class CardPickerExpCollector
{
    public void Collect(PPORolloutBuffer<GameState, Card> buffer, PPOModel strategy)
    {
        int numGames = buffer.Steps / 8;
        int numSessions = buffer.NumEnvs / 4;
        var envs = Enumerable.Range(0, numSessions)
            .Select(i => new MultiAgentCardPickerEnv()).ToArray();

        var vecAgent = new VectorizedCardPickerAgent(strategy, numSessions);
        var agents = Enumerable.Range(0, buffer.NumEnvs)
            .Select(i => new AsyncCardPickerAgent(vecAgent)).ToArray();

        var expCache = new SchafkopfPPOExp[buffer.NumEnvs];
        int t = 0;
        var barr = new Barrier(buffer.NumEnvs, (b) => {
            buffer.AppendStep(expCache, t++);
            Console.Write($"\rcollecting ppo data {t} / {buffer.Steps}    ");
        });

        var collectTasks = Enumerable.Range(0, buffer.NumEnvs)
            .Select(i => Task.Run(() => {
                var agent = agents[i];
                var env = envs[i / 4];
                foreach (var exp in agent.PlaySteps(i % 4, env, buffer.Steps))
                {
                    barr.SignalAndWait();
                    expCache[i] = exp;
                }
            }))
            .ToArray();

        Task.WaitAll(collectTasks);
        Console.WriteLine();
    }
}

public class VectorizedCardPickerAgent
{
    public VectorizedCardPickerAgent(PPOModel strategy, int numSessions)
    {
        states = Matrix2D.Zeros(numSessions, GameState.NUM_FEATURES);
        predPi = Matrix2D.Zeros(numSessions, 1);
        predPiProbs = Matrix2D.Zeros(numSessions, 1);
        predV = Matrix2D.Zeros(numSessions, 1);

        samplers = Enumerable.Range(0, numSessions)
            .Select(i => new PossibleCardPicker()).ToArray();

        threadIds = new int[numSessions];
        barr = new Barrier(numSessions, (b) => strategy.Predict(states, predPi, predPiProbs, predV));
    }

    private int[] threadIds;
    private Barrier barr;

    private Matrix2D states;
    private Matrix2D predPi;
    private Matrix2D predPiProbs;
    private Matrix2D predV;

    private PossibleCardPicker[] samplers;

    private int sessionIdByThread()
    {
        int threadId = Environment.CurrentManagedThreadId;
        for (int i = 0; i < threadIds.Length; i++)
            if (threadIds[i] == threadId)
                return i;
        throw new InvalidOperationException("Unregistered thread!");
    }

    public void Register(int sessionId)
    {
        threadIds[sessionId] = Environment.CurrentManagedThreadId;
    }

    public (Card, double, double) Predict(
        GameState state, ReadOnlySpan<Card> possCards)
    {
        int sessionId = sessionIdByThread();
        var s0Slice = states.SliceRowsRaw(sessionId, 1);
        state.ExportFeatures(s0Slice);

        barr.SignalAndWait();

        var idx = (int)predPi.At(sessionId, 0);
        var pi = predPi.At(sessionId, 0);
        var card = possCards[idx];
        double V = predV.At(sessionId, 0);

        return (card, pi, V);
    }
}

public class AsyncCardPickerAgent
{
    public AsyncCardPickerAgent(VectorizedCardPickerAgent vecAgent)
    {
        this.vecAgent = vecAgent;
    }

    private VectorizedCardPickerAgent vecAgent;
    private Card[] cardCache = new Card[8];
    private GameRules rules = new GameRules();
    private GameStateSerializer stateSerializer = new GameStateSerializer();

    public IEnumerable<SchafkopfPPOExp> PlaySteps(
        int playerId, MultiAgentCardPickerEnv env, int steps)
    {
        var exp = new SchafkopfPPOExp();
        env.Register(playerId);
        var state = env.Reset();

        for (int i = 0; i < steps; i++)
        {
            (GameState s0, Card a0, double pi, double V) = predict(state);
            (state, double r1, bool t1) = env.Step(a0);
            if (t1)
                state = env.Reset();

            exp.StateBefore = s0;
            exp.Action = a0;
            exp.Reward = r1;
            exp.IsTerminal = t1;
            exp.OldProb = pi;
            exp.OldBaseline = V;
            yield return exp;
        }
    }

    private (GameState, Card, double, double) predict(GameLog state)
    {
        var possCards = rules.PossibleCards(state, cardCache);
        var encState = stateSerializer.SerializeState(state);
        (var a0, var pi, var V) = vecAgent.Predict(encState, possCards);
        return (encState, a0, pi, V);
    }
}

public class PossibleCardPicker
{
    public Card PickCard(ReadOnlySpan<Card> possibleCards, ReadOnlySpan<double> predPi)
        => possibleCards[normProbDist(predPi, possibleCards).Sample()];

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
