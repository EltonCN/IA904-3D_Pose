using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

public class AdditiveTransformationTag : RandomizerTag 
{
    public Vector3 positionScale = Vector3.one;
    public Vector3 rotationScale = Vector3.one;

}

