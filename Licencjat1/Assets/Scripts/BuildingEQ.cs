// BuildingEQ.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingEQ : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuildingSystem buildingSystem;
    [SerializeField] private Transform inventoryPanel;     // Panel z HorizontalLayoutGroup
    [SerializeField] private GameObject slotPrefab;        // Prefab slotu (Button + Image)
    [SerializeField] private Canvas mainCanvas;            // G?ówny Canvas (przeci?gnij z hierarchii)

    [Header("Visuals")]
    [SerializeField] private Sprite defaultIcon;

    private List<BuildingData> buildings = new();
    private GameObject draggingIcon;
    private BuildingData draggedData;

    private void Awake()
    {
        if (buildingSystem == null) buildingSystem = FindObjectOfType<BuildingSystem>();
        if (mainCanvas == null) mainCanvas = FindObjectOfType<Canvas>();
    }

    public void Initialize(List<BuildingData> availableBuildings)
    {
        buildings = availableBuildings;
        CreateSlots();
    }

    private void CreateSlots()
    {
        // Czy?cimy panel
        foreach (Transform child in inventoryPanel)
            Destroy(child.gameObject);

        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingData data = buildings[i];
            GameObject slot = Instantiate(slotPrefab, inventoryPanel);

            // Ikona
            Image iconImg = slot.GetComponentInChildren<Image>();
            if (iconImg != null)
                iconImg.sprite = data.Icon != null ? data.Icon : defaultIcon;

            // Dodajemy handler drag & drop
            var handler = slot.AddComponent<EQSlotDragHandler>();
            handler.Setup(data, this);

            // Klikni?cie bez drag te? aktywuje preview
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => StartPreview(data));
            }
        }
    }

    // === Metody wywo?ywane przez EQSlotDragHandler ===

    public void BeginDrag(BuildingData data)
    {
        draggedData = data;

        // Tworzymy ikon? lec?c? za kursorem
        draggingIcon = new GameObject("EQ_DraggingIcon");
        draggingIcon.transform.SetParent(mainCanvas.transform, false);

        Image img = draggingIcon.AddComponent<Image>();
        img.sprite = data.Icon != null ? data.Icon : defaultIcon;
        img.raycastTarget = false;

        RectTransform rt = draggingIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingIcon != null)
            draggingIcon.transform.position = eventData.position;

        // Natychmiast tworzymy preview pod myszk?
        Vector3 worldPos = buildingSystem.GetMouseWorldPosition();
        buildingSystem.CreatePreviewFromInventory(draggedData, worldPos);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggingIcon != null)
            Destroy(draggingIcon);

        // Je?li puszczono poza terenem (np. nad UI) ? anuluj
        if (eventData.pointerEnter == null || !eventData.pointerEnter.CompareTag("Ground"))
        {
            buildingSystem.CancelCurrentPreview();
        }
        // Je?li nad terenem i zielony ? budowa wykona si? w BuildingSystem na LMB up

        draggedData = null;
    }

    // Zwyk?e klikni?cie w slot
    private void StartPreview(BuildingData data)
    {
        Vector3 mousePos = buildingSystem.GetMouseWorldPosition();
        buildingSystem.CreatePreviewFromInventory(data, mousePos);
    }
}

// Pomocniczy handler na ka?dy slot – implementuje wszystkie wymagane interfejsy
public class EQSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BuildingData data;
    private BuildingEQ parent;

    public void Setup(BuildingData buildingData, BuildingEQ parentScript)
    {
        data = buildingData;
        parent = parentScript;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parent.BeginDrag(data);
    }

    public void OnDrag(PointerEventData eventData)
    {
        parent.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        parent.EndDrag(eventData);
    }
}