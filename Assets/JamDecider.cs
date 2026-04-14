using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class JamDecider : MonoBehaviour
{
    public static JamDecider Instance;

    [System.Serializable]
    public struct JamType
    {
        public string name;
        public Color jamColor;
        public Transform dippingStation;
    }

    [Header("Settings")]
    public GameObject armPrefab;
    public List<JamType> jams;

    [Header("Animation")]
    public float dipDepth = 0.8f;      // How far "into" the jar it punches
    public float dipDuration = 0.15f;
    public float zOffset = 2.0f;       // How far back on the Z axis it starts

    [Header("Current Selection")]
    public int currentJamIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        for (int i = 0; i < jams.Count; i++)
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
        Debug.Log($"Selected: {jams[index].name}");

        PerformDipAnimation(jams[index]);
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

    public Color GetCurrentJamColor()
    {
        return jams[currentJamIndex].jamColor;
    }
}