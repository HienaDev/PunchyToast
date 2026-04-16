using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public enum JamFlavor { None, Butter, StrawberryJam, GrapeJam, PeanutButter, Random }
public class JamDecider : MonoBehaviour
{
    public static JamDecider Instance;

    [System.Serializable]
    public struct JamType
    {
        public string name;
        public JamFlavor flavor; // Match this to the enum
        public Color jamColor;
        public Transform dippingStation;
    }

    [Header("Settings")]
    public GameObject armPrefab;
    public List<JamType> allAvailableJams; // The full list of all 4 possible jams
    public List<JamType> activeJams;      // The ones currently allowed in this level

    [Header("Animation")]
    public float dipDepth = 0.8f;
    public float dipDuration = 0.15f;
    public float zOffset = 2.0f;

    public int currentJamIndex = -1;

    [SerializeField] private GameObject butterFist;
    [SerializeField] private GameObject stawberryFist;
    [SerializeField] private GameObject grapeFist;
    [SerializeField] private GameObject peanutFist;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Called by ClientManager at Level Start
    public void SetupLevelJams(HashSet<JamFlavor> requiredFlavors)
    {
        activeJams = new List<JamType>();

        // Loop through our "Master List" and pick the ones the level needs
        foreach (var jam in allAvailableJams)
        {
            if (requiredFlavors.Contains(jam.flavor))
            {
                activeJams.Add(jam);
                // Enable the jar visual in the scene
                jam.dippingStation.gameObject.SetActive(true);
            }
            else
            {
                // Disable jars not in this level
                jam.dippingStation.gameObject.SetActive(false);
            }
        }
        SelectJam(0);
    }

    void Update()
    {
        // Use activeJams.Count so player can't press 4 if only 2 jams exist
        for (int i = 0; i < activeJams.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectJam(i);
            }
        }
    }

    void SelectJam(int index)
    {
        if (index == currentJamIndex) return;
        currentJamIndex = index;
        PerformDipAnimation(activeJams[index]);

        // Disable all fists first
        butterFist.SetActive(false);
        stawberryFist.SetActive(false);
        grapeFist.SetActive(false);
        peanutFist.SetActive(false);

        // Enable the correct fist based on flavor
        switch (activeJams[index].flavor)
        {
            case JamFlavor.Butter:
                butterFist.SetActive(true);
                break;
            case JamFlavor.StrawberryJam:
                stawberryFist.SetActive(true);
                break;
            case JamFlavor.GrapeJam:
                grapeFist.SetActive(true);
                break;
            case JamFlavor.PeanutButter:
                peanutFist.SetActive(true);
                break;
        }

    }

    // --- Helper Methods using activeJams ---
    public string GetCurrentJamName() => activeJams[currentJamIndex].flavor.ToString();
    public Color GetCurrentJamColor() => activeJams[currentJamIndex].jamColor;

    public Color GetColorFromFlavor(JamFlavor flavor)
    {
        foreach (var j in allAvailableJams)
            if (j.flavor == flavor) return j.jamColor;
        return Color.white;
    }

    void PerformDipAnimation(JamType jam)
    {
        // 1. Calculate the spawn position behind the station (Negative Z)
        // We use the station's position and subtract the offset on the Z axis
        Vector3 spawnPos = jam.dippingStation.position + new Vector3(0, 0, -zOffset);

        // 2. Spawn pointing toward the station (Rotation 0,0,0 points toward Positive Z)
        GameObject dippingArm = Instantiate(armPrefab, spawnPos, Quaternion.identity);

        float screenX = Camera.main.WorldToViewportPoint(dippingArm.transform.position).x;

        if (screenX > 0.5f)
        {
            dippingArm.transform.localScale = Vector3.Scale(dippingArm.transform.localScale, new Vector3(-1, 1, 1));
        }

        Vector3 originalScale = dippingArm.transform.localScale;
        dippingArm.transform.localScale = Vector3.zero;

        Sequence dipSeq = DOTween.Sequence();

        // 3. Animation Steps (Moving on Z instead of Y)
        // Move "Forward" to station position + dipDepth
        float targetZ = jam.dippingStation.position.z + dipDepth;

        dipSeq.Append(dippingArm.transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutBack));

        // Punch forward into the jam
        dipSeq.Append(dippingArm.transform.DOMoveZ(targetZ, dipDuration).SetEase(Ease.InQuad));

        // Pull back to starting spawn Z
        dipSeq.Append(dippingArm.transform.DOMoveZ(spawnPos.z, dipDuration).SetEase(Ease.OutQuad));

        dipSeq.Append(dippingArm.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack));

        dipSeq.OnComplete(() => Destroy(dippingArm));
    }





}