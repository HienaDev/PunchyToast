using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "ToastGame/LevelConfiguration")]
public class LevelConfiguration : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;

    [Header("Hover Logic (Toaster)")]
    public float hoverTime = 1.5f;
    public float minPreHoverDelay = 0f;
    public float maxPreHoverDelay = 0.1f;
    public float driftFactor = 0.2f;

    [Header("Star Thresholds (Time in seconds)")]
    public float fiveStarTime = 30f;
    public float fourStarTime = 45f;
    public float threeStarTime = 60f;
    public float twoStarTime = 90f;

    [System.Serializable]
    public struct ClientData
    {
        public JamFlavor jamFlavor;
        public string customLetter;
    }

    [System.Serializable]
    public struct Wave
    {
        public List<ClientData> clientsInWave;
    }

    public List<Wave> waves = new List<Wave>();
}