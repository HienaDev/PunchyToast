using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

// Helper class for JSON serialization
[System.Serializable]
public class StickerData
{
    public string spriteName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 size;
}

[System.Serializable]
public class StickerSaveList
{
    public List<StickerData> stickers = new List<StickerData>();
}

public class ToasterCustomization : MonoBehaviour
{
    [SerializeField] private Transform positionForToasterCamera;
    [SerializeField] private GameObject customizationUI;

    [Header("Prefabs & Containers")]
    [SerializeField] private GameObject decalPrefab;
    [SerializeField] private GameObject decalTester;
    [SerializeField] private GameObject stickerUIPrefab;
    [SerializeField] private Transform uiStickerContainer;

    [SerializeField] private Collider toasterCollider;

    [Header("Settings")]
    [SerializeField] private LayerMask toasterLayer;
    [SerializeField] private float sizeMultiplier = 0.1f;
    [SerializeField] private int maxStickers = 10;
    [SerializeField] private GameObject maxStickersWarning;

    [Header("Scale Limits")]
    [SerializeField] private float startingScale = 0.2f;
    [SerializeField] private float minScale = 0.05f;
    [SerializeField] private float maxScale = 1.0f;

    private Vector3 originalCameraPosition;
    private Vector3 originalCameraRotation;
    private Vector3 originalWarningScale;

    [SerializeField] private Sprite startingSticker;
    private Sprite currentSticker;
    public bool triggered = false;
    private bool isCustomizing = false;
    private float currentScale;

    private List<GameObject> placedStickers = new List<GameObject>();
    private List<GameObject> uiItems = new List<GameObject>();

    private const string SAVE_KEY = "ToasterStickersSave";

    void Start()
    {
        originalCameraPosition = Camera.main.transform.position;
        originalCameraRotation = Camera.main.transform.rotation.eulerAngles;

        if (maxStickersWarning != null)
        {
            originalWarningScale = maxStickersWarning.transform.localScale;
            maxStickersWarning.SetActive(false);
        }

        currentScale = startingScale;
        currentSticker = startingSticker;
        decalTester.SetActive(false);

        if (currentSticker != null) ApplySticker(currentSticker);

        // Load existing stickers immediately on Start
        LoadStickers();
    }

