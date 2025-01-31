using Microsoft.Azure.Kinect.BodyTracking;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using System.Collections;

public class ComparePose : MonoBehaviour
{
    public GameObject targetModel; // The model showing recorded poses
    public GameObject playerModel; // The model controlled by the player
    public float rotationThreshold = 15f; // Allowable rotational difference (in degrees)

    public float matchScore; // Overall match score (0-1)
    public float matchPercentage; // Percentage accuracy
    public float targetMatchPercentage = 90f; // Required percentage accuracy

    public TextMeshProUGUI matchPercentageText;
    public TextMeshProUGUI scoreText;

    public int bpm = 60;
    public int gameLength = 10;
    private int currentSequence = 0;
    private int currentPoseIndex = 0;
    private int totalPoses = 4;
    private int score = 0;
    private float beatInterval;

    public List<string> includedJoints;
    private List<int> poseSequence = new List<int>();
    private List<Transform> targetJoints;
    private List<Transform> playerJoints;

    private Dictionary<Transform, Transform> includedJointPairs = new Dictionary<Transform, Transform>();
    private Dictionary<Transform, bool> jointMatchResults = new Dictionary<Transform, bool>();

    private PoseRecorder poseRecorder;
    private PoseData poseData;

    // Weights for different joints
    private Dictionary<string, float> jointWeights = new Dictionary<string, float>
    {
        { "Hips", 1.0f },
        { "LeftShoulder", 1.5f },
        { "LeftArm", 2.0f },
        { "LeftForeArm", 2.0f },
        { "RightShoulder", 1.5f },
        { "RightArm", 2.0f },
        { "RightForeArm", 2.0f },
        { "RightUpLeg", 1.5f },
        { "RightLeg", 2.0f },
        { "LeftUpLeg", 1.5f },
        { "LeftLeg", 2.0f },
        { "RightHand", 0.25f },
        { "LeftHand", 0.25f },
        { "RightFoot", 0.25f },
        { "LeftFoot", 0.25f }
    };

    void Start()
    {
        InitializeIncludedJoints();

        beatInterval = 60f / bpm;

        poseRecorder = targetModel.GetComponent<PoseRecorder>();
        if (poseRecorder == null)
        {
            Debug.LogError("PoseRecorder not found");
            return;
        }

        poseData = poseRecorder.poseData;
        if (poseData == null)
        {
            Debug.LogError("PoseData not found");
        }

        StartCoroutine(GameLoop());
    }

    void Update()
    {
        matchPercentageText.text = (matchPercentage * 100f).ToString("F2") + "%";
        scoreText.text = score.ToString();
    }

    private IEnumerator GameLoop()
    {
        while(currentSequence < gameLength)
        {
            GenerateRandomPoseIndexes();

            yield return StartCoroutine(PlayPoseSequence());

            yield return StartCoroutine(PlayerSequence());

            currentSequence++;
        }

        Debug.Log("Game Over");
    }

    private void GenerateRandomPoseIndexes()
    {
        poseSequence.Clear();

        for (int i = 0; i < totalPoses; i++)
        {
            int randomIndex = Random.Range(0, poseData.poses.Count);
            poseSequence.Add(randomIndex);
        }
    }

    private IEnumerator PlayPoseSequence()
    {
        for(int i = 0; i < totalPoses; i++)
        {
            poseRecorder.poseIndex = poseSequence[i];
            poseRecorder.RecreatePose();

            yield return new WaitForSeconds(beatInterval);
        }
    }

    private IEnumerator PlayerSequence()
    {
        for (int i = 0; i < totalPoses; i++)
        {
            yield return new WaitForSeconds(beatInterval);
            EvaluatePlayerPose(poseSequence[i]);
        }
    }

    private void EvaluatePlayerPose(int poseIndex)
    {
        Pose targetPose = poseData.poses[poseIndex];

        matchPercentage = CompareRotations(targetPose);

        score += Mathf.RoundToInt(100 * matchPercentage);
    }

    private void InitializeIncludedJoints()
    {
        List<Transform> targetJoints = GetAllChildTransforms(targetModel.transform);
        List<Transform> playerJoints = GetAllChildTransforms(playerModel.transform);

        if (targetJoints.Count != playerJoints.Count)
        {
            Debug.LogError("Model hierarchies do not match!");
            return;
        }

        includedJointPairs.Clear();

        for (int i = 0; i < targetJoints.Count; i++)
        {
            Transform targetJoint = targetJoints[i];
            Transform playerJoint = playerJoints[i];

            if (IsJointIncluded(targetJoint.name))
            {
                includedJointPairs.Add(targetJoint, playerJoint);
            }
        }
    }

