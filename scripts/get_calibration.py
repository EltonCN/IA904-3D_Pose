import json

import depthai as dai
import numpy as np
from scipy.spatial.transform import Rotation


with dai.Device() as device:
    calibData = device.readCalibration()

img_w = 4032 # 3840 4K
img_h = 3040 # 2160 4K

CAM_RGB = dai.CameraBoardSocket.CAM_A
CAM_LEFT = dai.CameraBoardSocket.CAM_B
CAM_RIGHT = dai.CameraBoardSocket.CAM_C

len_pos = calibData.getLensPosition(CAM_RGB) 
M_rgb = np.array(calibData.getCameraIntrinsics(CAM_RGB, img_w, img_h))
left_pose = np.array(calibData.getCameraExtrinsics(CAM_RGB, CAM_LEFT))
right_pose = np.array(calibData.getCameraExtrinsics(CAM_RGB, CAM_RIGHT))

left_rotation = Rotation.from_matrix(left_pose[:3,:3])
right_rotation = Rotation.from_matrix(right_pose[:3,:3])

left_euler = left_rotation.as_euler("xyz", degrees=True)
right_euler = left_rotation.as_euler("xyz", degrees=True)

left_t = left_pose[:3, 3] / 100 #meters
right_t = right_pose[:3, 3] / 100 #meters

data = {}
data["left_rotation"] = list(left_euler)
data["right_rotation"] = list(right_euler)
data["left_translation"] = list(left_t)
data["right_translation"] = list(right_t)

file = open("calibration_data.json", "w")
json.dump(data, file)
file.close()

######

sensor_width = 4056*(1.55/1000) # w_resolution * pixel_size [mm]
sensor_heigth = 3040*(1.55/1000) # h_resolution * pixel_size [mm]
sensor_diagonal = np.sqrt(np.power([sensor_width, sensor_heigth], 2).sum())


f_mm = M_rgb[0, 0] * (sensor_width/img_w) #fx [px] * sensor_width [mm] /image_width [px]

crop_factor = 43.27 / sensor_diagonal  #35 mm diagonal [mm] / sensor diagonal [mm]
f_eq_35mm = crop_factor * f_mm # crop_factor [-] * focal length [mm]

f_number = 1.8 #Spec: 1.8 +- 5%