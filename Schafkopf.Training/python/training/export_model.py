
import tensorflow as tf
from tensorflow.python.framework.convert_to_constants \
    import convert_variables_to_constants_v2


def export_model_as_pb(model: tf.keras.Model, out_dir: str, out_filename: str):
    # Convert Keras model to ConcreteFunction
    full_model = tf.function(lambda x: model(x))
    full_model = full_model.get_concrete_function(
        tf.TensorSpec(model.inputs[0].shape, model.inputs[0].dtype))

    # Get frozen ConcreteFunction
    frozen_func = convert_variables_to_constants_v2(full_model)
    frozen_func.graph.as_graph_def()

    # Save frozen graph to disk
    tf.io.write_graph(
        graph_or_graph_def=frozen_func.graph,
        logdir=out_dir,
        name=f"{out_filename}.pb",
        as_text=False)
