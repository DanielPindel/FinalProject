using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseRecorder))]
public class PoseRecorderEditor : Editor
{
    private SerializedProperty characterJointsProperty;
    private SerializedProperty poseDataProperty;
    private SerializedProperty poseIndexProperty;

    private void OnEnable()
    {
        // Cache the SerializedProperty references
        characterJointsProperty = serializedObject.FindProperty("characterJoints");
        poseDataProperty = serializedObject.FindProperty("poseData");
        poseIndexProperty = serializedObject.FindProperty("poseIndex");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Draw the Character Joints and Pose Data fields
        EditorGUILayout.PropertyField(characterJointsProperty, true);
        EditorGUILayout.PropertyField(poseDataProperty, true);

        // Add a space in the Inspector
        EditorGUILayout.Space();

        // Add the Record Pose button
        if (GUILayout.Button("Record Pose"))
        {
            ((PoseRecorder)target).RecordPose();
        }

        // Add a space in the Inspector
        EditorGUILayout.Space();

        // Draw the Pose Index field
        EditorGUILayout.PropertyField(poseIndexProperty);

        // Add a space in the Inspector
        EditorGUILayout.Space();

        // Add the Recreate Pose button
        if (GUILayout.Button("Recreate Pose"))
        {
            ((PoseRecorder)target).RecreatePose();
        }

        // Add a warning if the pose index is out of range
        PoseRecorder poseRecorder = (PoseRecorder)target;
        if (poseRecorder.poseData != null && (poseRecorder.poseIndex < 0 || poseRecorder.poseIndex >= poseRecorder.poseData.poses.Count))
        {
            EditorGUILayout.HelpBox($"Invalid pose index: {poseRecorder.poseIndex}. Valid range is 0 to {poseRecorder.poseData.poses.Count - 1}.", MessageType.Error);
        }

        // Apply changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}