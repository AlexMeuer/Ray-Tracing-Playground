using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RayTracingMaster))]
public class RayTracingInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Reset Sampling"))
        {
            ((RayTracingMaster) serializedObject.targetObject).currentSample = 0;
        }
    }
}
