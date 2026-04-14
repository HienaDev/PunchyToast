using UnityEngine;
using UnityEngine.UI; // For a patience bar if you have one
using DG.Tweening;

public class Client : MonoBehaviour
{
    [Header("Settings")]
    public float maxPatience = 20f;
    public Transform mouthBone;
    public Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);

    [Header("Current Order")]
    public string desiredCondiment; // e.g., "StrawberryJam", "None"
    public Color condimentColor;
    public float currentPatience;
    public bool isSatisfied = false;

    private Vector3 originalMouthRot;

    [SerializeField] private Image patienceCover; // Will go from 1 to 0 as patience goes down
    [SerializeField] private Image patience; // Will go from green to red as patience goes down

    void Start()
    {
        currentPatience = maxPatience;
        originalMouthRot = mouthBone.localEulerAngles;
    }

    public void SetOrder(string jamName, Color jamColor)
    {
        desiredCondiment = jamName;
        condimentColor = jamColor;
        GetComponentInChildren<TAG_Thought>().GetComponent<Renderer>().material.SetColor("_BaseColor", condimentColor);
        Debug.Log($"New Client spawned! I want: {desiredCondiment}");
    }

    [SerializeField] private Gradient patienceGradient; // Define this in the Inspector

    void Update()
    {
        if (isSatisfied) return;

        currentPatience -= Time.deltaTime;

        if (patienceCover != null || patience != null)
        {
            float patienceRatio = Mathf.Clamp01(currentPatience / maxPatience);

            if (patienceCover != null)
            {
                patienceCover.fillAmount = patienceRatio;
            }

            if (patience != null)
            {
                // Evaluates the gradient based on the current ratio (0 to 1)
                patience.color = patienceGradient.Evaluate(patienceRatio);
            }
        }

        if (currentPatience <= 0)
        {
            LeaveAngry();
        }
    }

    void GenerateRandomOrder()
    {
        // Example: Logic to pick from your JamDecider list or None
        string[] options = { "StrawberryJam", "BlueberryJam", "None" };
        desiredCondiment = options[Random.Range(0, options.Length)];
        Debug.Log($"Client at {name} wants {desiredCondiment}");
    }

    public void OpenMouth()
    {
        mouthBone.DOLocalRotate(mouthOpenRotation, 0.2f).SetEase(Ease.OutBack);
    }

    public void CloseMouth()
    {
        mouthBone.DOLocalRotate(originalMouthRot, 0.2f).SetEase(Ease.InSine);
    }

    public void TryEatToast(string incomingJam, GameObject toast)
    {
        if (isSatisfied) return;

        if (incomingJam == desiredCondiment)
        {
            // SUCCESS: Open mouth and eat
            isSatisfied = true;
            Debug.Log("Delicious! Client Satisfied.");

            // Let the toast stay in the mouth for a second before "consuming"
            DOVirtual.DelayedCall(0.5f, () => {
                ReceiveFood();
                CloseMouth();
                Destroy(toast);
            });
        }
        else
        {
            // FAILURE: Stay closed, toast hits face
            Debug.Log("Bleh! I didn't order this.");
            // You could add a "shake head" animation here if you want!
        }
    }

    void LeaveAngry()
    {
        Debug.Log("Client left angry!");
        ClientManager.Instance.ClearSeat(transform.parent); // Parent is likely the "Target" spot
        Destroy(gameObject);
    }

    public void ReceiveFood()
    {
        isSatisfied = true;
        Debug.Log("YUM!");
        // Add score logic here
        Destroy(gameObject, 1f);
    }
}