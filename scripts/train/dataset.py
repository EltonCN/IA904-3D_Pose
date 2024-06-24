from __future__ import annotations

import json
import glob #Search files
import re
import os #Path things
import pickle
from types import MappingProxyType
from typing import Tuple

import tqdm
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import torch
from torch.utils.data import Dataset, DataLoader, random_split

coco2unity = MappingProxyType({"nose": 87, #nose
                "left_eye": 80, #eye_left
                "right_eye": 81, #eye_right
                "left_ear": 85, #ear_left
                "right_ear": 86, #ear_right
                "left_shoulder": 21, #shoulder_left
                "right_shoulder": 50, #shoulder_right
                "left_elbow": 22, #elbow_left
                "right_elbow": 51, #elbow_right
                "left_wrist": 25, #wrist_left
                "right_wrist": 54, #wrist_right
                "left_hip": 88, #hip_left
                "right_hip": 89, #hip_right
                "left_knee": 2, #knee_left
                "right_knee": 10, #knee_right
                "left_ankle": 4, #ankle_left
                "right_ankle": 12, #ankle_right
                })

n_sample_per_scenario = [2000, 2000, 2000, 200, 200]

class SyntheticOAKDDataset(Dataset):
    def __init__(self, dataset_path:str, 
                scenarios:list[int]) -> None:
        super().__init__()


        self._dataset_path = dataset_path
        self._scenarios = scenarios

        n_sample = 0
        n_sample = []
        for scenario in scenarios:
            n_sample.append(n_sample_per_scenario[scenario])

        self._n_sample = np.array(n_sample)

        self._init = False
    
    def _lazy_init(self) -> None:
        if self._init:
            return

        self._init = True

        index_path = os.path.join(self._dataset_path, "index.pickle")
        with open(index_path, "rb") as index_file:
            dataset_index = pickle.load(index_file)

        dataset_index = pd.DataFrame(dataset_index)

        features_dataset_path = os.path.join(self._dataset_path, "extracted_features")

        self._features_dataset_path = features_dataset_path
        self._dataset_index = dataset_index
        self._keypoint_indexes = list(coco2unity.values())
        
        self._n_keypoint_per_image = len(coco2unity)
        self._n_sample_cumsum = np.cumsum(self._n_sample)


    def __len__(self) -> int:
        return self._n_sample.sum()
    
    def __getitem__(self, index) -> Tuple[torch.Tensor, torch.Tensor]:
        self._lazy_init()

        img_index = index // self._n_keypoint_per_image
        keypoint_index = index % self._n_keypoint_per_image

        scenario_index = np.argwhere(self._n_sample_cumsum > img_index)[0][0]
        scenario = self._scenarios[scenario_index]

        index_in_scenario = img_index
        if scenario_index != 0:
            index_in_scenario -= self._n_sample_cumsum[scenario_index-1]

        scenario_mask = self._dataset_index["scenario"] == scenario
        df = self._dataset_index[scenario_mask]
        line = df.iloc[[index_in_scenario]]
        
        # Get features
        sequence = line["sequence"].values[0]
        step = line["step"].values[0]

        features_folder = os.path.join(self._features_dataset_path,
                            f"Scenario{scenario}",
                            f"sequence.{sequence}")
        
        features = []
        for name in ["left", "right", "center"]:
            features_path = os.path.join(features_folder, 
                                        f"step{step}.camera_{name}_heatmaps.npz")
            
            camera_features = np.load(features_path)["img_heatmaps"]
            keypoint_features = camera_features[keypoint_index]
            features.append(keypoint_features)
        
        features = np.array(features)

        # Get target

        keypoints_3d = df.iloc[[1]]["keypoints_3d"].values[0]
        unity_keypoint_index = self._keypoint_indexes[keypoint_index]
        keypoints_3d = keypoints_3d[unity_keypoint_index]   

        return torch.tensor(features, dtype=torch.float32), torch.tensor(keypoints_3d, dtype=torch.float32)

