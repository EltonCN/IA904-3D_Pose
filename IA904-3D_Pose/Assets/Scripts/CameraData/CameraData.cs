using UnityEngine;

class CameraData : ScriptableObject
{
    [System.Serializable]
    public struct Data
    {
        public string  camera_name;
        public float f_number;
        public float pixel_size;
        public float[] resolution;
        public float[][] intrinsic_matrix;
        public float[] translation;
        public float[] rotation;
        public float[] sensor_size;
        public float sensor_diagonal;
        public float[] offset;

        public float focal_lenght;
    }

    public Data[] data;

    
}