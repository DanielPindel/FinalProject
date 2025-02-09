using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public HeartRateReceiver heartRateReceiver;
    public GameObject targetModel; // The model showing recorded poses
    public GameObject playerModel; // The model controlled by the player
    public float rotationThreshold = 15f; // Allowable rotational difference (in degrees)

    public float matchScore; // Overall match score (0-1)
    public float matchPercentage; // Percentage accuracy
    public float targetMatchPercentage = 90f; // Required percentage accuracy

    public TextMeshProUGUI matchPercentageText;
    public TextMeshProUGUI scoreText;

    public int baseBPM = 60;
    private int currentBPM;
    private float heartRate;
    private int hrSeqCounter = 0;
    public int gameLength;
    public int volume;
    private int currentSequence = 0;
    private int currentPoseIndex = 0;
    private int totalPoses = 4;
    private int score = 0;
    private float beatInterval = 1f;

    public List<string> includedJoints;
    private List<int> poseSequence = new List<int>();
    private List<Transform> targetJoints;
    private List<Transform> playerJoints;

    private Dictionary<Transform, Transform> includedJointPairs = new Dictionary<Transform, Transform>();
    private Dictionary<Transform, bool> jointMatchResults = new Dictionary<Transform, bool>();

    private PoseRecorder poseRecorder;
    private PoseData poseData;

    public AudioSource soundEffectSource;
    public AudioClip beatSound;
    public AudioClip muffledBeatSound;

    public Image popUpMessage;
    public Sprite getReadySprite;
    public Sprite yourTurnSprite;
    public Sprite finishSprite;
    public Sprite threeSprite;
    public Sprite twoSprite;
    public Sprite oneSprite;
    public Sprite speedUpSprite;
    public Sprite slowDownSprite;

    private bool isPaused = false;
    public GameObject pauseMenuCanvas;
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;

    // Weights for different joints
    private Dictionary<string, float> jointWeights = new Dictionary<string, float>
    {
        { "Hips", 0.25f },
        { "LeftShoulder", 2.0f },
        { "LeftArm", 3.0f },
        { "LeftForeArm", 3.0f },
        { "RightShoulder", 2.0f },
        { "RightArm", 3.0f },
        { "RightForeArm", 3.0f },
        { "RightUpLeg", 2.0f },
        { "RightLeg", 2.0f },
        { "LeftUpLeg", 2.0f },
        { "LeftLeg", 2.0f },
        { "RightHand", 0.0f },
        { "LeftHand", 0.0f },
        { "RightFoot", 0.25f },
        { "LeftFoot", 0.25f },
        { "Spine", 0.25f },
        { "Neck", 0.25f }
    };

    private void Awake()
    {
        heartRateReceiver = GetComponent<HeartRateReceiver>();
        popUpMessage.gameObject.SetActive(false);
    }

    void Start()
    {
        volume = PlayerPrefs.GetInt("Volume", 20);
        gameLength = PlayerPrefs.GetInt("GameDuration", 12);

        volumeSlider.value = volume;
        UpdateVolumeText();

        InitializeIncludedJoints();

        soundEffectSource.volume = volume / 100f;

        currentBPM = baseBPM;
        beatInterval = 60f / currentBPM;

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

        StartCoroutine(Countdown());
    }

    void Update()
    {
        matchPercentageText.text = Convert.ToInt32(matchPercentage * 100f).ToString() + "%";
        scoreText.text = score.ToString();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseMenuCanvas.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenuCanvas.SetActive(false);
    }

    public void OnVolumeChanged()
    {
        PlayerPrefs.SetInt("Volume", (int)volumeSlider.value);
        soundEffectSource.volume = volumeSlider.value / 100f;
        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        volumeText.text = volumeSlider.value.ToString();
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator Countdown()
    {
        yield return ShowPopUpMessage(getReadySprite, 2);

        yield return ShowPopUpMessage(threeSprite, 1);

        yield return ShowPopUpMessage(twoSprite, 1);

        yield return ShowPopUpMessage(oneSprite, 1);

        StartCoroutine(GameLoop());
    }

    private IEnumerator ShowPopUpMessage(Sprite sprite, int time)
    {
        popUpMessage.sprite = sprite;

        if (sprite != null)
        {
            RectTransform rt = popUpMessage.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);
            }
        }

        popUpMessage.gameObject.SetActive(true);

        StartCoroutine(HidePopUp(time * beatInterval));

        for (int i = 0; i < time; i++)
        {
            PlaySound(muffledBeatSound);
            yield return new WaitForSeconds(beatInterval);
        }
    }

    private IEnumerator HidePopUp(float delay)
    {
        yield return new WaitForSeconds(delay);

        popUpMessage.gameObject.SetActive(false);
    }

    private IEnumerator GameLoop()
    {
        while(currentSequence < gameLength)
        {
            GenerateRandomPoseIndexes();

            yield return StartCoroutine(PlayPoseSequence());

            yield return ShowPopUpMessage(yourTurnSprite, 4);

            yield return StartCoroutine(PlayerSequence());

            currentSequence++;
            hrSeqCounter++;

            if (hrSeqCounter >= 3 && currentSequence != gameLength)
            {
                float previousBPM = currentBPM;
                AdjustTempo();

                if (currentBPM > previousBPM)
                {
                    yield return ShowPopUpMessage(speedUpSprite, 4);
                }
                else if (currentBPM < previousBPM)
                {
                    yield return ShowPopUpMessage(slowDownSprite, 4);
                }

                hrSeqCounter = 0;
            }

            for (int i = 0; i < 4; i++)
            {
                PlaySound(muffledBeatSound);
                yield return new WaitForSeconds(beatInterval);
            }
        }

        Debug.Log("Game Over");

        yield return ShowPopUpMessage(finishSprite, 5);
        SceneManager.LoadScene("MainMenu");
    }

    private void AdjustTempo()
    {
        float tempoScalingFactor = 1.0f;

        heartRate = heartRateReceiver.GetHeartRate();

        if (heartRate > 0)
        {
            tempoScalingFactor = Mathf.Clamp(80f / heartRate, 0.5f, 2.0f);
        }

        currentBPM = Convert.ToInt32(baseBPM * tempoScalingFactor);
        beatInterval = 60f / currentBPM;

        Debug.Log($"Adjusted tempo: {currentBPM} BPM, Beat Interval: {beatInterval} seconds");
    }

    private void GenerateRandomPoseIndexes()
    {
        poseSequence.Clear();

        int previousIndex = -1;

        for (int i = 0; i < totalPoses; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = UnityEngine.Random.Range(0, poseData.poses.Count);
            } 
            while (randomIndex == previousIndex);

            poseSequence.Add(randomIndex);
            previousIndex = randomIndex;
        }
    }

    private IEnumerator PlayPoseSequence()
    {
        for(int i = 0; i < totalPoses; i++)
        {
            poseRecorder.poseIndex = poseSequence[i];
            poseRecorder.RecreatePose();

            PlaySound(beatSound);

            yield return new WaitForSeconds(beatInterval);
        }
    }

    private IEnumerator PlayerSequence()
    {
        for (int i = 0; i < totalPoses; i++)
        {
            PlaySound(beatSound);
            StartCoroutine(DelayedPoseEvaluation(poseSequence[i], 0.5f));

            yield return new WaitForSeconds(beatInterval);
        }
        PlaySound(muffledBeatSound);
    }

    private IEnumerator DelayedPoseEvaluation(int poseIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        EvaluatePlayerPose(poseIndex);
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

            float weight = GetJointWeight(jointName);
            totalWeight += weight;

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
                jointMatchResults[targetJoint] = false;
                continue;
            }

            Pose.JointPosition targetJointPosition = targetPose.jointPositions.Find(jp => jp.jointType.ToString() == jointName);
            if (targetJointPosition.Equals(default(Pose.JointPosition)))
            {
                Debug.LogWarning($"Joint {jointName} not found");
                continue;
            }

            float rotationDifference = Quaternion.Angle(targetJointPosition.rotation, playerJoint.localRotation);
            bool isRotationMatching = rotationDifference <= rotationThreshold;

            if (!isRotationMatching)
            {
                MarkJointAndDescendantsIncorrect(playerJoint, jointIncorrect);
            }

            jointMatchResults[targetJoint] = isRotationMatching;

            if (isRotationMatching)
            {
                matchingScore += weight;
            }
        }

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
            newPoseIndex = UnityEngine.Random.Range(0, poseRecorder.poseData.poses.Count);
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

    private void PlaySound(AudioClip clip)
    {
        soundEffectSource.PlayOneShot(clip);
    }
}