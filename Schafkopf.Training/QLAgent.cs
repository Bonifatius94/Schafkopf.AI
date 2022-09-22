using Keras.Models;
using Keras.Layers;
using Keras;
using Numpy;

namespace Schafkopf.Training;

public class GameState
{
    public GameState(GameLog log)
    {
        const int NO_CARD = -1;
        int p = 0;
        var stateArr = new float[90];

        // game call (6 floats)
        stateArr[p++] = encode(log.Call.Mode);
        stateArr[p++] = encode(log.Call.IsTout);
        stateArr[p++] = log.Call.CallingPlayerId;
        stateArr[p++] = log.Call.PartnerPlayerId;
        stateArr[p++] = encode(log.Call.Trumpf);
        stateArr[p++] = encode(log.Call.GsuchteFarbe);

        // hand (16 floats)
        var hand = log.InitialHands[0];
        for (int i = 0; i < hand.CardsCount; i++)
        {
            var card = hand[i];
            if (card.Exists)
                stateArr[p++] = encode(card.Type);
                stateArr[p++] = encode(card.Color);
        }
        while (p < 22)
            stateArr[p++] = -1;

        // turn history (64 floats)
        for (int t = 0; t < log.TurnCount; t++)
        {
            var cardsOfTurn = new Card[4];
            var turn = log.Turns[t];
            turn.CopyCards(cardsOfTurn);

            for (int i = 0; i < turn.CardsCount; i++)
            {
                int normPid = (i + turn.FirstDrawingPlayerId) % 4;
                stateArr[p++] = encode(cardsOfTurn[normPid].Type);
                stateArr[p++] = encode(cardsOfTurn[normPid].Color);
            }
        }
        while (p < 86)
            stateArr[p++] = NO_CARD;

        // augen (4 floats)
        for (int i = 0; i < 4; i++)
            stateArr[p++] = log.Scores[0];

        EncodedState = new NDarray<float>(stateArr);
        Reward = reward(log, 0);
    }

    public NDarray<float> EncodedState { get; private set; }
    public float Reward { get; private set; }

    private float encode(GameMode mode) => (float)mode / 4;
    private float encode(CardColor color) => (float)color / 4;
    private float encode(CardType type) => (float)type / 8;
    private float encode (bool flag) => flag ? 1 : 0;

    private float reward(GameLog log, int playerId)
    {
        // intention of this reward system:
        // - players receive reward 1 as soon as they are in a winning state
        // - if they are in a losing or undetermined state, they receive reward 0

        // info: players don't know yet who the sauspiel partner is
        //       -> no reward, even if it's already won
        if (log.Call.Mode == GameMode.Sauspiel && !log.CurrentTurn.AlreadyGsucht)
            return 0;

        bool isCaller = log.CallerIds.Contains(playerId);
        double callerScore = log.CallerIds
            .Select(i => log.Scores[i]).Sum();

        if (log.Call.Mode != GameMode.Sauspiel && log.Call.IsTout)
            return isCaller && callerScore == 120 ? 1 : 0;
        else
            return (isCaller && callerScore >= 61) || !isCaller ? 1 : 0;
    }
}

public class SARSCardExperience
{
    public GameState StateBefore { get; init; }
    public GameState StateAfter { get; init; }
    // TODO: think about whether this is a good action representation
    public int Action { get; init; }
    public float Reward { get; init; }
    public bool IsTerminal { get; init; }
}

public class QLTrainingHparams
{
    public double LearningRate { get; init; }
    public double ExplorationRate { get; init; }
    public double RewardDiscount { get; init; }
}

public class QLAgent : ISchafkopfAIAgent
{
    private QLCardPicker cardPicker = new QLCardPicker();

    // TODO: try to get rid of this statefulness -> extract into another class
    private int cardsPlayed = 0;
    private GameState[] statesOfGame = new GameState[33];
    private int[] actions = new int[32];

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var state = new GameState(log);
        int action = cardPicker.Predict(state, possibleCards.Length);

        // cache states and actions for training on it
        statesOfGame[cardsPlayed] = state;
        actions[cardsPlayed] = action;
        cardsPlayed++;

