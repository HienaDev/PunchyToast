using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FollowCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Image cursorImage; // The Image component to swap

    [Header("Cursor States")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite clickedSprite;

    [Header("Movement")]
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float smoothSpeed = 20f;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (cursorImage == null) cursorImage = GetComponent<Image>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        // Initialize with normal sprite
        if (normalSprite != null) cursorImage.sprite = normalSprite;
    }

    void Update()
    {
        Cursor.visible = false;
        // 1. Position Logic
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition,
            parentCanvas.worldCamera,
            out localPoint);

        if (smoothMovement)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                localPoint,
                Time.unscaledDeltaTime * smoothSpeed);
        }
        else
        {
            rectTransform.anchoredPosition = localPoint;
        }

        // 2. Click State Logic
        // We use Input.GetMouseButton here because a custom cursor 
        // usually needs to show the click state anywhere on the screen.
        if (Input.GetMouseButtonDown(0))
        {
            SetCursorState(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            SetCursorState(false);
        }
    }

    private void SetCursorState(bool isClicked)
    {
        if (cursorImage == null) return;

        if (isClicked && clickedSprite != null)
        {
            cursorImage.sprite = clickedSprite;
        }
        else if (!isClicked && normalSprite != null)
        {
            cursorImage.sprite = normalSprite;
        }
    }
}