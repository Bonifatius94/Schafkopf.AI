
from tensorflow.keras import Sequential
from tensorflow.keras.layers import Dense


def create_model():
    model = Sequential([
        Dense(256, activation='relu', name='input'),
        Dense(256, activation='relu', name='hidden_1'),
        Dense(8, activation='sigmoid', name='output'),
    ])
    model.build((None, 8))
    return model
