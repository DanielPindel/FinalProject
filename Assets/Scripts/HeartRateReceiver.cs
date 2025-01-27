using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;

public class HeartRateReceiver : MonoBehaviour
{
    FirebaseFirestore db;
    CollectionReference heartRateCollection;
    DocumentReference heartRates;
    ListenerRegistration listenerRegistration;
    public TextMeshProUGUI heartRateText;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        heartRateCollection = db.Collection("heartRates");
        heartRates = heartRateCollection.Document("latestHeartRate");

        ListenToHeartRateUpdates();
    }

    void Update()
    {
        
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
                    int heartRate = Convert.ToInt32(data["heartRate"]);
                    string timestamp = data["timestamp"].ToString();

                    //Debug.Log($"Heart Rate Updated: {heartRate} bpm");
                    //Debug.Log($"Timestamp: {timestamp}");

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
}
