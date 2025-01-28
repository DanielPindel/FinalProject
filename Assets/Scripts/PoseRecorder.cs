using System.Collections.Generic;
using UnityEngine;

public class PoseRecorder : MonoBehaviour
{
    public Transform[] characterJoints;
    public PoseData poseData;

    [SerializeField]
    public int poseIndex = 0;

    public void RecordPose()
    {
        if (poseData == null)
        {
            Debug.LogError("No PoseData assigned. Assign a PoseData ScriptableObject.");
            return;
        }

        if (characterJoints == null || characterJoints.Length == 0)
        {
            Debug.LogError("No joints assigned to characterJoints!");
            return;
        }

        Pose newPose = new Pose();
        foreach (var joint in characterJoints)
        {
            if (System.Enum.TryParse(joint.name, out JointType jointType))
            {
                newPose.jointPositions.Add(new Pose.JointPosition
                {
                    jointType = jointType,
                    position = joint.position,
                    rotation = joint.rotation
                });
            }
            else
            {
                Debug.LogWarning($"Joint '{joint.name}' does not match any JointType. Skipping.");
            }
        }

        poseData.poses.Add(newPose);
        Debug.Log($"Pose Recorded. Total poses: {poseData.poses.Count}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(poseData);
#endif
    }

    public void RecreatePose()
    {
        if (poseData == null)
        {
            Debug.LogError("No PoseData assigned. Assign a PoseData ScriptableObject.");
            return;
        }

        if (poseData.poses.Count == 0)
        {
            Debug.LogError("No poses recorded in PoseData.");
            return;
        }

        if (poseIndex < 0 || poseIndex >= poseData.poses.Count)
        {
            Debug.LogError($"Invalid pose index: {poseIndex}. Valid range is 0 to {poseData.poses.Count - 1}.");
            return;
        }

        Pose poseToRecreate = poseData.poses[poseIndex];

        foreach (var jointPosition in poseToRecreate.jointPositions)
        {
            foreach (var joint in characterJoints)
            {
                if (joint.name == jointPosition.jointType.ToString())
                {
                    joint.position = jointPosition.position;
                    joint.rotation = jointPosition.rotation;
                    break;
                }
            }
        }

        Debug.Log($"Pose {poseIndex} recreated.");
    }
}

[System.Serializable]
public class Pose
{
    [System.Serializable]
    public struct JointPosition
    {
        public JointType jointType;
        public Vector3 position;
        public Quaternion rotation;
    }

    public List<JointPosition> jointPositions = new List<JointPosition>();
}

public enum JointType
{
    Hips,
    LeftUpperLeg,
    LeftLowerLeg,
    LeftFoot,
    RightUpperLeg,
    RightLowerLeg,
    RightFoot,
    Spine,
    Spine1,
    Spine2,
    LeftShoulder,
    LeftUpperArm,
    LeftLowerArm,
    LeftHand,
    Neck,
    Head,
    RightShoulder,
    RightUpperArm,
    RightLowerArm,
    RightHand
}
