using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

#region Subclasses
namespace IA904_3DPose
{
    public class Annotation
    {
        [JsonProperty("@type")]
        public string type { get; set; }
        public string id { get; set; }
        public string sensorId { get; set; }
        public string description { get; set; }
        public List<Keypoint> keypoints { get; set; }
        public string templateId { get; set; }
        public List<Value> values { get; set; }
        public List<Metadata> metadata { get; set; }
        public double? nearClippingPlane { get; set; }
        public double? farClippingPlane { get; set; }
        public List<double> calibration_matrix { get; set; }
        public double? aperture { get; set; }
        public List<double> sensorSize { get; set; }
        public List<double> lensShift { get; set; }
    }

    public class Capture
    {
        [JsonProperty("@type")]
        public string type { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public List<double> position { get; set; }
        public List<double> rotation { get; set; }
        public List<double> velocity { get; set; }
        public List<double> acceleration { get; set; }
        public string filename { get; set; }
        public string imageFormat { get; set; }
        public List<double> dimension { get; set; }
        public string projection { get; set; }
        public List<double> matrix { get; set; }
        public List<Annotation> annotations { get; set; }

        private const string TYPE_INFO = "type.unity.com/unity.solo.HumanMetadataAnnotation";
        private const string TYPE_KEYPOINT_2D = "type.unity.com/unity.solo.KeypointAnnotation";
        private const string TYPE_KEYPOINT_3D = "type.unity.com/unity.solo.Keypoint3dAnnotation";

        public string GetInfo()
        {
            return annotations.FirstOrDefault(a => a.type == TYPE_INFO).metadata.FirstOrDefault()?.ToString();
        }

        public string GetKeypoints2D()
        {
            return $"{id}{annotations.FirstOrDefault(a => a.type == TYPE_KEYPOINT_2D)?.values?.FirstOrDefault()}";
        }

        public string GetKeypoints3D()
        {
            return $"{id}{annotations.FirstOrDefault(a => a.type == TYPE_KEYPOINT_3D)?.keypoints?.FirstOrDefault()}";
        }

        public string GetHead2D()
        {
            return annotations.FirstOrDefault(a => a.type == TYPE_KEYPOINT_2D)?.values?.FirstOrDefault().GetHead();
        }

        public string GetHead3D()
        {
            return annotations.FirstOrDefault(a => a.type == TYPE_KEYPOINT_3D)?.keypoints?.FirstOrDefault().GetHead();
        }
    }

    public class Keypoint
    {
        public int instanceId { get; set; }
        public List<Keypoint> keypoints { get; set; }
        public string label { get; set; }
        public List<double> location { get; set; }
        public List<double> orientation { get; set; }
        public int index { get; set; }
        public List<double> cameraCartesianLocation { get; set; }
        public int state { get; set; }

        public override string ToString()
        {
            string result = string.Empty;
            foreach (var keypoint in keypoints)
            {
                if (keypoint.location != null && keypoint.location.Count > 0)
                    result += $",{string.Join(",", keypoint.location.Select(l => l.ToString(CultureInfo.InvariantCulture)))}";
            }
            return result;
        }

        public string GetHead()
        {
            return string.Join(",", keypoints.Select(k => $"{k.label}_x,{k.label}_y,{k.label}_z"));
        }
    }

    public class Metadata
    {
        public int instanceId { get; set; }
        public string age { get; set; }
        public string height { get; set; }
        public string weight { get; set; }
        public string sex { get; set; }
        public string ethnicity { get; set; }
        public string bodyMeshTag { get; set; }
        public string hairMeshTag { get; set; }
        public string faceVatTag { get; set; }
        public string primaryBlendVatTag { get; set; }
        public string secondaryBlendVatTag { get; set; }
        public string bodyMaterialTag { get; set; }
        public string faceMaterialTag { get; set; }
        public string eyeMaterialTag { get; set; }
        public string hairMaterialTag { get; set; }
        public object templateSkeleton { get; set; }
        public List<string> clothingTags { get; set; }
        public List<string> clothingMaterialTags { get; set; }

        public override string ToString()
        {
            return $"{age},{height.Replace(",", ".")},{weight.Replace(",", ".")},{sex},{ethnicity}";
        }
    }

    public class Metric
    {
        [JsonProperty("@type")]
        public string type { get; set; }
        public string id { get; set; }
        public string sensorId { get; set; }
        public string annotationId { get; set; }
        public string description { get; set; }
        public int value { get; set; }
    }

    public class Value
    {
        public int instanceId { get; set; }
        public int labelId { get; set; }
        public string pose { get; set; }
        public List<Keypoint> keypoints { get; set; }

        public override string ToString()
        {
            string result = string.Empty;
            Debug.Log($"2D: {keypoints.Count}");
            foreach (var keypoint in keypoints)
            {
                if (keypoint.location != null && keypoint.location.Count > 0)
                    result += $",{string.Join(",", keypoint.location.Select(l => l.ToString(CultureInfo.InvariantCulture)))}";
            }
            return result;
        }

        public string GetHead()
        {
            return string.Join(",", keypoints.Select(k => $"{k.index}_x,{k.index}_y"));
        }
    }
    #endregion

    public class FrameData
    {
        public int frame { get; set; }
        public int sequence { get; set; }
        public int step { get; set; }
        public double timestamp { get; set; }
        public List<Capture> captures { get; set; }
        public List<Metric> metrics { get; set; }

        public override string ToString()
        {
            return $"{sequence},{captures.FirstOrDefault()?.GetInfo()}";
        }

        public List<string> ToDataFrame2D(string scenario)
        {
            return captures.Select(capture => $"{scenario},{sequence},{capture.GetKeypoints2D()}").ToList();
        }

        public List<string> ToDataFrame3D(string scenario)
        {
            return captures.Select(capture => $"{scenario},{sequence},{capture.GetKeypoints3D()}").ToList();
        }

        public string GetHead()
        {
            return $"scenario,sequence,age,height,weight,sex,ethnicity";
        }

        public string GetHead2D()
        {
            return $"scenario,sequence,camera,{captures.FirstOrDefault().GetHead2D()}";
        }

        public string GetHead3D()
        {
            return $"scenario,sequence,camera,{captures.FirstOrDefault().GetHead3D()}";
        }
    }
}
