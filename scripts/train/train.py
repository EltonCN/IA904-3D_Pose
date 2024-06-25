from __future__ import annotations

import json
import glob #Search files
import re
import os #Path things
import pickle
import time
import wandb
import datetime
import random
import multiprocessing as mp
from typing import Tuple, Callable
    

import tqdm
import numpy as np
from numpy.typing import ArrayLike
import pandas as pd
import matplotlib.pyplot as plt
import torch
from torch.utils.data import Dataset, DataLoader, random_split

from dataset import SyntheticOAKDDataset
from loader import FastDataLoader
from model import OAKD3DKeypoint


dataset_path = os.environ["USERPROFILE"]+"\\AppData\\LocalLow\\DefaultCompany\\IA904-3D_Pose\\solo"
dataset_path = "I:\\.shortcut-targets-by-id\\1S6q0nt4z5LYa-5VkpC8qxag2b_jjO6e9\\IA904\\Dataset"

features_dataset_path = os.path.join(dataset_path, "extracted_features")
index_path = os.path.join(dataset_path, "index.pickle")

DEV_TEST_TAG = "DevTest"
FINAL_MODEL_TAG = "FinalModel"

MODE_TRAIN = 0
MODE_EVALUATE = 1

class EarlyStop:
    def __init__(self, patience:int|None, min_delta:float|None):
        if patience is None:
            patience = 1
        if min_delta is None:
            min_delta = 0.0

        self._patience = patience
        self._min_delta = min_delta

        self._counter = 0
        self._last_maximum = float("inf")

    def __call__(self, validation_loss:float) -> bool:
        if validation_loss < self._last_maximum:
            self._counter = 0

            self._last_maximum = validation_loss
        elif validation_loss - self._last_maximum > self._min_delta:
            self._counter += 1

        if self._counter >= self._patience:
            return True
        return False

def print_info(loss_value:torch.Tensor, epoch:int, total_epochs:int, 
               time:float|None=None):
    """
    Prints the information of a epoch.

    Args:
        loss_value (torch.Tensor): epoch loss.
        epoch (int): epoch number.
        total_epochs (int): total number of epochs. 
        time (float, optional): time to run the epoch. Don't print if is 0.0. Defaults to 0.0.
        accuracy (float, optional): epoch accuracy.
    """

    print(f'Epoch [{epoch+1}/{total_epochs}], \
            Loss: {loss_value.item():.4f}', end="")

    if time is None:
        print("")
    else:
        print(f", Elapsed Time: {time:.2f} sec")
        
def compute_loss_and_optimize(model:torch.nn.Module, 
                 loader:DataLoader, 
                 criterions:dict[torch.nn.Module],
                 loss_name:str, 
                 mode:int = MODE_EVALUATE, 
                 optimizer:torch.optim.Optimizer|None=None, 
                 accumulation_steps:int|None = None) -> dict[str, torch.Tensor]:
    """
    Computes the loss from a model across a dataset.

    If in train mode also runs optimizer steps.

    Args:
        model (torch.nn.Module): model to evaluate.
        loader (DataLoader): dataset.
        criterion (torch.nn.Module): loss function to compute.
        mode (int): mode of the computation. 
                    If MODE_EVALUATE, computes without gradient, in eval mode and detachs loss.
                    If MODE_TRAIN, computes with gradient and in train mode.
                    Default is MODE_EVALUATE.
        optimizer (torch.optim.Optimizer, optional): optimizer to use in the train mode.

    Returns:
        torch.Tensor: resulting loss.
    """
    if accumulation_steps is None:
        accumulation_steps = 1

    original_grad_state = torch.is_grad_enabled()
    original_model_state = model.training

    device = next(iter(model.parameters())).device

    if mode == MODE_EVALUATE:
        model.eval()
        torch.set_grad_enabled(False)
    elif mode == MODE_TRAIN:
        model.train()
        torch.set_grad_enabled(True)
        optimizer.zero_grad()
    else:
        raise ValueError(f"Unknown mode: {mode}.")

    batch_index = 0

    total_loss : dict[str, torch.Tensor] = {}
    for name in criterions:
        total_loss[name] = torch.tensor(0, dtype=torch.float32, device=device)

    n = 0
    for inputs, targets in tqdm.tqdm(loader):
        inputs : torch.Tensor = inputs.to(device)
        targets : torch.Tensor = targets.to(device)
        
        logits = model(inputs)
        logits = logits.view(-1, logits.shape[-1])

        losses : dict[str, torch.Tensor] = {}
        for name in criterions:
            losses[name] = criterions[name](logits.squeeze(), targets)

            total_loss[name] += losses[name]*targets.size(0)
        
        
        n += targets.size(0)

        if mode == MODE_TRAIN:
            loss = losses[loss_name]

            loss /= accumulation_steps
            loss.backward()

            if ((batch_index+1) % accumulation_steps == 0) or (batch_index+1 == len(loader)):
                optimizer.step()
                optimizer.zero_grad()

        batch_index += 1

    for name in total_loss:
        total_loss[name] /= n 
        total_loss[name] = total_loss[name].detach()

    #Return original state
    torch.set_grad_enabled(original_grad_state)
    
    if original_model_state:
        model.train()
    else:
        model.eval()

    return total_loss

