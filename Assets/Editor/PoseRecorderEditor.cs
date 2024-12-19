using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseRecorder))]
public class PoseRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PoseRecorder poseRecorder = (PoseRecorder)target;
        if (GUILayout.Button("Record Pose"))
        {
            poseRecorder.RecordPose();
        }
    }
}
