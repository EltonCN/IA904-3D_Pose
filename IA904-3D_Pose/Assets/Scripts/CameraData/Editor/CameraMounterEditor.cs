using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CameraMounter))]
public class ObjectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraMounter myScript = (CameraMounter)target;
        if(GUILayout.Button("Mount"))
        {
            myScript.Mount();
        }
    }
}