def train(model:torch.nn.Module, criterions:dict[str, torch.nn.Module], loss_name:str, optimizer:torch.optim.Optimizer, 
          dataloaders:DataLoader, n_epoch:int, accumulation_steps:int=1,
          use_wandb:bool=False, early_stop:Callable[[float], bool]|None=None) -> dict[str, ArrayLike]:
    
    hist : dict[str, list[torch.Tensor]|list[float]]= {}
    for name in criterions:
        hist[f"loss_train_{name}"] = []
        hist[f"loss_val_{name}"] = []
    hist["time"] = []

    loss_val = compute_loss_and_optimize(model, dataloaders["val"], criterions, loss_name, MODE_EVALUATE)
        
    print("VAL ", end="")
    print_info(loss_val[loss_name], -1, n_epoch)

    for epoch in range(n_epoch):
        start_time = time.time() 

        loss_train = compute_loss_and_optimize(model, dataloaders["train"], criterions, loss_name, MODE_TRAIN, optimizer, accumulation_steps)

        end_time = time.time() 
        
        epoch_duration = end_time - start_time 


        print_info(loss_train[loss_name], epoch, n_epoch, epoch_duration)
        
        #Validation stats
        loss_val = compute_loss_and_optimize(model, dataloaders["val"], criterions, loss_name, MODE_EVALUATE)
        
        print("VAL ", end="")
        print_info(loss_val[loss_name], epoch, n_epoch)

        #Save history and log
        log : dict[str, float] = {}

        for name in criterions:
            hist[f"loss_train_{name}"].append(loss_train[name].item())
            hist[f"loss_val_{name}"].append(loss_val[name].item())
            
            log[f"loss_train_{name}"] = loss_train[name].item()
            log[f"loss_val_{name}"] = loss_val[name].item()

        hist["time"].append(epoch_duration)


        if use_wandb:
            wandb.log(log)

        if early_stop is not None:
            if early_stop(loss_val[loss_name]):
                break

    for key in hist:
        hist[key] = np.array(hist[key])

    return hist

if __name__ == "__main__":

    use_wandb = True

    accumulation_steps = 1 # Passos de acumulação de gradiente
    batch_size = 32 # Tamanho de um batch
    convolutional_sizes = [10, 5]
    dataset_configuration = 0
    dense_hidden_size = 50 # Quantidade de unidades na camada escondida
    dropout_rate = 0.2
    lr = 5e-3 # Taxa de treinamento
    min_delta = 0
    n_epoch = 10 # Quantidade de epochs
    optimizer_class = torch.optim.Adam # Otimizador
    patience = 10
    seed = 42
    train_loss = "mse"
    weight_decay = 5e-4 # Regularização L2

    tags = [DEV_TEST_TAG]#[FINAL_MODEL_TAG]

    config = {
        "accumulation_steps": accumulation_steps,
        "batch_size": batch_size,
        "convolutional_sizes" : convolutional_sizes,
        "dataset_configuration" : dataset_configuration,
        "dense_hidden_size":dense_hidden_size,
        "dropout_rate" : dropout_rate,
        "lr": lr,
        "min_delta":min_delta,
        "n_epoch": n_epoch,
        "optimizer_class": optimizer_class.__name__,
        "patience":patience,
        "seed" : seed,
        "train_loss" : train_loss,
        "weight_decay": weight_decay,
    }

    if use_wandb:
        run = wandb.init(project="IA904-OAKD3DKeypoint", config=config, tags=tags)
        run_name = run.name
    else:
        run_name = str(datetime.datetime.now().timestamp())

    if dataset_configuration == 0:
        dataset = SyntheticOAKDDataset(dataset_path, [0])
    elif dataset_configuration == 1:
        dataset = SyntheticOAKDDataset(dataset_path, [0, 1])
    elif dataset_configuration == 2:
        dataset = SyntheticOAKDDataset(dataset_path, [0, 1, 2])
    else:
        raise ValueError(f"dataset_configuration must be in {{0, 1, 2}} (configuration {dataset_configuration} does not exist).")

    torch_generator = torch.Generator()
    torch_generator.manual_seed(seed)
    torch.manual_seed(seed)
    random.seed(seed)

    train_dataset, val_dataset = random_split(dataset, [0.8, 0.2], generator=torch_generator)
    datasets = {"train":train_dataset, "val":val_dataset}

    cpu_count = mp.cpu_count()

    dataloaders = {}
    for name in datasets:
        dataset = datasets[name]
        loader = FastDataLoader(dataset, batch_size, True, num_workers=cpu_count-1, generator=torch_generator)
        dataloaders[name] = loader
    
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

    model = OAKD3DKeypoint([256, 192], convolutional_sizes, dense_hidden_size, dropout_rate)
    model.to(device)

    criterions = {}
    criterions["mse"] = torch.nn.MSELoss()
    criterions["mae"] = torch.nn.L1Loss()

    optmizer = optimizer_class(model.parameters(), lr=lr, weight_decay=weight_decay)

    if use_wandb:
        run.watch(model, criterions[train_loss], log_graph=True)

    early_stop = EarlyStop(patience, min_delta)

    train_history = train(model, criterions, train_loss, optmizer, 
                          dataloaders, n_epoch, accumulation_steps, 
                          use_wandb, early_stop)

    model_path = f"{run_name}.pth"
    model_path = os.path.join("models", model_path)
    torch.save(model.state_dict(), model_path)

    if use_wandb:
        if DEV_TEST_TAG not in tags:
            artifact = wandb.Artifact("model", type="model")
            artifact.add_file(model_path)

            run.log_artifact(artifact)
        
        run.finish()