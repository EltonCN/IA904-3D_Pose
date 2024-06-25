import os
import glob
import json
import multiprocessing as mp

import torch
import wandb

from train import compute_loss_and_optimize, MODE_TRAIN, MODE_EVALUATE
from model import OAKD3DKeypoint
from dataset import SyntheticOAKDDataset
from loader import FastDataLoader

DATASET_PATH = os.environ["USERPROFILE"]+"\\AppData\\LocalLow\\DefaultCompany\\IA904-3D_Pose\\solo"
DATASET_PATH = "I:\\.shortcut-targets-by-id\\1S6q0nt4z5LYa-5VkpC8qxag2b_jjO6e9\\IA904\\Dataset"

BASE_ARTIFACT_NAME = "eltoncn/IA904-OAKD3DKeypoint/model:{model_version}"
MODEL_VERSIONS = ["0_mse", "0_mae", "1_mse", "1_mae", "2_mse", "2_mae"]
SCENARIOS = [0, 1, 2, 3, 4]

convolutional_sizes = [10, 5]
dense_hidden_size = 50
dropout_rate = 0.2 #Not necessary

api = wandb.Api()

def get_model_path(model_version:str) -> str:
    artifact_name = BASE_ARTIFACT_NAME.format(model_version=model_version)
    artifact = api.artifact(artifact_name)
    artifact_dir = artifact.download()

    model_pattern = os.path.join(artifact_dir, "*.pth")
    paths = glob.glob(model_pattern)

    return paths[0]

def get_model(model_version:str) -> OAKD3DKeypoint:
    model_path = get_model_path(model_version)

    model = OAKD3DKeypoint([256, 192], convolutional_sizes, dense_hidden_size, dropout_rate)

    state_dict = torch.load(model_path)
    model.load_state_dict(state_dict)

    return model

if __name__ == "__main__":
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

    cpu_count = mp.cpu_count()

    criterions = {}
    criterions["mse"] = torch.nn.MSELoss()
    criterions["mae"] = torch.nn.L1Loss()

    all_evaluations = {}

    for model_version in MODEL_VERSIONS:
        model = get_model(model_version)
        model.to(device)

        model_evaluations = {}

        for scenario in SCENARIOS:
            dataset = SyntheticOAKDDataset(DATASET_PATH, [scenario])
            dataloader = FastDataLoader(dataset, 32, True, num_workers=cpu_count-1)

            print(model_version, scenario)
            losses = compute_loss_and_optimize(model, dataloader, criterions, "mse", MODE_EVALUATE)

            for name in losses:
                losses[name] = losses[name].item()

            model_evaluations[scenario] = losses
        
        all_evaluations[model_version] = model_evaluations

    file_name = "evaluations.json"
    with open(file_name, "w") as file:
        json.dump(all_evaluations, file)