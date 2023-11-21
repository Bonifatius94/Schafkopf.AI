namespace Schafkopf.Training;

public struct GameState : IEquatable<GameState>
{
    public const int NUM_FEATURES = 90;

    public GameState() { }

    public double[] State = new double[NUM_FEATURES];

    public void LoadFeatures(double[] other)
        => Array.Copy(other, State, NUM_FEATURES);

    public unsafe void ExportFeatures(double* other)
    {
        for (int i = 0; i < NUM_FEATURES; i++)
            other[i] = State[i];
    }

    public bool Equals(GameState other)
    {
        for (int i = 0; i < NUM_FEATURES; i++)
            if (State[i] != other.State[i])
                return false;
        return true;
    }

    public override int GetHashCode() => 0;
}

public class GameStateSerializer
{
    private const int NO_CARD = -1;

    public static GameState[] NewBuffer()
        => Enumerable.Range(0, 36).Select(x => new GameState()).ToArray();

    private GameState[] stateBuffer = NewBuffer();
    public void SerializeSarsExps(
        GameLog completedGame, SarsExp[] exps,
        Func<GameLog, int, double> reward)
    {
        if (completedGame.CardCount != 32)
            throw new ArgumentException("Can only process finished games!");
        serializeHistory(completedGame, stateBuffer);

        var actions = completedGame.UnrollActions().GetEnumerator();
        for (int t0 = 0; t0 < 32; t0++)
        {
            actions.MoveNext();
            var card = actions.Current.CardPlayed;
            int p_id = actions.Current.PlayerId;
            int t_id = t0 / 4;
            bool isTerminal = t0 >= 28;
            int t1 = (t_id+1) * 4 + playerPosOfTurn(completedGame, t_id+1, p_id);

            exps[t0].Action = card;
            exps[t0].StateBefore.LoadFeatures(stateBuffer[t0].State);
            exps[t0].StateAfter.LoadFeatures(stateBuffer[t1].State);
            exps[t0].IsTerminal = isTerminal;
            exps[t0].Reward = reward(completedGame, t1);
        }
    }

    public GameState SerializeState(GameLog liveGame)
    {
        serializeHistory(liveGame, stateBuffer);
        return stateBuffer[liveGame.CardCount - 1];
    }

    private int playerPosOfTurn(GameLog log, int t_id, int p_id)
        => t_id == 8 ? p_id : normPlayerId(p_id, log.Turns[t_id].FirstDrawingPlayerId);

    private void serializeHistory(GameLog completedGame, GameState[] statesCache)
    {
        int timesteps = completedGame.CardCount;
        var origCall = completedGame.Call;
        var hands = completedGame.UnrollHands().GetEnumerator();
        var scores = completedGame.UnrollAugen().GetEnumerator();
        var allActions = completedGame.UnrollActions().ToArray();
        var normCalls = new GameCall[] {
            normCallForPlayer(origCall, 0), normCallForPlayer(origCall, 1),
            normCallForPlayer(origCall, 2), normCallForPlayer(origCall, 3)
        };

        int t = 0;
        for (int t_id = 0; t_id < Math.Ceiling((double)timesteps / 4); t_id++)
        {
            scores.MoveNext();

            for (int i = 0; i < 4; i++)
            {
                hands.MoveNext();

                var hand = hands.Current;
                var score = scores.Current;
                var state = statesCache[t].State;
                serializeState(state, normCalls, hand, t++, allActions, score);

                if (t == timesteps) return;
            }
        }

        scores.MoveNext();

        for (; t < 36; t++)
            serializeState(
                statesCache[t].State, normCalls, Hand.EMPTY,
                t, allActions, scores.Current);
    }

    private unsafe void serializeState(
        double[] state, ReadOnlySpan<GameCall> normCalls, Hand hand, int t,
        ReadOnlySpan<GameAction> turnHistory, int[] augen)
    {
        if (state.Length < 90)
            throw new IndexOutOfRangeException("Memory overflow");

        // memory layout:
        //  - game call (6 floats)
        //  - hand (16 floats)
        //  - turn history (64 floats)
        //  - augen (4 floats)

        int actingPlayer = t < 32 ? turnHistory[t].PlayerId : t & 0x3;
        var call = normCalls[actingPlayer];

        fixed (double* stateArr = &state[0])
        {
            int offset = 0;
            offset += serializeGameCall(stateArr, call);
            offset += serializeHand(stateArr + offset, hand);
            offset += serializeTurnHistory(
                stateArr + offset, turnHistory, Math.Min(t, 31), actingPlayer);
            serializeAugen(stateArr + offset, augen, actingPlayer);
        }
    }

