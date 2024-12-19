using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPoseData", menuName = "Pose Data")]
public class PoseData : ScriptableObject
{
    public List<Pose> poses = new List<Pose>();
}
