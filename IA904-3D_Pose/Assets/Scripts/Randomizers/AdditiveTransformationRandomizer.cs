using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.TextCore;

[Serializable]
[AddRandomizerMenu("Additive Transformation Randomizer")]
public class AdditiveTransformationRandomizer : Randomizer
{
    public FloatParameter positionParameter = new() { value = new UniformSampler(-1, 1) };
    public FloatParameter rotationParameter = new() { value = new UniformSampler(-1, 1) };


    Dictionary<AdditiveTransformationTag, Vector3> originalPosition = new();
    Dictionary<AdditiveTransformationTag, Quaternion> originalRotation  = new();

    IEnumerable<AdditiveTransformationTag> iterationTags;

    // Run this every randomization iteration
    protected override void OnIterationStart()
    {
        originalPosition.Clear();
        originalRotation.Clear();

        // Get all MyLightRandomizerTag's in the scene
        iterationTags = tagManager.Query<AdditiveTransformationTag>();
        foreach (var tag in iterationTags)
        {
            originalPosition[tag] = new Vector3(tag.transform.position.x, tag.transform.position.y, tag.transform.position.z);
            originalRotation[tag] = tag.transform.rotation;

            Vector3 additivePosition = new()
            {
                x = positionParameter.Sample() * tag.positionScale.x,
                y = positionParameter.Sample() * tag.positionScale.y,
                z = positionParameter.Sample() * tag.positionScale.z
            };

            tag.transform.position += additivePosition;
            

            Vector3 additiveRotation = new()
            {
                x = rotationParameter.Sample() * tag.rotationScale.x,
                y = rotationParameter.Sample() * tag.rotationScale.y,
                z = rotationParameter.Sample() * tag.rotationScale.z
            };

            Vector3 rotation = tag.transform.rotation.eulerAngles;
            rotation += additiveRotation;
            tag.transform.rotation = Quaternion.Euler(rotation);
        }
    }

    protected override void OnIterationEnd()
    {
        foreach (var tag in iterationTags)
        {
            tag.transform.position = originalPosition[tag];
            tag.transform.rotation = originalRotation[tag];
        }
    }
}