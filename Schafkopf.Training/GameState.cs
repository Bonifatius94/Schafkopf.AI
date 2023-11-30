namespace Schafkopf.Training;

public struct GameState : IEquatable<GameState>
{
    public const int NUM_FEATURES = 90;

    public GameState() { }

    public double[] State = new double[NUM_FEATURES];

    public void LoadFeatures(double[] other)
        => Array.Copy(other, State, NUM_FEATURES);

    public unsafe void ExportFeatures(Span<double> other)
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
    public void SerializeSarsExps(GameLog completedGame, SarsExp[] exps)
    {
        if (completedGame.CardCount != 32)
            throw new ArgumentException("Can only process finished games!");
        serializeHistory(completedGame, stateBuffer);

        var actions = completedGame.UnrollActions().GetEnumerator();
        var rewards = completedGame.UnrollRewards().GetEnumerator();
        for (int t0 = 0; t0 < 32; t0++)
        {
            rewards.MoveNext();
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
            exps[t0].Reward = rewards.Current.Item2;
        }
    }

    public GameState SerializeState(GameLog liveGame)
    {
        int t = liveGame.CardCount;
        serializeHistory(liveGame, stateBuffer, t);
        return stateBuffer[t];
    }

    private int playerPosOfTurn(GameLog log, int t_id, int p_id)
        => t_id == 8 ? p_id : normPlayerId(p_id, log.Turns[t_id].FirstDrawingPlayerId);

    private void serializeHistory(GameLog history, GameState[] statesCache, int skip = 0)
    {
        int timesteps = history.CardCount;
        var origCall = history.Call;
        var hands = history.UnrollHands().GetEnumerator();
        var scores = history.UnrollAugen().GetEnumerator();
        var allActions = history.UnrollActions().ToArray();
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

                if (t >= skip)
                {
                    var hand = hands.Current;
                    var score = scores.Current;
                    var state = statesCache[t].State;
                    serializeState(state, normCalls, hand, t++, allActions, score);
                }

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

public static class GameLogEx
{
    public static IEnumerable<GameAction> UnrollActions(this GameLog log)
    {
        var turnCache = new Card[4];
        var action = new GameAction();

        foreach (var turn in log.Turns)
        {
            int p_id = turn.FirstDrawingPlayerId;
            turn.CopyCards(turnCache);

            for (int i = 0; i < turn.CardsCount; i++)
            {
                var card = turnCache[p_id];
                action.PlayerId = (byte)p_id;
                action.CardPlayed = card;
                yield return action;
                p_id = (p_id + 1) % 4;
            }
        }
    }

    public static IEnumerable<int> UnrollActingPlayers(this GameLog log)
    {
        foreach (var turn in log.Turns)
        {
            int p_id = turn.FirstDrawingPlayerId;

            for (int i = 0; i < turn.CardsCount; i++)
            {
                yield return p_id;
                p_id = (p_id + 1) % 4;
            }
        }
    }

    public static IEnumerable<Hand> UnrollHands(this GameLog log)
    {
        int i = 0;
        var hands = log.InitialHands.ToArray();
        foreach (var action in log.UnrollActions())
        {
            if (i++ >= log.CardCount)
                break;
            yield return hands[action.PlayerId];
            hands[action.PlayerId] = hands[action.PlayerId].Discard(action.CardPlayed);
        }
        if (log.CardCount == 32)
            yield return Hand.EMPTY;
    }

    public static IEnumerable<int[]> UnrollAugen(this GameLog log)
    {
        var augen = new int[4];
        foreach (var turn in log.Turns)
        {
            yield return augen;
            augen[turn.WinnerId] += turn.Augen;
        }
        yield return augen;
    }
}

public static class GameReward
{
    public static IEnumerable<(int, int, double)> UnrollRewards(this GameLog completeGame)
    {
        int callerId = completeGame.Call.CallingPlayerId;
        int partnerId = completeGame.Call.PartnerPlayerId;
        var oppIds = completeGame.OpponentIds.ToArray();
        var augenIter = completeGame.UnrollAugen().GetEnumerator();
        augenIter.MoveNext();

        int t = 0;
        foreach (var action in completeGame.UnrollActions())
        {
            if (t % 4 == 0)
                augenIter.MoveNext();
            var augen = augenIter.Current;

            if (completeGame.Call.Mode == GameMode.Sauspiel)
            {
                int p_id = action.PlayerId;
                bool isCaller = p_id == callerId;
                bool isPartner = p_id == partnerId;

                int ownAugen = augen[p_id];
                int partnerAugen;
                if (isCaller)
                    partnerAugen = augen[partnerId];
                else if (isPartner)
                    partnerAugen = augen[callerId];
                else if (p_id == oppIds[0])
                    partnerAugen = augen[oppIds[1]];
                else // if (p_id == oppIds[0])
                    partnerAugen = augen[oppIds[0]];

                bool knowsPartner = isPartner || completeGame.Turns[t / 4].AlreadyGsucht;
                double reward = rewardSauspiel(
                    ownAugen, partnerAugen, isCaller || isPartner, knowsPartner);
                yield return (t / 4, p_id, reward);
            }
            else // Wenz or Solo
            {
                int p_id = action.PlayerId;
                int callerAugen = augen[callerId];
                int opponentAugen = augen.Sum() - callerAugen;
                bool isCaller = p_id == callerId;
                bool isTout = completeGame.Call.IsTout;
                double reward = rewardSoloWenz(callerAugen, opponentAugen, isCaller, isTout);
                yield return (t / 4, p_id, reward);
            }

            t++;
        }
    }

    private static double rewardSoloWenz(
            int callerAugen, int opponentAugen,
            bool isCaller, bool isTout)
        => isTout
            ? (isCaller && callerAugen == 120 ? 1 : 0)
            : ((isCaller && callerAugen >= 61) || (!isCaller && opponentAugen >= 60) ? 1 : 0);

    private static double rewardSauspiel(
        int ownAugen, int partnerAugen,
        bool isCaller, bool knowsPartner)
    {
        int effAugen = knowsPartner ? ownAugen + partnerAugen : ownAugen;
        return (isCaller && effAugen >= 61) || (!isCaller && effAugen >= 60) ? 1 : 0;
    }
}
