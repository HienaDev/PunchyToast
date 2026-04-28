using UnityEngine;

public class FireController : MonoBehaviour
{
    [SerializeField] public GameObject fire1;
    [SerializeField] public GameObject fire2;
    [SerializeField] public GameObject fire3;

    public void Disable()
    {
        fire1.SetActive(false);
        fire2.SetActive(false);
        fire3.SetActive(false);
    }
}

