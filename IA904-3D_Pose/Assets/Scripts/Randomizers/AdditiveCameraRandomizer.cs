using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

[Serializable]
[AddRandomizerMenu("Additive Camera Randomizer")]
public class AdditiveCameraRandomizer : Randomizer
{
    public FloatParameter focalLenght_percentage = new() { value = new NormalSampler() };
    public Vector2Parameter sensorSize_percentage = new() { x = new NormalSampler(), y = new NormalSampler() };

    Dictionary<AdditiveCameraTag, float> originalFocalLenght = new();
    Dictionary<AdditiveCameraTag, Vector2> originalSensorSize = new();

    IEnumerable<AdditiveCameraTag> iterationTags;

    // Run this every randomization iteration
    protected override void OnIterationStart()
    {
        originalFocalLenght.Clear();
        originalSensorSize.Clear();

        // Get all MyLightRandomizerTag's in the scene
        iterationTags = tagManager.Query<AdditiveCameraTag>();
        foreach (AdditiveCameraTag tag in iterationTags)
        {
            Camera camera = tag.GetComponent<Camera>();

            originalFocalLenght[tag] = camera.focalLength;
            originalSensorSize[tag] = camera.sensorSize;

            float focalLenghtAdd = camera.focalLength*focalLenght_percentage.Sample();
            camera.focalLength += focalLenghtAdd;

            if(camera.focalLength < 0)
            {
                camera.focalLength = 0;
            }

            Vector2 sensorSizeAdd = camera.sensorSize*sensorSize_percentage.Sample();
            Vector2 sensorSize = camera.sensorSize+sensorSizeAdd;

            if(sensorSize.x < 0)
            {
                sensorSize.x = 0;
            }
            if(sensorSize.y < 0)
            {
                sensorSize.y = 0;
            }

            camera.sensorSize = sensorSize;
        }
    }

    protected override void OnIterationEnd()
    {
        foreach (AdditiveCameraTag tag in iterationTags)
        {
            Camera camera = tag.GetComponent<Camera>();

            camera.focalLength = originalFocalLenght[tag];
            camera.sensorSize = originalSensorSize[tag];
        }
    }
}