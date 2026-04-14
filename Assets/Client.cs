using UnityEngine;
using DG.Tweening;

public class Client : MonoBehaviour
{
    [Header("Settings")]
    public Transform mouthBone;
    public Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);

    [Header("Current Order")]
    public string desiredCondiment;
    public Color condimentColor;
    public bool isSatisfied = false;

    private Vector3 originalMouthRot;
    private Transform mySeat;

    void Start()
    {
        originalMouthRot = mouthBone.localEulerAngles;
        // Cache the seat immediately

    }

    public void Initialize(Transform seat)
    {
        mySeat = seat;
    }

    public void SetOrder(string jamName, Color jamColor)
    {
        desiredCondiment = jamName;
        condimentColor = jamColor;

        // Preservation of your custom logic
        TAG_Thought thought = GetComponentInChildren<TAG_Thought>();
        if (thought != null)
        {
            thought.GetComponent<Renderer>().material.SetColor("_BaseColor", condimentColor);
        }

        Debug.Log($"New Client spawned! I want: {desiredCondiment}");
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

        Debug.Log("Client received toast with: " + incomingJam);
        if (incomingJam == desiredCondiment)
        {
            isSatisfied = true;
            OpenMouth();

            DOVirtual.DelayedCall(0.5f, () => {
                ReceiveFood();
                CloseMouth();
                if (toast != null) Destroy(toast);
            });
        }
        else
        {
            Debug.Log("Wrong order!");
        }
    }

    public void ReceiveFood()
    {
        isSatisfied = true;
        ClientManager.Instance.OnClientFinished();

        // Safety check: only clear if we actually have a seat reference
        if (mySeat != null)
        {
            ClientManager.Instance.ClearSeat(mySeat);
        }
        else
        {
            Debug.LogWarning($"Client {gameObject.name} tried to clear a null seat!");
        }

        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => {
            Destroy(gameObject);
        });
    }
}