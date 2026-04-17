using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Toaster : MonoBehaviour
{
    public GameObject toastPrefab;
    public Transform ejectPoint;
    public static Toaster Instance;

    [Header("Punch Game References")]
    public List<Transform> targets;
    public GameObject armPrefab;

    [SerializeField] private float timeToLaunchToast = 2f;
    private float lastLaunchTime = 0f;

    [Header("Launch Physics")]
    public float upForce = 9f;
    public float xSpread = 1.5f;
    public float zSpread = 0.5f;
    [SerializeField] private float simultaneousStep = 0.25f; // Gap between toasts

    [Header("Hover Logic")]
    public float hoverTime = 2.5f;
    public float minPreHoverDelay = 0.1f;
    public float maxPreHoverDelay = 0.4f;
    public float driftFactor = 0.2f;

    [Header("Bobbing")]
    public float bobAmount = 0.2f;
    public float bobSpeed = 0.4f;

    [Header("Punch Settings")]
    public bool debugAlwaysL = true;
    public float armSpawnOffset = 4f;
    public float armPunchDuration = 0.15f;
    public float targetFlightForce = 25f;
    [SerializeField] private float toastFlightDuration = 0.2f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 0.3f;
    [SerializeField] private int shakeVibrato = 20;

    [SerializeField] private AudioSource ding;
    [SerializeField] private AudioSource popUp;
    [SerializeField] private Toggle easyModeToggle;
    public bool easyMode = false;

    public List<ToastBehavior> activeToasts = new List<ToastBehavior>();

    // Tracking for spread logic
    private int simultaneousCount = 0;
    private int simultaneousIndex = 0;

    public AudioClip[] punchSounds;
    public AudioClip[] toastGettingIntoMouth;
    public AudioClip[] toastFlying;
    public AudioClip[] toastLandingNaturally;

    public AudioClip toastComboSound;
    public AudioClip toastFinalComboSound;

    [SerializeField] private GameObject fireLvl1;
    [SerializeField] private GameObject fireLvl2;
    [SerializeField] private GameObject fireLvl3;

    public float GetComboPitch()
    {
        if (currentCombo >= 1)
        {
            float pitch = 1f + (currentCombo * 0.1f);
            return Mathf.Min(pitch, 1.7f); // Cap the pitch increase at 2x
        }
        else
        {
            return -1f; // Normal pitch when no combo
        }
    }

    public int currentCombo = 0;

    public void IncrementCombo() => currentCombo++;
    public void ResetCombo() => currentCombo = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (Time.time - lastLaunchTime >= timeToLaunchToast && ClientManager.Instance.areThereClients)
        {
            if (!AreTherePunchableToasts())
            {
                LaunchToast();
            }
        }
    }

    public bool AreTherePunchableToasts()
    {
        activeToasts.RemoveAll(t => t == null || !t.isPunchable);
        return activeToasts.Count > 0;
    }

    public void SetupToasterSettings(LevelConfiguration config)
    {
        this.hoverTime = config.hoverTime;
        this.minPreHoverDelay = config.minPreHoverDelay;
        this.maxPreHoverDelay = config.maxPreHoverDelay;
        this.driftFactor = config.driftFactor;
    }

    public void LaunchToast(string customLetter = "")
    {
        if (!ClientManager.Instance.areThereClients) return;

        // Spread Calculation Logic
        float currentXSpread;
        bool isSimul = ClientManager.Instance.GetSimultaneousStatusForNextToast();

        if (isSimul || simultaneousCount > 0)
        {
            // If this is the start of a burst, find out how many are coming
            if (simultaneousCount == 0)
            {
                simultaneousCount = ClientManager.Instance.GetSimultaneousBurstCount();
                simultaneousIndex = 0;
            }

            // Calculate centered position
            // Formula: (Index - (Total-1)/2) * Step
            float offset = (simultaneousIndex - (simultaneousCount - 1) * 0.5f) * simultaneousStep;
            currentXSpread = offset;

            simultaneousIndex++;
        }
        else
        {
            // Normal random spread
            currentXSpread = Random.Range(-xSpread, xSpread);
            simultaneousCount = 0;
            simultaneousIndex = 0;
        }

        ding.Play();
        popUp.Play();
        transform.DOComplete();
        transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato);

        lastLaunchTime = Time.time;

        GameObject toast = Instantiate(toastPrefab, ejectPoint.position, ejectPoint.rotation);
        Rigidbody rb = toast.GetComponent<Rigidbody>();
        ToastBehavior behavior = toast.AddComponent<ToastBehavior>();

        if (currentCombo > 1)
        {
            // Choose which fire prefab to use based on combo intensity
            GameObject fireToSpawn = null;

            if (currentCombo >= 7) fireToSpawn = fireLvl3;
            else if (currentCombo >= 3) fireToSpawn = fireLvl2;
            else fireToSpawn = fireLvl1;

            if (fireToSpawn != null)
            {
                // Instantiate at the toast's position and parent it to the toast
                GameObject fireInstance = Instantiate(fireToSpawn, toast.transform.position, Quaternion.identity);
                fireInstance.transform.SetParent(toast.transform);
            }
        }

        activeToasts.Add(behavior);
        behavior.isSimultaneous = isSimul;

        TAG_ToastMesh toastMesh = toast.GetComponentInChildren<TAG_ToastMesh>();
        if (toastMesh != null)
        {
            Renderer rend = toastMesh.GetComponent<Renderer>();
            if (rend != null)
            {
                Color baseColor = rend.material.GetColor("baseColorFactor");
                float h, s, v;
                Color.RGBToHSV(baseColor, out h, out s, out v);
                s = Random.Range(0f, 100f) / 100f;
                rend.material.SetColor("baseColorFactor", Color.HSVToRGB(h, s, v));
            }
        }

        behavior.potentialTargets = targets;
        behavior.armPrefab = armPrefab;
        behavior.flightDuration = toastFlightDuration;
        behavior.hoverDuration = easyModeToggle.isOn ? 600f : hoverTime;
        behavior.driftFactor = easyModeToggle.isOn ? 0f : driftFactor;
        behavior.bobAmount = bobAmount;
        behavior.bobSpeed = bobSpeed;
        behavior.preHoverDelay = Random.Range(minPreHoverDelay, maxPreHoverDelay);
        behavior.debugAlwaysL = debugAlwaysL;
        behavior.armSpawnOffset = armSpawnOffset;
        behavior.armPunchDuration = armPunchDuration;
        behavior.targetFlightForce = targetFlightForce;
        behavior.armShrinkDuration = 0.6f;

        behavior.punchSounds = punchSounds;
        behavior.toastGettingIntoMouth = toastGettingIntoMouth;
        behavior.toastFlying = toastFlying;
        behavior.toastLandingNaturally = toastLandingNaturally;

        toast.transform.eulerAngles += new Vector3(Random.Range(-15f, 15f), Random.Range(-15f, 15f), Random.Range(-15f, 15f));

        // Apply the calculated spread here
        rb.AddForce(new Vector3(currentXSpread, Random.Range(upForce - 0.5f, upForce), Random.Range(-zSpread, zSpread)), ForceMode.Impulse);

        if (isSimul)
        {
            ClientManager.Instance.PrepareNextLetterForSimultaneous();
            LaunchToast();
        }
        else
        {
            // Reset for next normal launch
            simultaneousCount = 0;
            simultaneousIndex = 0;
        }
    }

    public void UnregisterToast(ToastBehavior toast) => activeToasts.Remove(toast);
}