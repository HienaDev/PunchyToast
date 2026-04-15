using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class LevelMenuManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject pagePrefab; // A prefab with a Grid Layout Group (2x5)
    public GameObject levelButtonPrefab;
    public GameObject emptySlotPrefab;

    [Header("Navigation")]
    public Button btnLeft;
    public Button btnRight;
    public RectTransform pageAnchor; // The parent inside the Mask
    public float slideDuration = 0.5f;
    public float slideAmount = 1000f; // Define this in the inspector (e.g., 1080 or 1920)

    private List<LevelConfiguration> allLevels = new List<LevelConfiguration>();
    private int currentPage = 0;
    private int levelsPerPage = 10;
    private GameObject currentActivePage;
    private bool isTransitioning = false;

    void Start()
    {
        LoadAllLevels();
        ShowPage(0, false);
    }

    private void OnEnable()
    {
        LoadAllLevels();
        ShowPage(0, false);
    }

    void LoadAllLevels()
    {
        // Loads everything from Resources/Levels/
        LevelConfiguration[] loaded = Resources.LoadAll<LevelConfiguration>("Levels");
        allLevels = loaded.OrderBy(l => l.levelNumber).ToList();
    }

    public void NextPage() => ChangePage(1);
    public void PrevPage() => ChangePage(-1);

    private void ChangePage(int direction)
    {
        if (isTransitioning) return;

        int nextPageIndex = currentPage + direction;
        if (nextPageIndex < 0 || nextPageIndex >= GetTotalPages()) return;

        ShowPage(nextPageIndex, true, direction);
    }

    private int GetTotalPages()
    {
        return Mathf.CeilToInt((float)allLevels.Count / levelsPerPage);
    }

    private void ShowPage(int index, bool animate, int direction = 1)
    {
        isTransitioning = true;
        currentPage = index;

        // 1. Instantiate the new page
        GameObject newPage = Instantiate(pagePrefab, pageAnchor);
        FillPage(newPage, currentPage);

        // 2. Handle buttons state
        btnLeft.interactable = currentPage > 0;
        btnRight.interactable = currentPage < GetTotalPages() - 1;

        if (!animate)
        {
            if (currentActivePage != null) Destroy(currentActivePage);
            currentActivePage = newPage;
            newPage.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            isTransitioning = false;
        }
        else
        {
            // 3. Updated Animation Logic with slideAmount
            RectTransform newRect = newPage.GetComponent<RectTransform>();
            RectTransform oldRect = currentActivePage.GetComponent<RectTransform>();

            // Position new page exactly at the slideAmount offset
            newRect.anchoredPosition = new Vector2(slideAmount * direction, 0);

            // Slide both using InOutCubic for a smoother start/end transition
            oldRect.DOAnchorPos(new Vector2(-slideAmount * direction, 0), slideDuration)
                .SetEase(Ease.InOutCubic);

            newRect.DOAnchorPos(Vector2.zero, slideDuration)
                .SetEase(Ease.InOutCubic)
                .OnComplete(() =>
                {
                    if (oldRect != null) Destroy(oldRect.gameObject);
                    currentActivePage = newPage;
                    isTransitioning = false;
                });
        }
    }

    private void FillPage(GameObject pageObj, int pageIndex)
    {
        int startIdx = pageIndex * levelsPerPage;

        for (int i = 0; i < levelsPerPage; i++)
        {
            int dataIdx = startIdx + i;

            if (dataIdx < allLevels.Count)
            {
                // Add actual Level Button
                GameObject btn = Instantiate(levelButtonPrefab, pageObj.transform);
                btn.GetComponent<LevelButton>().Initialize(allLevels[dataIdx], gameObject.transform.parent.gameObject);
            }
            else
            {
                // Add Empty Slot prefab to keep the 2x5 grid intact
                Instantiate(emptySlotPrefab, pageObj.transform);
            }
        }
    }
}