    private unsafe int serializeGameCall(
        double* stateArr, GameCall call)
    {
        int p = 0;
        stateArr[p++] = GameEncoding.Encode(call.Mode);
        stateArr[p++] = GameEncoding.Encode(call.IsTout);
        stateArr[p++] = (double)call.CallingPlayerId / 4;
        stateArr[p++] = (double)call.PartnerPlayerId / 4;
        stateArr[p++] = GameEncoding.Encode(call.Trumpf);
        stateArr[p++] = GameEncoding.Encode(call.GsuchteFarbe);
        return 6;
    }

    private unsafe int serializeHand(double* stateArr, Hand hand)
    {
        int p = 0;
        foreach (var card in hand)
            p += serializeCard(stateArr, card);
        while (p < 16)
            stateArr[p++] = NO_CARD;
        return 16;
    }

    private unsafe int serializeTurnHistory(
        double* stateArr, ReadOnlySpan<GameAction> turnHistory, int t, int p_id)
    {
        int offset = 0;
        for (int i = 0; i < 64; i++)
            stateArr[offset++] = NO_CARD;

        for (int i = 0; i < t; i++)
        {
            var action = turnHistory[i];
            int norm_pid = normPlayerId(action.PlayerId, p_id);
            offset = ((i & ~0x3) + norm_pid) * 2;
            serializeCard(stateArr + offset, turnHistory[i].CardPlayed);
        }
        return 64;
    }

    private unsafe int serializeAugen(
        double* stateArr, int[] scores, int p_id)
    {
        for (int i = 0; i < 4; i++)
            stateArr[i] = (double)scores[(p_id + i) & 0x3] / 120;
        return 4;
    }

    private unsafe int serializeCard(double* stateArr, Card card)
    {
        stateArr[0] = GameEncoding.Encode(card.Type);
        stateArr[1] = GameEncoding.Encode(card.Color);
        return 2;
    }

    private GameCall normCallForPlayer(GameCall call, int p_id)
    {
        if (call.Mode == GameMode.Weiter)
            return call;

        int callingPlayer = normPlayerId(call.CallingPlayerId, p_id);
        int partnerPlayer = normPlayerId(call.PartnerPlayerId, p_id);

        if (call.Mode == GameMode.Sauspiel)
            return GameCall.Sauspiel(callingPlayer, partnerPlayer, call.GsuchteFarbe);
        else if (call.Mode == GameMode.Wenz)
            return GameCall.Wenz(callingPlayer, call.IsTout);
        else // if (call.Mode == GameMode.Solo)
            return GameCall.Solo(callingPlayer, call.Trumpf, call.IsTout);
    }

    private int normPlayerId(int id, int offset)
        => (id - offset + 4) & 0x03;
}

public class GameEncoding
{
    public static double Encode(GameMode mode) => (double)mode / 4;
    public static double Encode(CardColor color) => (double)color / 4;
    public static double Encode(CardType type) => (double)type / 8;
    public static double Encode(bool flag) => flag ? 1 : 0;

    public static unsafe int EncodeOnehot(double* stateArr, GameMode mode)
        => EncodeOnehot(stateArr, (int)mode, 4);

    public static unsafe int EncodeOnehot(double* stateArr, CardColor color)
        => EncodeOnehot(stateArr, (int)color, 4);

    public static unsafe int EncodeOnehot(double* stateArr, CardType type)
        => EncodeOnehot(stateArr, (int)type, 8);

    public static unsafe int EncodeOnehot(double* stateArr, int id, int numClasses)
    {
        for (int i = 0; i < numClasses; i++)
            stateArr[i] = 0;
        stateArr[id] = 1;
        return numClasses;
    }

    public static unsafe int EncodeOnehot(double* stateArr, bool flag)
    {
        stateArr[0] = flag ? 1 : -1;
        return 1;
    }
}

public class GameReward
{
    public static double Reward(GameLog log, int t)
    {
        // intention of this reward system:
        // - players receive reward 1 as soon as they are in a winning state
        // - if they are in a losing or undetermined state, they receive reward 0

        // info: t >= 32 relate to the final game outcome
        //       from the view of the player with p_id = t%4
        int playerId = t >= 32 ? t % 4 :
            normPlayerId(log.Turns[t / 4].FirstDrawingPlayerId, t % 4);

        // info: players don't know yet who the sauspiel partner is
        //       -> no reward, even if it's already won
        bool alreadyGsucht = t < 28 ? log.Turns[t / 4].AlreadyGsucht : true;
        if (log.Call.Mode == GameMode.Sauspiel && !alreadyGsucht)
            return 0;

        bool isCaller = log.CallerIds.Contains(playerId);
        var augen = log.UnrollAugen().Last();
        double callerScore = 0;
        for (int i = 0; i < log.CallerIds.Length; i++)
            callerScore += augen[log.CallerIds[i]];

        if (log.Call.Mode != GameMode.Sauspiel && log.Call.IsTout)
            return isCaller && callerScore == 120 ? 1 : 0;
        else
            return (isCaller && callerScore >= 61) || !isCaller ? 1 : 0;
    }

    private static int normPlayerId(int id, int offset)
        => (id - offset + 4) & 0x03;
}
