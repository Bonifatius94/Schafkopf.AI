namespace Schafkopf.Training;

public interface MDPEnv<StateT, ActionT>
{
    StateT Reset();
    (StateT, double, bool) Step(ActionT cardToPlay);
}

public class CardPickerEnv : MDPEnv<GameLog, Card>
{
    private CardsDeck deck = new CardsDeck();
    private int kommtRaus = 3;

    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    private GameLog log;
    private Hand[] initialHandsCache = new Hand[4];

    public GameLog Reset()
    {
        kommtRaus = (kommtRaus + 1) % 4;

        GameCall call; int klopfer = 0;
        do {
            deck.Shuffle();
            deck.InitialHands(initialHandsCache);
            call = makeCalls(klopfer, initialHandsCache, kommtRaus);
        }
        while (call.Mode == GameMode.Weiter);

        return log = GameLog.NewLiveGame(call, initialHandsCache, kommtRaus, klopfer);
    }

    public (GameLog, double, bool) Step(Card cardToPlay)
    {
        if (log.CardCount >= 32)
            throw new InvalidOperationException("Game is already finished!");

        log.NextCard(cardToPlay);
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

public class MultiAgentCardPickerEnv : MDPEnv<GameLog, Card>
{
    public MultiAgentCardPickerEnv()
    {
        env = new CardPickerEnv();
        threadIds = new int[4];
        termBarr = new Barrier(4);
        restBarr = new Barrier(4, (b) => { state = env.Reset(); });
        stateModMut = new Mutex();
        state = env.Reset();
    }

    private CardPickerEnv env;
    private GameLog state;
    private int[] threadIds;
    private Barrier termBarr;
    private Barrier restBarr;
    private Mutex stateModMut;

    private int playerIdByThread()
    {
        int threadId = Environment.CurrentManagedThreadId;
        for (int i = 0; i < 4; i++)
            if (threadIds[i] == threadId)
                return i;

        throw new InvalidOperationException(
            "Unregistered thread playing games!");
    }

    public void Register(int playerId)
    {
        threadIds[playerId] = Environment.CurrentManagedThreadId;
    }

    public GameLog Reset()
    {
        int playerId = playerIdByThread();
        while (state.DrawingPlayerId != playerId)
            Thread.Sleep(1);
        return state;
    }

    public (GameLog, double, bool) Step(Card cardToPlay)
    {
        int t_id = state.CardCount / 4;
        bool isTermial = state.CardCount >= 28;
        stateModMut.WaitOne();
        (state, var _, var __) = env.Step(cardToPlay);
        stateModMut.ReleaseMutex();

        if (isTermial)
        {
            // info: wait until game is in final state
            termBarr.SignalAndWait();
            var finalState = state;
            restBarr.SignalAndWait();

            // TODO: include reward computation here ...
            return (finalState, 0.0, true);
        }
        else
        {
            // info: wait until it's the player's turn again
            int playerId = playerIdByThread();
            while (checkId(playerId))
                Thread.Sleep(1);

            // TODO: include reward computation here ...
            return (state, 0.0, false);
        }
    }

    private bool checkId(int playerId)
    {
        stateModMut.WaitOne();
        bool ret = state.DrawingPlayerId != playerId;
        stateModMut.ReleaseMutex();
        return ret;
    }
}
