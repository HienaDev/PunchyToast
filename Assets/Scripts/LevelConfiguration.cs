using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "ToastGame/LevelConfiguration")]
public class LevelConfiguration : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber; // e.g., 1 for Level1

    [Header("Star Thresholds (Time in seconds)")]
    public float fiveStarTime = 30f;
    public float fourStarTime = 45f;
    public float threeStarTime = 60f;
    public float twoStarTime = 90f;
    // Anything higher is 1 star

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