using DG.Tweening;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

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

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 0.3f;
    [SerializeField] private int shakeVibrato = 20;

    [SerializeField] private AudioSource ding;
    [SerializeField] private AudioSource popUp;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space)) LaunchToast();

        if(Time.time - lastLaunchTime >= timeToLaunchToast && ClientManager.Instance.areThereClients)
        {
            LaunchToast();
        }
    }

    public void LaunchToast(string customLetter = "")
    {
        ding.Play();
        popUp.Play();

        transform.DOComplete(); // Prevents "drifting" if toasts launch rapidly
        transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato);

        lastLaunchTime = Time.time;

        GameObject toast = Instantiate(toastPrefab, ejectPoint.position, ejectPoint.rotation);
        Rigidbody rb = toast.GetComponent<Rigidbody>();
        ToastBehavior behavior = toast.AddComponent<ToastBehavior>();

        // Randomsize the saturation of the base color of the toast to be more burnt
        TAG_ToastMesh toastMesh = toast.GetComponentInChildren<TAG_ToastMesh>();
        if (toastMesh != null)
        {
            Renderer rend = toastMesh.GetComponent<Renderer>();
            if (rend != null)
            {
                // 1. Get the current color
                Color baseColor = rend.material.GetColor("baseColorFactor");

                // 2. Convert RGB to HSV
                float h, s, v;
                Color.RGBToHSV(baseColor, out h, out s, out v);

                // 3. Set Saturation based on your 0-100 logic
                // If you want a random saturation between 0 and 100:
                float inspectorSaturation = Random.Range(0f, 100f);
                s = inspectorSaturation / 100f; // Convert 100 to 1.0

                // 4. Convert back to RGB
                Color finalColor = Color.HSVToRGB(h, s, v);

                // 5. Apply it
                rend.material.SetColor("baseColorFactor", finalColor);
            }
        }


        // 1. Pass Punch/Target References
        behavior.potentialTargets = targets;
        behavior.armPrefab = armPrefab;

        behavior.flightDuration = 0.3f;

        // 2. Pass Hover/Bob Settings
        behavior.hoverDuration = hoverTime;
        behavior.bobAmount = bobAmount;
        behavior.bobSpeed = bobSpeed;
        behavior.preHoverDelay = Random.Range(minPreHoverDelay, maxPreHoverDelay);
        behavior.driftFactor = driftFactor;

        // 3. Pass Punch Mechanics Settings
        behavior.debugAlwaysL = debugAlwaysL;
        behavior.armSpawnOffset = armSpawnOffset;
        behavior.armPunchDuration = armPunchDuration;
        behavior.targetFlightForce = targetFlightForce;
        behavior.armShrinkDuration = 0.6f;

        // 4. Randomized Visuals/Physics
        toast.transform.eulerAngles += new Vector3(Random.Range(-15f, 15f), Random.Range(-15f, 15f), Random.Range(-15f, 15f));

        Vector3 force = new Vector3(
            Random.Range(-xSpread, xSpread),
            Random.Range(upForce - 0.5f, upForce),
            Random.Range(-zSpread, zSpread)
        );

        rb.AddForce(force, ForceMode.Impulse);

        char letterToAssign;

        if (!string.IsNullOrEmpty(customLetter))
        {
            letterToAssign = customLetter[0]; // Take the first char
        }
        else
        {
            // Your existing random letter logic
            letterToAssign = (char)Random.Range('A', 'Z' + 1);
        }

        // toast.SetLetter(letterToAssign);
    }
}