using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "ToastGame/LevelConfiguration")]
public class LevelConfiguration : ScriptableObject
{
    [System.Serializable]
    public struct ClientData
    {
        public JamFlavor jamFlavor; // Use the enum here!
        public string customLetter;
    }

    [System.Serializable]
    public struct Wave
    {
        public List<ClientData> clientsInWave;
    }

    public List<Wave> waves = new List<Wave>();
}