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

    private List<ToastBehavior> activeToasts = new List<ToastBehavior>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (Time.time - lastLaunchTime >= timeToLaunchToast && ClientManager.Instance.areThereClients)
        {
            // Use the new method to check if the air is clear
            if (!AreTherePunchableToasts())
            {
                LaunchToast();
            }
        }
    }

    public bool AreTherePunchableToasts()
    {
        // Remove toasts that are null OR have already been hit/timed out (isPunchable == false)
        activeToasts.RemoveAll(t => t == null || !t.isPunchable);

        // If the list still has items, it means there are active targets in the air
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

        ding.Play();
        popUp.Play();
        transform.DOComplete();
        transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato);

        lastLaunchTime = Time.time;

        GameObject toast = Instantiate(toastPrefab, ejectPoint.position, ejectPoint.rotation);
        Rigidbody rb = toast.GetComponent<Rigidbody>();
        ToastBehavior behavior = toast.AddComponent<ToastBehavior>();

        activeToasts.Add(behavior);

        bool isSimul = ClientManager.Instance.GetSimultaneousStatusForNextToast();
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

        toast.transform.eulerAngles += new Vector3(Random.Range(-15f, 15f), Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        rb.AddForce(new Vector3(Random.Range(-xSpread, xSpread), Random.Range(upForce - 0.5f, upForce), Random.Range(-zSpread, zSpread)), ForceMode.Impulse);

        if (isSimul)
        {
            ClientManager.Instance.PrepareNextLetterForSimultaneous();
            LaunchToast();
        }
    }

    public void UnregisterToast(ToastBehavior toast) => activeToasts.Remove(toast);
}