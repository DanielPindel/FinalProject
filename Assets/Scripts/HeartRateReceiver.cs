using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;
using System;
using TMPro;

public class HeartRateReceiver : MonoBehaviour
{
    FirebaseFirestore db;
    CollectionReference heartRateCollection;
    DocumentReference heartRates;
    ListenerRegistration listenerRegistration;
    public TextMeshProUGUI heartRateText;
    public int heartRate;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        heartRateCollection = db.Collection("heartRates");
        heartRates = heartRateCollection.Document("latestHeartRate");

        ListenToHeartRateUpdates();
    }

    void ListenToHeartRateUpdates()
    {
        listenerRegistration = heartRates.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                Dictionary<string, object> data = snapshot.ToDictionary();

                if (data.ContainsKey("heartRate") && data.ContainsKey("timestamp"))
                {
                    heartRate = Convert.ToInt32(data["heartRate"]);
                    string timestamp = data["timestamp"].ToString();

                    heartRateText.text = $"{heartRate} bpm";
                }
                else
                {
                    Debug.LogWarning("Document exists but is missing required fields");
                }
            }
            else
            {
                Debug.LogWarning("Document does not exist");
            }
        });
    }

    public int GetHeartRate()
    {
        return heartRate;
    }
}