        return possibleCards[action];
    }

    private static readonly Random rng = new Random();

    public GameCall MakeCall(
        ReadOnlySpan<GameCall> possibleCalls,
        int position, Hand hand, int klopfer)
    {
        // info: just make random calls for now to see whether the card picker AI works
        //       -> if picking cards does work, collect some training data and
        //          create a call maker model from it (requires win rate data to evaluate calls)
        return possibleCalls[rng.Next(possibleCalls.Length)];
    }

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameLog history)
        => false;

    public bool CallRe(GameLog history)
        => false;

    public void OnGameFinished(GameLog final)
    {
        if (cardsPlayed != 32)
            throw new InvalidOperationException("Game is not over yet!");

        var finalState = new GameState(final);
        statesOfGame[32] = finalState;

        for (int i = 0; i < 32; i++)
        {
            var s0 = statesOfGame[i];
            var s1 = statesOfGame[i+1];
            int action = actions[i];
            float reward = s0.Reward;
            bool isTerminal = i == 31;
        }

        // reset game cache

    }
}

public class QLCardPicker
{
    public QLCardPicker(QLTrainingHparams? hparams = null)
    {
        this.hparams = hparams ?? new QLTrainingHparams() {
            LearningRate = 0.01,
            ExplorationRate = 0.1,
            RewardDiscount = 0.99
        };
        model = createModel();
    }

    private QLTrainingHparams hparams;
    private static readonly Random rng = new Random();
    private Sequential model;

    public int Predict(GameState state, int numPossibleCards)
    {
        if (numPossibleCards == 1)
            return 0;

        bool explore = rng.NextDouble() <= hparams.ExplorationRate;
        if (explore)
            return rng.Next(numPossibleCards);

        var input = state.EncodedState.reshape((1, 90));
        var pred = model.Predict(input);
        var bestAction = (int)np.argmax(pred, axis: 1)[0];
        // TODO: check out if this transformation actually works

        // info: prediction always yields reward estimations for all 8 cards;
        //       if there are less card this might produce index overflows,
        //       so a valid random index is picked instead
        return bestAction < numPossibleCards ? bestAction
            : rng.Next(numPossibleCards);
    }

    public void TrainModel(SARSCardExperience exp)
    {
        // TODO: implement the training loop here ...
    }

    private Sequential createModel()
    {
        return new Sequential(new BaseLayer[] {
            new Dense(256, activation: "relu", input_shape: new Shape(90)),
            new Dense(256, activation: "relu"),
            new Dense(256, activation: "relu"),
            new Dense(8, activation: "sigmoid")
        });
    }
}

// =====================================================

// public class InitialHandState
// {
//     private float encode(CardColor color) => (float)color / 4;
//     private float encode(CardType type) => (float)type / 8;
//     private float encode(int pos) => (float)pos / 8;

//     public InitialHandState(Hand initialHand, int position)
//     {
//         int p = 0;
//         var stateArr = new float[17];
//         for (int i = 0; i < 8; i++)
//         {
//             stateArr[p++] = encode(initialHand[i].Type);
//             stateArr[p++] = encode(initialHand[i].Color);
//         }
//         stateArr[p] = encode(position);
//         EncodedState = new NDarray<float>(stateArr);
//     }

//     public NDarray<float> EncodedState { get; private set; }
// }

// TODO: use monetary gain / loss as reward function for game calls
// public class SARSCallExperience
// {
//     // TODO: normalize action mappings towards calls

//     public GameState StateBefore { get; init; }
//     public GameState StateAfter { get; init; }
//     public int Action { get; init; }
//     public float Reward { get; init; }
//     public bool IsTerminal { get; init; }
// }

// public class QLCallMaker
// {
//     public QLCallMaker(QLTrainingHparams? hparams = null)
//     {
//         this.hparams = hparams ?? new QLTrainingHparams() {
//             LearningRate = 0.01,
//             ExplorationRate = 0.1,
//             RewardDiscount = 0.99
//         };
//         model = createModel();
//     }

//     private QLTrainingHparams hparams;
//     private Sequential model;

//     public void TrainModel(SARSCardExperience exp)
//     {
//         // TODO: implement the training loop
//     }

//     private Sequential createModel()
//     {
//         return new Sequential(new BaseLayer[] {
//             new Dense(16, activation: "relu", input_shape: new Shape(6)),
//             new Dense(32, activation: "relu"),
//             new Dense(32, activation: "relu"),
//             new Dense(14, activation: "sigmoid")
//         });
//     }
// }