    private float CompareRotations(Pose targetPose)
    {
        float totalWeight = 0f;
        float matchingScore = 0f;

        jointMatchResults.Clear();

        Dictionary<Transform, bool> jointIncorrect = new Dictionary<Transform, bool>();

        foreach (var jointPair in includedJointPairs)
        {
            Transform targetJoint = jointPair.Key;
            Transform playerJoint = jointPair.Value;

            string jointName = targetJoint.name;

            // Determine weight for the current joint
            float weight = GetJointWeight(jointName);

            // Check if any ancestor of this joint is incorrect
            bool isAncestorIncorrect = false;
            Transform current = playerJoint;
            while (current != null)
            {
                if (jointIncorrect.ContainsKey(current) && jointIncorrect[current])
                {
                    isAncestorIncorrect = true;
                    break;
                }
                current = current.parent;
            }

            if (isAncestorIncorrect)
            {
                // If any ancestor is incorrect, mark this joint as incorrect
                jointMatchResults[targetJoint] = false;
                continue;
            }

            Pose.JointPosition targetJointPosition = targetPose.jointPositions.Find(jp => jp.jointType.ToString() == jointName);
            if (targetJointPosition.Equals(default(Pose.JointPosition)))
            {
                Debug.LogWarning($"Joint {jointName} not found");
                continue;
            }

            Debug.Log($"Comparing {jointName}: Target Rotation = {targetJointPosition.rotation}, Player Rotation = {playerJoint.localRotation}");

            // Compare rotations
            float rotationDifference = Quaternion.Angle(targetJointPosition.rotation, playerJoint.localRotation);
            bool isRotationMatching = rotationDifference <= rotationThreshold;

            if (!isRotationMatching)
            {
                Debug.Log($"{jointName} marked as incorrect. Rotation difference: {rotationDifference}");
                MarkJointAndDescendantsIncorrect(playerJoint, jointIncorrect);
            }

            jointMatchResults[targetJoint] = isRotationMatching;

            // Apply weight if the rotation matches
            if (isRotationMatching)
            {
                matchingScore += weight;
            }

            totalWeight += weight;
        }

        // Return the weighted match percentage
        return totalWeight > 0f ? matchingScore / totalWeight : 0f;
    }

    // Helper method to mark a joint and all its descendants as incorrect
    private void MarkJointAndDescendantsIncorrect(Transform joint, Dictionary<Transform, bool> jointIncorrect)
    {
        if (joint == null) return;

        // Mark the current joint as incorrect
        jointIncorrect[joint] = true;

        // Recursively mark all children as incorrect
        foreach (Transform child in joint)
        {
            MarkJointAndDescendantsIncorrect(child, jointIncorrect);
        }
    }

    private float GetJointWeight(string jointName)
    {
        foreach (var pair in jointWeights)
        {
            if (jointName.Contains(pair.Key))
            {
                return pair.Value;
            }
        }

        return 1.0f; // Default weight
    }

    private bool IsJointIncluded(string jointName)
    {
        return includedJoints.Contains(jointName);
    }

    private List<Transform> GetAllChildTransforms(Transform root)
    {
        List<Transform> transforms = new List<Transform>();
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            transforms.Add(current);

            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }

        // Remove the root transform itself
        transforms.RemoveAt(0);

        return transforms;
    }

    private void LoadRandomPose()
    {
        if (poseRecorder == null || poseRecorder.poseData == null || poseRecorder.poseData.poses.Count == 0)
        {
            Debug.LogError("No poses available to load!");
            return;
        }

        // Select a random pose index different from the current one
        int newPoseIndex;

        do
        {
            newPoseIndex = Random.Range(0, poseRecorder.poseData.poses.Count);
        } 
        while (newPoseIndex == currentPoseIndex);

        currentPoseIndex = newPoseIndex;

        // Apply the selected pose to the targetModel
        poseRecorder.poseIndex = currentPoseIndex;
        poseRecorder.RecreatePose();

        Debug.Log($"Loaded pose {currentPoseIndex}");
    }

    void OnDrawGizmos()
    {
        if (jointMatchResults == null || jointMatchResults.Count == 0) return;

        foreach (var jointResult in jointMatchResults)
        {
            Transform joint = jointResult.Key;
            bool isMatching = jointResult.Value;

            Gizmos.color = isMatching ? Color.green : Color.red;
            Gizmos.DrawSphere(joint.position, 0.01f);
        }
    }
}