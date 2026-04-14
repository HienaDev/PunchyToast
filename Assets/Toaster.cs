using UnityEngine;
using System.Collections.Generic;

public class Toaster : MonoBehaviour
{
    public GameObject toastPrefab;
    public Transform ejectPoint;

    [Header("Punch Game References")]
    public List<Transform> targets;
    public GameObject armPrefab;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) LaunchToast();
    }

    void LaunchToast()
    {
        GameObject toast = Instantiate(toastPrefab, ejectPoint.position, ejectPoint.rotation);
        Rigidbody rb = toast.GetComponent<Rigidbody>();
        ToastBehavior behavior = toast.AddComponent<ToastBehavior>();

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
    }
}