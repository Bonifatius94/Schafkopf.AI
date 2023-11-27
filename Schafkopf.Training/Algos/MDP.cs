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

    public void Collect(PPORolloutBuffer buffer)
    {
        if (buffer.NumEnvs % 4 != 0)
            throw new ArgumentException("The number of envs needs to be "
                + "divisible by 4 because 4 agents are playing the game!");

        int numGames = buffer.NumEnvs / 4;
        var envs = Enumerable.Range(0, numGames)
            .Select(i => new CardPickerEnv()).ToArray();
        var states = envs.Select(env => env.Reset()).ToArray();
        var s0Batch = Matrix2D.Zeros(numGames, 90);
        var a0Batch = Matrix2D.Zeros(numGames, 1);
        var piBatch = Matrix2D.Zeros(numGames, 32);
        var piSparse = Matrix2D.Zeros(numGames, 32);
        var vBatch = Matrix2D.Zeros(numGames, 1);
        var cardsCache = new Card[8];
        var logsCache = new GameLog[numGames];
        var predBuffer = new PPOPredictionCache(buffer.NumEnvs, 8);

        int step = 0;
        while (step < buffer.Steps)
        {
            for (int envId = 0; envId < states.Length; envId++)
            {
                var s0 = stateSerializer.SerializeState(states[envId]);
                unsafe { s0.ExportFeatures(s0Batch.Data + envId * 90); }
            }

            strategy.Predict(s0Batch, piBatch, vBatch);

            var actions = a0Batch.SliceRowsRaw(0, numGames);
            var selProbs = piSparse.SliceRowsRaw(0, numGames);
            for (int envId = 0; envId < numGames; envId++)
            {
                var piSlice = piBatch.SliceRowsRaw(envId, 1);
                var possCards = rules.PossibleCards(states[envId], cardsCache);
                var card = cardSampler.PickCard(possCards, piSlice);
                int action = card.Id % 32;
                actions[envId] = action;
                selProbs[envId] = piSlice[action];
            }

            for (int envId = 0; envId < numGames; envId++)
            {
                (var newState, double reward, bool isTerminal) =
                    envs[envId].Step(new Card((byte)actions[envId]));
                states[envId] = newState;
            }

            if (step % 32 == 0)
            {
                // TODO: continue implementation
                // 1) evaluate the game logs to determine rewards
                // 2) fill the training buffer with game histories
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
