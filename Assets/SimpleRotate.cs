using UnityEngine;

public class SimpleRotate : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 rotationAxis = Vector3.up; // Default is Y axis
    public float speed = 50f;

    [Tooltip("If true, uses world space. If false, rotates around its own local axis.")]
    public bool useWorldSpace = false;

    void Update()
    {
        // Rotating every frame
        // Space.Self uses the object's local orientation
        // Space.World ignores the object's rotation and uses the global axes
        transform.Rotate(rotationAxis * (speed * Time.deltaTime), useWorldSpace ? Space.World : Space.Self);
    }
}