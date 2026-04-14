using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    void Awake()
    {
        Instance = this;
    }

    // Call this and provide the "juice" levels on the fly
    public void Shake(float duration, float strength, int vibrato)
    {
        // Reset position before shaking to prevent "drift"
        transform.DOComplete();
        transform.DOShakePosition(duration, strength, vibrato, 90f, false, true);
    }
}