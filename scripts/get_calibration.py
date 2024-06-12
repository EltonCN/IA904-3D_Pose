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

#Specs: https://docs.luxonis.com/projects/hardware/en/latest/pages/BW1098OAK/
#from specs: f_number, pixel_size, resolution

center_data = {
    "camera_name": "center",
    "f_number":1.8, #+- 5%
    "pixel_size":1.55, #uM
    "resolution":[4056, 3040], #12MP
}

left_data = {
    "camera_name": "left",
    "f_number":2.0, #+- 5%
    "pixel_size":3.0,#uM
    "resolution":[1280, 800], #1MP
}


right_data = {
    "camera_name": "right",
    "f_number":2.0, #+- 5%
    "pixel_size":3.0,#uM
    "resolution":[1280, 800], #1MP
}

sensors_data = [center_data, left_data, right_data]
sensors_socket = [CAM_RGB, CAM_LEFT, CAM_RIGHT]

#From calibration
for i in range(3):
    socket = sensors_socket[i]
    resolution = sensors_data[i]["resolution"]

    intrinsic_matrix = np.array(calibData.getCameraIntrinsics(socket, resolution[0], resolution[1]))

    sensors_data[i]["intrinsic_matrix"] = intrinsic_matrix

    if socket == CAM_RGB:
        translation = np.zeros(3)
        rotation = Rotation.identity().as_quat()
    else:
        pose = np.array(calibData.getCameraExtrinsics(CAM_RGB, socket)) #cm
        
        translation = pose[:3, 3]/100
        rotation = Rotation.from_matrix(pose[:3,:3]).as_quat()

    sensors_data[i]["translation"] = translation #m
    sensors_data[i]["rotation"] = rotation #quat [x,y,z,w]


#Compute from calibration and specs
for data in sensors_data:
    #Sensor sizes
    sensor_width = data["resolution"][0] * (data["pixel_size"] / 1000) # w_resolution * pixel_size [mm]
    sensor_height = data["resolution"][1] * (data["pixel_size"] / 1000) # w_resolution * pixel_size [mm]
    sensor_diagonal = np.sqrt(np.power([sensor_width, sensor_height], 2).sum())

    sensor_size = [sensor_width, sensor_height]

    data["sensor_size"] = sensor_size #mm
    data["sensor_diagonal"] = sensor_diagonal #mm

    #Focus
    f_mm = data["intrinsic_matrix"][0, 0] * (sensor_width/data["resolution"][0]) #fx [px] * sensor_width [mm] /image_width [px]

    #Not used
    #crop_factor = 43.27 / sensor_diagonal  #35 mm diagonal [mm] / sensor diagonal [mm]
    #f_eq_35mm = crop_factor * f_mm # crop_factor [-] * focal length [mm]

    data["focal_lenght"] = f_mm

    #offset [px] * sensor_width [mm] /image_width [px]
    offset_x = data["intrinsic_matrix"][0,2] * (sensor_width/data["resolution"][0]) #mm
    offset_y = data["intrinsic_matrix"][1,2] * (sensor_height/data["resolution"][1]) #mm

    offset_x /= sensor_width #multiple of sensor size
    offset_y /= sensor_height #multiple of sensor size

    offset_x -= 0.5
    offset_y -= 0.5


    data["offset"] = [offset_x, offset_y]


def encoder(obj):
    if isinstance(obj, np.integer):
            return int(obj)
    elif isinstance(obj, np.floating):
        return float(obj)
    elif isinstance(obj, np.ndarray):
        return obj.tolist()
    
    return obj.__dict__

file = open("camera_data.json", "w")
json.dump(sensors_data, file, default=lambda o: encoder(o))
file.close()

sensors_data_unity = {"type":"CameraData", "data": sensors_data}

file = open("camera_data_unity.json", "w")
json.dump(sensors_data_unity, file, default=lambda o: encoder(o))
file.close()