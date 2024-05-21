using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

public class CameraInstrinsicsLabeler : CameraLabeler
{
    public override string description => "Register the camera intrinsics info";
    public override string labelerId => "InstrinsicsLabeler";
    protected override bool supportsVisualization => false;


    Camera cam;

    AnnotationDefinition targetMetricsDef;
    

    protected override void Setup()
    {
        targetMetricsDef = new TargetMetricsDef("target1");
        DatasetCapture.RegisterAnnotationDefinition(targetMetricsDef);

        cam = perceptionCamera.GetComponent<Camera>();
    }

    protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
    {
        float pixel_aspect_ratio = (float)cam.pixelWidth / (float)cam.pixelHeight;

        // f_mm [mm] *  resolution [px] / sensorSize [mm] -> f [px]
        // 2,56 * 1280 / 3,84
        float alpha_u = cam.focalLength * ((float)cam.pixelWidth / cam.sensorSize.x); 

        float alpha_v = cam.focalLength * ((float)cam.pixelHeight / cam.sensorSize.y); //cam.focalLength * pixel_aspect_ratio * ((float)cam.pixelHeight / cam.sensorSize.y);

        float u_0 = (float)cam.pixelWidth / 2;
        float v_0 = (float)cam.pixelHeight / 2;

        float[] calibration_matrix = {alpha_u, 0f, u_0,
                                    0f, alpha_v, v_0,
                                    0f, 0f, 1f};

        float far_clip_plane = cam.farClipPlane;
        float near_clip_plane = cam.nearClipPlane;

        var sensorHandle = perceptionCamera.SensorHandle;

        var annotation1 = new TargetMetrics(targetMetricsDef, sensorHandle.Id, calibration_matrix, far_clip_plane, near_clip_plane);
        sensorHandle.ReportAnnotation(targetMetricsDef, annotation1);;
    }

    class TargetMetricsDef : AnnotationDefinition
    {
        public TargetMetricsDef(string id)
            : base(id) { }

        public override string modelType => "instrinsicsMetricsDef";
        public override string description => "The perception camera intrinsics.";
    }

    [Serializable]
    class TargetMetrics : Annotation
    {
        public TargetMetrics(AnnotationDefinition definition, string sensorId, float[] calibration_matrix, float far_plane, float near_plane)
            : base(definition, sensorId)
        {
            this.farClippingPlane = far_plane;
            this.nearClippingPlane = near_plane;
            this.calibration_matrix = calibration_matrix;
        }
        public float farClippingPlane ;
        public float nearClippingPlane ;
        public float[] calibration_matrix;

        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddFloat("nearClippingPlane", nearClippingPlane);
            builder.AddFloat("farClippingPlane", farClippingPlane);
            builder.AddFloatArray("calibration_matrix", calibration_matrix);
        }

        public override bool IsValid() => true;

    }
}