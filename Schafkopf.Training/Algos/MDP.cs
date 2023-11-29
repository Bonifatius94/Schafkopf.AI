namespace Schafkopf.Training;

public class CardPickerExpCollector
{
    public CardPickerExpCollector(
        PPOModel strategy, PossibleCardPicker cardSampler)
    {
        this.strategy = strategy;
        this.cardSampler = cardSampler;
    }

    private GameRules rules = new GameRules();
    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private PPOModel strategy;
    private PossibleCardPicker cardSampler;

    private struct TurnBatches
    {
        public TurnBatches(int numSessions)
        {
            s0Batches = Enumerable.Range(0, 4)
                .Select(i => Matrix2D.Zeros(numSessions, 90)).ToArray();
            a0Batches = Enumerable.Range(0, 4)
                .Select(i => Matrix2D.Zeros(numSessions, 1)).ToArray();
            piBatches = Enumerable.Range(0, 4)
                .Select(i => Matrix2D.Zeros(numSessions, 32)).ToArray();
            piSparseBatches = Enumerable.Range(0, 4)
                .Select(i => Matrix2D.Zeros(numSessions, 32)).ToArray();
            vBatches = Enumerable.Range(0, 4)
                .Select(i => Matrix2D.Zeros(numSessions, 1)).ToArray();
        }

        public Matrix2D[] s0Batches { get; set; }
        public Matrix2D[] a0Batches { get; set; }
        public Matrix2D[] piBatches { get; set; }
        public Matrix2D[] piSparseBatches { get; set; }
        public Matrix2D[] vBatches { get; set; }
    }

    public void Collect(PPORolloutBuffer buffer)
    {
        if (buffer.NumEnvs % 4 != 0)
            throw new ArgumentException("The number of envs needs to be "
                + "divisible by 4 because 4 agents are playing the game!");
        if (buffer.Steps % 8 != 0)
            throw new ArgumentException("The number of steps needs to be "
                + "divisible by 8 because each agent plays 8 cards per game!");

        int numGames = buffer.Steps / 8;
        int numSessions = buffer.NumEnvs / 4;
        var envs = Enumerable.Range(0, numSessions)
            .Select(i => new CardPickerEnv()).ToArray();
        var states = envs.Select(env => env.Reset()).ToArray();
        var batchesOfTurns = Enumerable.Range(0, 8)
            .Select(i => new TurnBatches(numSessions)).ToArray();
        var rewards = Matrix2D.Zeros(8, buffer.NumEnvs);

        for (int gameId = 0; gameId < numGames + 1; gameId++)
        {
            playGame(envs, states, batchesOfTurns);
            prepareRewards(states, rewards);
            fillBuffer(gameId, buffer, states, batchesOfTurns, rewards);
        }
    }

    private void fillBuffer(
        int gameId, PPORolloutBuffer buffer, GameLog[] states,
        TurnBatches[] batchesOfTurns, Matrix2D rewards)
    {
        for (int t_id = 0; t_id < 8; t_id++)
        {
            var expBufNull = buffer.SliceStep(gameId * 8 + t_id);
            if (expBufNull == null) return;
            var expBuf = expBufNull.Value;

            var batches = batchesOfTurns[t_id];
            var r1Batch = rewards.SliceRows(t_id, 1);

            for (int envId = 0; envId < states.Length; envId++)
            {
                var p_ids = states[envId].UnrollActingPlayers()
                    .Skip(t_id * 4).Take(4).Zip(Enumerable.Range(0, 4));
                foreach ((int p_id, int i) in p_ids)
                {
                    var s0Batch = batches.s0Batches[i];
                    var a0Batch = batches.a0Batches[i];
                    var vBatch = batches.vBatches[i];
                    var piSparseBatch = batches.piSparseBatches[i];

                    int rowid = envId * 4 + p_id;
                    Matrix2D.CopyData(
                        s0Batch.SliceRows(envId, 1),
                        expBuf.StatesBefore.SliceRows(rowid, 1));

                    unsafe
                    {
                        expBuf.Actions.Data[rowid] = a0Batch.Data[envId];
                        expBuf.Rewards.Data[rowid] = rewards.Data[envId];
                        expBuf.Terminals.Data[rowid] = t_id == 7 ? 1 : 0;
                        expBuf.OldProbs.Data[rowid] = piSparseBatch.Data[envId];
                        expBuf.OldBaselines.Data[rowid] = vBatch.Data[envId];
                    }
                }
            }
        }
    }

