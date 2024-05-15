using UnityEngine;

public class CameraMounter : MonoBehaviour
{
    [SerializeField] CameraData cameraData;

    public void Mount()
    {
        foreach(CameraData.Data data in cameraData.data)
        {
            Transform cameraTransform = transform.Find(data.camera_name);
            Camera camera = cameraTransform.GetComponent<Camera>();

            camera.usePhysicalProperties = true;

            camera.sensorSize = new Vector2(data.sensor_size[0], data.sensor_size[1]);
            camera.aperture = data.f_number;
            camera.focalLength = data.focal_lenght;
            camera.lensShift = new Vector2(data.offset[0], data.offset[1]);
            camera.gateFit = Camera.GateFitMode.Fill;


            Vector3 position = new Vector3(data.translation[0], data.translation[1], data.translation[2]);
            Quaternion rotation = new Quaternion(data.rotation[0], data.rotation[1], data.rotation[2], data.rotation[3]);

            cameraTransform.localPosition = position;
            cameraTransform.localRotation = rotation;
        }
    }
}