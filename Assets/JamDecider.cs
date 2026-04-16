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
        public JamFlavor flavor;
        public Color jamColor;
        public Transform dippingStation;
    }

    [Header("Settings")]
    public GameObject armPrefab;
    public List<JamType> allAvailableJams;
    public List<JamType> activeJams;

    [Header("Animation & Cooldown")]
    public float dipDepth = 0.8f;
    public float dipDuration = 0.15f;
    public float zOffset = 2.0f;
    [SerializeField] private float dipCooldown = 1.0f; // New Variable
    private float lastDipTime;                        // New Variable

    public int currentJamIndex = -1;

    [SerializeField] private GameObject butterFist;
    [SerializeField] private GameObject stawberryFist;
    [SerializeField] private GameObject grapeFist;
    [SerializeField] private GameObject peanutFist;

    [SerializeField] private AudioClip[] dipSounds;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetupLevelJams(HashSet<JamFlavor> requiredFlavors)
    {
        activeJams = new List<JamType>();

        foreach (var jam in allAvailableJams)
        {
            if (requiredFlavors.Contains(jam.flavor))
            {
                activeJams.Add(jam);
                jam.dippingStation.gameObject.SetActive(true);
            }
            else
            {
                jam.dippingStation.gameObject.SetActive(false);
            }
        }
        SelectJam(0);
    }

    void Update()
    {
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
        // 1. Check if we are trying to switch to the jam we already have
        if (index == currentJamIndex) return;

        // 2. Cooldown Logic: Check if enough time has passed since the last DIP
        if (Time.time < lastDipTime + dipCooldown) return;

        // 3. Update timing and index
        lastDipTime = Time.time;
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

    public void SelectByFlavor(JamFlavor flavor)
    {
        for (int i = 0; i < activeJams.Count; i++)
        {
            if (activeJams[i].flavor == flavor)
            {
                SelectJam(i);
                return;
            }
        }
    }

    void PerformDipAnimation(JamType jam)
    {
        AudioManager.Instance.PlaySound(dipSounds, jam.dippingStation.position);

        Vector3 spawnPos = jam.dippingStation.position + new Vector3(0, 0, -zOffset);
        GameObject dippingArm = Instantiate(armPrefab, spawnPos, Quaternion.identity);

        float screenX = Camera.main.WorldToViewportPoint(dippingArm.transform.position).x;

        if (screenX > 0.5f)
        {
            dippingArm.transform.localScale = Vector3.Scale(dippingArm.transform.localScale, new Vector3(-1, 1, 1));
        }

        Vector3 originalScale = dippingArm.transform.localScale;
        dippingArm.transform.localScale = Vector3.zero;

        Sequence dipSeq = DOTween.Sequence();
        float targetZ = jam.dippingStation.position.z + dipDepth;

        dipSeq.Append(dippingArm.transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutBack));
        dipSeq.Append(dippingArm.transform.DOMoveZ(targetZ, dipDuration).SetEase(Ease.InQuad));
        dipSeq.Append(dippingArm.transform.DOMoveZ(spawnPos.z, dipDuration).SetEase(Ease.OutQuad));
        dipSeq.Append(dippingArm.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack));
        dipSeq.OnComplete(() => Destroy(dippingArm));
    }

    public string GetCurrentJamName() => activeJams[currentJamIndex].flavor.ToString();
    public Color GetCurrentJamColor() => activeJams[currentJamIndex].jamColor;

    public Color GetColorFromFlavor(JamFlavor flavor)
    {
        foreach (var j in allAvailableJams)
            if (j.flavor == flavor) return j.jamColor;
        return Color.white;
    }
}