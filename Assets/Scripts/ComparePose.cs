using Microsoft.Azure.Kinect.BodyTracking;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

public class ComparePose : MonoBehaviour
{
    public GameObject targetModel; // The model showing recorded poses
    public GameObject playerModel; // The model controlled by the player
    public float rotationThreshold = 15f; // Allowable rotational difference (in degrees)

    public float matchScore; // Overall match score (0-1)
    public float matchPercentage; // Percentage accuracy

    public TextMeshProUGUI matchPercentageText;

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
        { "LeftLeg", 2.0f }
    };

    public List<string> includedJoints;
    private List<Transform> targetJoints;
    private List<Transform> playerJoints;

    private Dictionary<Transform, Transform> includedJointPairs = new Dictionary<Transform, Transform>();
    private Dictionary<Transform, bool> jointMatchResults = new Dictionary<Transform, bool>();


    void Start()
    {
        InitializeIncludedJoints();
    }

    void Update()
    {
        matchScore = CompareRotations();
        matchPercentage = matchScore * 100f; // Convert to percentage
        
        matchPercentageText.text = matchPercentage.ToString("F2") + "%";
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

    private float CompareRotations()
    {
        float totalWeight = 0f;
        float matchingScore = 0f;
        jointMatchResults.Clear();

        foreach (var jointPair in includedJointPairs)
        {
            Transform targetJoint = jointPair.Key;
            Transform playerJoint = jointPair.Value;

            string jointName = targetJoint.name;

            // Determine weight for the current joint
            float weight = GetJointWeight(jointName);

            // Compare rotations
            float rotationDifference = Quaternion.Angle(targetJoint.localRotation, playerJoint.localRotation);
            bool isRotationMatching = rotationDifference <= rotationThreshold;

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