    private void prepareRewards(GameLog[] states, Matrix2D rewards)
    {
        for (int envId = 0; envId < states.Length; envId++)
        {
            var finalState = states[envId];
            foreach ((int t_id, var p_id, var reward) in finalState.UnrollRewards())
            {
                int rowid = states.Length * 4 * t_id + envId * 4 + p_id;
                unsafe { rewards.Data[rowid] = reward; }
            }
        }
    }

    private Card[] cardsCache = new Card[8];
    private void playGame(CardPickerEnv[] envs, GameLog[] states, TurnBatches[] batchesOfTurns)
    {
        for (int t_id = 0; t_id < 8; t_id++)
        {
            var batches = batchesOfTurns[t_id];

            for (int i = 0; i < 4; i++)
            {
                var s0Batch = batches.s0Batches[i];
                var a0Batch = batches.a0Batches[i];
                var piBatch = batches.piBatches[i];
                var vBatch = batches.vBatches[i];
                var piSparseBatch = batches.piSparseBatches[i];

                for (int envId = 0; envId < states.Length; envId++)
                {
                    var s0 = stateSerializer.SerializeState(states[envId]);
                    unsafe { s0.ExportFeatures(s0Batch.Data + envId * 90); }
                }

                strategy.Predict(s0Batch, piBatch, vBatch);

                var actions = a0Batch.SliceRowsRaw(0, envs.Length);
                var selProbs = piSparseBatch.SliceRowsRaw(0, envs.Length);
                for (int envId = 0; envId < envs.Length; envId++)
                {
                    var piSlice = piBatch.SliceRowsRaw(envId, 1);
                    var possCards = rules.PossibleCards(states[envId], cardsCache);
                    var card = cardSampler.PickCard(possCards, piSlice);
                    int action = card.Id % 32;
                    actions[envId] = action;
                    selProbs[envId] = piSlice[action];
                }

                for (int envId = 0; envId < envs.Length; envId++)
                {
                    // info: rewards and terminals are
                    //       determined after the game is over
                    (var newState, double reward, bool isTerminal) =
                        envs[envId].Step(new Card((byte)actions[envId]));
                    states[envId] = newState;
                }
            }
        }
    }
}

public class CardPickerEnv
{
    private CardsDeck deck = new CardsDeck();
    private int kommtRaus = 3;

    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    private GameLog log;
    private Hand[] initialHandsCache = new Hand[4];

    public GameLog Reset()
    {
        kommtRaus = (kommtRaus + 1) % 4;
        deck.Shuffle();
        deck.InitialHands(initialHandsCache);

        // info: klopfer is not required to train a card picker
        int klopfer = 0; // askForKlopfer(initialHandsCache);
        var call = makeCalls(klopfer, initialHandsCache, kommtRaus);
        log = GameLog.NewLiveGame(call, initialHandsCache, kommtRaus, klopfer);

        return log;
    }

    public (GameLog, double, bool) Step(Card cardToPlay)
    {
        if (log.CardCount >= 32)
            throw new InvalidOperationException("Game is already finished!");

        // info: kontra/re is not required to train a card picker
        // if (log.CardCount <= 1)
        //     askForKontraRe(log);

        log.NextCard(cardToPlay);

        // info: reward doesn't relate to the next state, compute it in calling scope
        return (log, 0.0, log.CardCount >= 28);
    }

    #region Call

    private HeuristicGameCaller caller =
        new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });

    private GameCall makeCalls(int klopfer, Hand[] initialHands, int kommtRaus)
    {
        var call = GameCall.Weiter();

        for (int i = 0; i < 4; i++)
        {
            int p_id = (kommtRaus + i) % 4;
            var hand = initialHands[p_id];
            var possibleCalls = callGen.AllPossibleCalls(p_id, initialHands, call);
            var nextCall = caller.MakeCall(possibleCalls, i, hand, klopfer);
            if (nextCall.Mode == GameMode.Weiter)
                continue;
            call = nextCall;
        }

        return call;
    }

    #endregion Call
}