    void Update()
    {
        if (!isCustomizing || currentSticker == null) return;

        HandleStickerPreview();
        HandleScaling();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (placedStickers.Count < maxStickers)
            {
                PlaceSticker();
                SaveStickers(); // Save whenever a sticker is added
            }
            else if (maxStickersWarning != null)
            {
                maxStickersWarning.SetActive(true);
                maxStickersWarning.transform.DOKill();
                maxStickersWarning.transform.localScale = originalWarningScale;
                maxStickersWarning.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 10, 1f);
            }
        }
    }

    // --- SAVE / LOAD LOGIC ---

    public void SaveStickers()
    {
        StickerSaveList saveList = new StickerSaveList();

        foreach (GameObject stickerObj in placedStickers)
        {
            if (stickerObj == null) continue;

            DecalProjector proj = stickerObj.GetComponent<DecalProjector>();

            // Get the texture from the material
            Texture tex = proj.material.GetTexture("Base_Map");
            if (tex == null) continue;

            saveList.stickers.Add(new StickerData
            {
                spriteName = tex.name, // Just the name, e.g., "CoolSticker"
                position = stickerObj.transform.position,
                rotation = stickerObj.transform.rotation,
                size = proj.size
            });
        }

        string json = JsonUtility.ToJson(saveList);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void LoadStickers()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        StickerSaveList loadList = JsonUtility.FromJson<StickerSaveList>(json);

        foreach (StickerData data in loadList.stickers)
        {
            // Path adjusted to look inside your "Stickers" subfolder
            Sprite loadedSprite = Resources.Load<Sprite>("Stickers/" + data.spriteName);

            if (loadedSprite == null)
            {
                Debug.LogWarning($"Sticker {data.spriteName} not found in Resources/Stickers/");
                continue;
            }

            // Spawn the decal
            GameObject newSticker = Instantiate(decalPrefab, data.position, data.rotation);
            newSticker.transform.SetParent(this.transform);

            // Re-apply Material and Size
            UpdateDecalMaterial(newSticker, loadedSprite);
            newSticker.GetComponent<DecalProjector>().size = data.size;
            placedStickers.Add(newSticker);

            // Re-create the UI removal button
            CreateUIItemForSticker(newSticker, loadedSprite);
        }
    }

    private void CreateUIItemForSticker(GameObject stickerObj, Sprite sprite)
    {
        GameObject uiItem = Instantiate(stickerUIPrefab, uiStickerContainer);
        StickerUIItem itemScript = uiItem.GetComponent<StickerUIItem>();
        itemScript.Setup(stickerObj, sprite);
        itemScript.xButton.onClick.AddListener(itemScript.DeleteSticker);
        uiItems.Add(uiItem);
    }

    // --- UPDATED CORE METHODS ---

    private void PlaceSticker()
    {
        if (!decalTester.activeSelf) return;

        GameObject newSticker = Instantiate(decalPrefab, decalTester.transform.position, decalTester.transform.rotation);
        newSticker.transform.SetParent(this.transform);
        UpdateDecalMaterial(newSticker, currentSticker);
        newSticker.GetComponent<DecalProjector>().size = decalTester.GetComponent<DecalProjector>().size;
        placedStickers.Add(newSticker);

        CreateUIItemForSticker(newSticker, currentSticker);
        ResetWarning();
    }

    public void RemoveStickerFromList(GameObject sticker, GameObject ui)
    {
        if (placedStickers.Contains(sticker)) placedStickers.Remove(sticker);
        if (uiItems.Contains(ui)) uiItems.Remove(ui);

        SaveStickers(); // Update save file after deletion
        ResetWarning();
    }

    public void ClearAllStickers()
    {
        foreach (GameObject sticker in placedStickers) if (sticker != null) Destroy(sticker);
        foreach (GameObject ui in uiItems) if (ui != null) Destroy(ui);
        placedStickers.Clear();
        uiItems.Clear();

        PlayerPrefs.DeleteKey(SAVE_KEY); // Wipe save
        ResetWarning();
    }

    // ... (Remainder of your existing methods: HandleScaling, ApplySticker, UpdateDecalSize, etc. stay exactly the same)

    private void HandleScaling()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentScale += scroll * sizeMultiplier;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            UpdateDecalSize(decalTester);
        }
    }

    public void ApplySticker(Sprite sticker)
    {
        currentSticker = sticker;
        UpdateDecalMaterial(decalTester, sticker);
        UpdateDecalSize(decalTester);
    }

    private void UpdateDecalSize(GameObject target)
    {
        if (currentSticker == null) return;
        float aspectRatio = (float)currentSticker.texture.width / currentSticker.texture.height;
        var projector = target.GetComponent<DecalProjector>();
        if (projector != null)
        {
            projector.size = new Vector3(currentScale * aspectRatio, currentScale, 0.5f);
        }
    }

    private void HandleStickerPreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, toasterLayer))
        {
            if (!decalTester.activeSelf) decalTester.SetActive(true);
            decalTester.transform.position = hit.point;
            decalTester.transform.rotation = Quaternion.LookRotation(-hit.normal);
        }
        else
        {
            decalTester.SetActive(false);
        }
    }

    public void SelectStickerFromButton()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        if (clickedButton != null)
        {
            Image stickerImage = clickedButton.GetComponentInChildren<Image>();
            if (stickerImage != null && stickerImage.sprite != null)
            {
                ApplySticker(stickerImage.sprite);
            }
        }
    }

    private void UpdateDecalMaterial(GameObject obj, Sprite sticker)
    {
        var projector = obj.GetComponent<DecalProjector>();
        Material newInst = new Material(projector.material);
        newInst.SetTexture("Base_Map", sticker.texture);
        projector.material = newInst;
    }

    private void ResetWarning()
    {
        if (maxStickersWarning != null)
        {
            maxStickersWarning.transform.DOKill();
            maxStickersWarning.transform.localScale = originalWarningScale;
            maxStickersWarning.SetActive(false);
        }
    }

    public void StartCustomizing()
    {
        triggered = true;
        Camera.main.transform.DOMove(positionForToasterCamera.position, 1f);
        Camera.main.transform.DORotate(positionForToasterCamera.rotation.eulerAngles, 1f).OnComplete(() =>
        {
            isCustomizing = true;
            customizationUI.SetActive(true);
        });
    }

    public void StopCustomizing()
    {
        isCustomizing = false;
        decalTester.SetActive(false);
        customizationUI.SetActive(false);
        Camera.main.transform.DOMove(originalCameraPosition, 1f);
        Camera.main.transform.DORotate(originalCameraRotation, 1f).OnComplete(()=> triggered = false);
    }
}