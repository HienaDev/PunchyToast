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
        public bool simultaneousToast;
        public int toastsNeeded;

        // New Variables
        public bool isSlappable;
        public string slapString;

        public ClientData(bool dummy)
        {
            jamFlavor = JamFlavor.Butter;
            customLetter = "A";
            simultaneousToast = false;
            toastsNeeded = 1;
            isSlappable = false; // Default off
            slapString = "SLAP"; // Default string
        }
    }

    [System.Serializable]
    public struct Wave
    {
        public bool allowBottomRow;
        public bool allowTopRow;
        public List<ClientData> clientsInWave;

        public Wave(bool dummy)
        {
            allowBottomRow = true;
            allowTopRow = true;
            clientsInWave = new List<ClientData>();
        }
    }

    public List<Wave> waves = new List<Wave>();
}