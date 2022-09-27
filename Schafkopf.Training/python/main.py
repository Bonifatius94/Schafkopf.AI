
import sys
import os
from training.create_model import create_model
from training.export_model import export_model_as_pb


def new_model(out_dir: str, out_filename: str):
    model = create_model()
    print(model.summary())
    export_model_as_pb(model, out_dir, out_filename)


def main():
    if len(sys.argv) <= 1:
        raise ValueError('no task specified! ("train", "new_model")')
    task_type = str(sys.argv[1])

    if task_type == 'new_model':
        out_filepath = str(sys.argv[2]) if len(sys.argv) >= 3 else './model.pb'
        new_model(os.path.dirname(out_filepath), os.path.basename(out_filepath))
    elif task_type == 'train':
        pass # TODO: implement this
    else:
        raise ValueError(f"unknown task '{task_type}'")


if __name__ == '__main__':
    main()
