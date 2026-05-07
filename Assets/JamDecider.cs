using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Audio;

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

        // NEW: Number sprite for each flavor
        public GameObject numberSprite;
    }

    [Header("Settings")]
    public GameObject armPrefab;
    public List<JamType> allAvailableJams;
    public List<JamType> activeJams;

    [Header("Animation & Cooldown")]
    public float dipDepth = 0.8f;
    public float dipDuration = 0.15f;
    public float zOffset = 2.0f;
    [SerializeField] private float dipCooldown = 0.5f;
    private float lastDipTime = -10f;

    public int currentJamIndex = 0;

    [SerializeField] private GameObject butterFist;
    [SerializeField] private GameObject stawberryFist;
    [SerializeField] private GameObject grapeFist;
    [SerializeField] private GameObject peanutFist;

    [SerializeField] private AudioMixer sfxMixer;
    [SerializeField] private AudioClip[] dipSounds;

    public bool alreadyDipped = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentJamIndex = -1;
        lastDipTime = -10f;
    }

    public void ResetJams()
    {
        foreach (var jam in allAvailableJams)
        {

                jam.dippingStation.gameObject.SetActive(false);
            
        }
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

                // Ensure number sprites start visible
                if (jam.numberSprite != null)
                    jam.numberSprite.SetActive(true);
            }
            else
            {
                jam.dippingStation.gameObject.SetActive(false);
            }
        }

        if (!alreadyDipped)
            SelectJam(0, true);
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

    void SelectJam(int index, bool bypassCooldown = false)
    {
        alreadyDipped = true;

        if (activeJams == null || index < 0 || index >= activeJams.Count) return;
        if (index == currentJamIndex) return;

        if (!bypassCooldown && Time.time < lastDipTime + dipCooldown) return;

        lastDipTime = Time.time;

        // Reactivate ALL number sprites first
        for (int i = 0; i < activeJams.Count; i++)
        {
            if (activeJams[i].numberSprite != null)
                activeJams[i].numberSprite.SetActive(true);
        }

        currentJamIndex = index;

        // Disable the selected jam's number sprite
        if (activeJams[index].numberSprite != null)
            activeJams[index].numberSprite.SetActive(false);

        PerformDipAnimation(activeJams[index]);

        // Visual Updates (fists)
        butterFist.SetActive(false);
        stawberryFist.SetActive(false);
        grapeFist.SetActive(false);
        peanutFist.SetActive(false);

        switch (activeJams[index].flavor)
        {
            case JamFlavor.Butter: butterFist.SetActive(true); break;
            case JamFlavor.StrawberryJam: stawberryFist.SetActive(true); break;
            case JamFlavor.GrapeJam: grapeFist.SetActive(true); break;
            case JamFlavor.PeanutButter: peanutFist.SetActive(true); break;
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(dipSounds, sfxMixer, jam.dippingStation.position);

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

    public string GetCurrentJamName()
    {
        if (activeJams.Count == 0 || currentJamIndex < 0) return "None";

        if (currentJamIndex >= activeJams.Count)
        {
            SelectByFlavor(activeJams[0].flavor);
        }

        return activeJams[currentJamIndex].flavor.ToString();
    }

    public Color GetCurrentJamColor()
    {
        if (activeJams.Count == 0 || currentJamIndex < 0) return Color.white;
        return activeJams[currentJamIndex].jamColor;
    }

    public Color GetColorFromFlavor(JamFlavor flavor)
    {
        foreach (var j in allAvailableJams)
            if (j.flavor == flavor) return j.jamColor;
        return Color.white;
    }
}