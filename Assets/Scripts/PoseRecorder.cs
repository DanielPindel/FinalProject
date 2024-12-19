using System.Collections.Generic;
using UnityEngine;

public class PoseRecorder : MonoBehaviour
{
    public Transform[] characterJoints;
    public PoseData poseData;

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
                    position = joint.position
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
}

[System.Serializable]
public class Pose
{
    [System.Serializable]
    public struct JointPosition
    {
        public JointType jointType;
        public Vector3 position;
    }

    public List<JointPosition> jointPositions = new List<JointPosition>();
}

public enum JointType
{
    Hips,
    LeftUpLeg,
    LeftLeg,
    LeftFoot,
    RightUpLeg,
    RightLeg,
    RightFoot,
    Spine,
    Spine1,
    Spine2,
    LeftShoulder,
    LeftArm,
    LeftForeArm,
    LeftHand,
    Neck,
    Head,
    RightShoulder,
    RightArm,
    RightForeArm,
    RightHand
}
