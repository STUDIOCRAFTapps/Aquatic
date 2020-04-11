using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TerrainEditorUI : MonoBehaviour {

    [Header("References")]
    public Dropdown sidebarDropdown;
    public GameObject[] allSidebars;

    [Header("Toolbar")]
    public GameObject toolSlotPrefab;
    public Sprite selectedToolSprite;
    public Sprite normalToolSprite;
    public Color selectedToolColor;
    public Color normalToolColor;
    public RectTransform toolbarContent;
    RectTransform[] toolsRect;

    [Header("Brushes")]
    public GameObject brushSlotPrefab;
    public Sprite selectedBrushSprite;
    public Sprite normalBrushSprite;
    public Color selectedBrushColor;
    public Color normalBrushColor;
    public RectTransform brushbarContent;
    public Sprite[] dropdownSprites;
    List<RectTransform> brushesRect;

    [Header("Item Menu")]
    public int minItemWidth;
    public GameObject tileAssetPrefab;
    public RectTransform itemMenuContent;
    public GridLayoutGroup itemMenuLayout;
    List<TileAssetUIElement> tileAssetRect;
    public RectTransform terrainEditorRect;
    public GameObject slotDragPrefab;
    public Sprite selectedSlotDragSprite;
    public Sprite normalSlotDragSprite;
    public Sprite[] menuButtonSprites;
    public GameObject itemMenu;
    public Button toggleButton;

    [Header("Hotbar")]
    public RectTransform hotbarRect;
    public RectTransform hotbarContent;
    public GameObject slotIndicator;
    public Dropdown layerMenu;

    [Header("Mobile Menu")]
    public Sprite[] renderingModeSprites;
    public Image renderingModeImage;

    [Header("Mobile Layering")]
    public Image[] modeTogglesImages;
    public Image[] modeTogglesIcons;

    [Header("Entity Menu")]
    public Image[] entityMenuButtonImages;
    public Image[] entityMenuButtonIcons;

    TerrainEditorManager tem;
    Vector2Int lastResolution;
    SlotDrag lastSelectedSlot;
    [HideInInspector]
    public bool isDraggingSlot = false;

    #region Mono
    private void Start () {
        tem = TerrainEditorManager.inst;

        // Load tools and brushes
        brushesRect = new List<RectTransform>();
        toolsRect = new RectTransform[tem.tools.Length];
        for(int i = 0; i < tem.tools.Length; i++) {
            GameObject newTool = Instantiate(toolSlotPrefab, toolbarContent);
            toolsRect[i] = newTool.GetComponent<RectTransform>();
            toolsRect[i].GetChild(0).GetChild(0).GetComponent<Image>().sprite = tem.tools[i].uiSprite;

            int index = i;
            toolsRect[i].GetComponent<Button>().onClick.AddListener(() => {
                SelectTool(index);
            });
        }
        SelectTool(tem.selectedTool);
        CloseBrushTypeSelection();

        // Load items in the item menu
        tileAssetRect = new List<TileAssetUIElement>();
        CleanInventory();
        FillTileInventory();

        UpdateResolution();
        SelectSidebar(0);
    }

    private void Update () {
        Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);
        if(resolution != lastResolution) {
            UpdateResolution();
            lastResolution = resolution;
        }
        
        if(!TerrainEditorManager.inst.enableEditor) {
            return;
        }

        if(RectTransformUtility.RectangleContainsScreenPoint(hotbarRect, Input.mousePosition) && isDraggingSlot) {
            int sibIndex = hotbarContent.childCount;
            for(int i = 0; i < hotbarContent.childCount; i++) {
                if(hotbarContent.GetChild(i).position.x < Input.mousePosition.x) {
                    sibIndex = i;
                }
            }
            slotIndicator.transform.SetSiblingIndex(sibIndex);
            slotIndicator.SetActive(true);
        } else {
            slotIndicator.SetActive(false);
        }
    }
    #endregion

    #region Inventory/Hotbar
    void CleanInventory () {
        foreach(TileAssetUIElement taUI in tileAssetRect) {
            Destroy(taUI.gameObject);
        }
        tileAssetRect.Clear();
    }

    void FillTileInventory () {
        for(int i = 0; i < TerrainManager.inst.tiles.tileByGlobalID.Count; i++) {
            // Spawn and configure tile asset displays
            GameObject newTileAsset = Instantiate(tileAssetPrefab, itemMenuContent);
            tileAssetRect.Add(newTileAsset.GetComponent<TileAssetUIElement>());
            tileAssetRect[tileAssetRect.Count - 1].image.sprite = TerrainManager.inst.tiles.tileByGlobalID[i].uiSprite;
            tileAssetRect[tileAssetRect.Count - 1].nameText.text = TerrainManager.inst.tiles.tileByGlobalID[i].fullName;
            tileAssetRect[tileAssetRect.Count - 1].infoText.text = TerrainManager.inst.tiles.tileByGlobalID[i].GetType().Name;
            int index = i;
            tileAssetRect[tileAssetRect.Count - 1].dragButton.onClick.AddListener(() => {

                // Spawn and configure slot drag
                SlotDrag slotDrag = Instantiate(slotDragPrefab, terrainEditorRect).GetComponent<SlotDrag>();
                slotDrag.teUI = this;
                slotDrag.terrainEditorRect = terrainEditorRect;
                slotDrag.transform.position = Input.mousePosition;
                slotDrag.icon.sprite = TerrainManager.inst.tiles.tileByGlobalID[index].uiSprite;
                int gid = index + 1;
                slotDrag.BeginDrag();

                slotDrag.button.onClick.AddListener(() => {
                    SelectTile(gid, slotDrag);
                });
            });
        }
    }

    void FillEntityInventory () {
        for(int i = 0; i < EntityManager.inst.entityCollectionGroup.entitiesByGlobalID.Count; i++) {
            // Spawn and configure tile asset displays
            GameObject newTileAsset = Instantiate(tileAssetPrefab, itemMenuContent);
            tileAssetRect.Add(newTileAsset.GetComponent<TileAssetUIElement>());
            tileAssetRect[tileAssetRect.Count - 1].image.sprite = EntityManager.inst.entityCollectionGroup.entitiesByGlobalID[i].uiSprite;
            tileAssetRect[tileAssetRect.Count - 1].nameText.text = EntityManager.inst.entityCollectionGroup.entitiesByGlobalID[i].fullName;
            tileAssetRect[tileAssetRect.Count - 1].infoText.text = EntityManager.inst.entityCollectionGroup.entitiesByGlobalID[i].GetType().Name;
            int index = i;
            tileAssetRect[tileAssetRect.Count - 1].dragButton.onClick.AddListener(() => {

                // Spawn and configure slot drag
                SlotDrag slotDrag = Instantiate(slotDragPrefab, terrainEditorRect).GetComponent<SlotDrag>();
                slotDrag.teUI = this;
                slotDrag.terrainEditorRect = terrainEditorRect;
                slotDrag.transform.position = Input.mousePosition;
                slotDrag.icon.sprite = EntityManager.inst.entityCollectionGroup.entitiesByGlobalID[index].uiSprite;
                int gid = index + 1;
                slotDrag.BeginDrag();

                slotDrag.button.onClick.AddListener(() => {
                    SelectEntity(gid, slotDrag);
                });
            });
        }
    }
    #endregion

    #region Event
    public void ToggleItemMenu () {
        itemMenu.SetActive(!itemMenu.activeSelf);
        if(!itemMenu.activeSelf) {
            SpriteState ss = toggleButton.spriteState;
            ss.highlightedSprite = menuButtonSprites[1];
            ss.pressedSprite = menuButtonSprites[2];
            toggleButton.spriteState = ss;
            toggleButton.GetComponent<Image>().sprite = menuButtonSprites[0];
        } else {
            SpriteState ss = toggleButton.spriteState;
            ss.highlightedSprite = menuButtonSprites[4];
            ss.pressedSprite = menuButtonSprites[5];
            toggleButton.spriteState = ss;
            toggleButton.GetComponent<Image>().sprite = menuButtonSprites[3];
        }
    }

    public void DropTileOnHotbar (SlotDrag slotDrag) {
        if(RectTransformUtility.RectangleContainsScreenPoint(hotbarRect, Input.mousePosition)) {
            slotDrag.transform.SetParent(hotbarContent);
            slotDrag.transform.SetSiblingIndex(slotIndicator.transform.GetSiblingIndex());
        } else {
            Destroy(slotDrag.gameObject);
        }
    }

    public void SelectTile (int globalID, SlotDrag slotDrag) {
        if(lastSelectedSlot != null) {
            lastSelectedSlot.button.GetComponent<Image>().sprite = normalSlotDragSprite;
        }
        slotDrag.button.GetComponent<Image>().sprite = selectedSlotDragSprite;
        layerMenu.value = (int)TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).defaultPlacingLayer;
        tem.selectedLayer = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).defaultPlacingLayer;
        tem.selectedMaterialID = globalID;

        lastSelectedSlot = slotDrag;

        if(tem.selectedSidebar != 0) {
            sidebarDropdown.value = 0;
        }
    }

    public void SelectEntity (int globalID, SlotDrag slotDrag) {
        if(lastSelectedSlot != null) {
            lastSelectedSlot.button.GetComponent<Image>().sprite = normalSlotDragSprite;
        }
        slotDrag.button.GetComponent<Image>().sprite = selectedSlotDragSprite;
        tem.selectedEntityID = globalID;

        lastSelectedSlot = slotDrag;

        if(tem.selectedSidebar != 2) {
            sidebarDropdown.value = 2;
            if(tem.selectedEntityTool != 0) {
                ToggleEntityMode(0);
            }
        }
    }

    public void SelectLayer (int layer) {
        tem.selectedLayer = (TerrainLayers)layer;
    }

    public void SelectSidebar (int bar) {
        foreach(GameObject sidebar in allSidebars) {
            sidebar.SetActive(false);
        }
        allSidebars[bar].SetActive(true);
        TerrainEditorManager.inst.SetSelectedSidebar(bar);
        if(bar == 0) {
            CleanInventory();
            FillTileInventory();
        }
        if(bar == 2) {
            CleanInventory();
            FillEntityInventory();
        }
    }
    #endregion

    #region Resolution
    void UpdateResolution () {
        int widthCount = Mathf.FloorToInt(itemMenuContent.rect.width / (minItemWidth + 1));
        itemMenuLayout.constraintCount = widthCount;
        itemMenuLayout.cellSize = new Vector2(Mathf.Floor((itemMenuContent.rect.width - widthCount) / widthCount), itemMenuLayout.cellSize.y);
    }
    #endregion


    #region Toolbar
    public void SelectTool (int id) {
        toolsRect[tem.selectedTool].GetComponent<Image>().sprite = normalToolSprite;
        toolsRect[tem.selectedTool].GetChild(0).GetChild(0).GetComponent<Image>().color = normalToolColor;
        tem.selectedTool = id;
        toolsRect[tem.selectedTool].GetComponent<Image>().sprite = selectedToolSprite;
        toolsRect[tem.selectedTool].GetChild(0).GetChild(0).GetComponent<Image>().color = selectedToolColor;
    }
    #endregion

    #region Brushbar
    public void OpenBrushTypeSelection () {
        foreach(RectTransform rect in brushesRect) {
            Destroy(rect.gameObject);
        }
        brushesRect.Clear();

        for(int i = -1; i < tem.brushes.Length; i++) {
            GameObject newBrushSlot = Instantiate(brushSlotPrefab, brushbarContent);
            brushesRect.Add(newBrushSlot.GetComponent<RectTransform>());

            // Dropdown
            if(i == -1) {
                brushesRect[brushesRect.Count - 1].GetChild(0).GetChild(0).GetComponent<Image>().sprite = dropdownSprites[1];
                brushesRect[brushesRect.Count - 1].GetComponent<Button>().onClick.AddListener(() => {
                    CloseBrushTypeSelection();
                });
                continue;
            }

            if(tem.brushes[i].uiSprites.Length >= 3) {
                brushesRect[brushesRect.Count - 1].GetChild(0).GetChild(0).GetComponent<Image>().sprite = tem.brushes[i].uiSprites[2];
            } else {
                brushesRect[brushesRect.Count - 1].GetChild(0).GetChild(0).GetComponent<Image>().sprite = tem.brushes[i].uiSprites[0];
            }
            int index = i;
            brushesRect[brushesRect.Count - 1].GetComponent<Button>().onClick.AddListener(() => {
                SelectBrush(index);
            });
        }

        SelectBrush(tem.selectedBrushes);
    }

    public void CloseBrushTypeSelection () {
        foreach(RectTransform rect in brushesRect) {
            Destroy(rect.gameObject);
        }
        brushesRect.Clear();

        for(int i = -1; i < tem.brushes[tem.selectedBrushes].brushesData.Length; i++) {
            GameObject newBrushSlot = Instantiate(brushSlotPrefab, brushbarContent);
            brushesRect.Add(newBrushSlot.GetComponent<RectTransform>());

            // Dropdown
            if(i == -1) {
                brushesRect[brushesRect.Count - 1].GetChild(0).GetChild(0).GetComponent<Image>().sprite = dropdownSprites[0];
                brushesRect[brushesRect.Count - 1].GetComponent<Button>().onClick.AddListener(() => {
                    OpenBrushTypeSelection();
                });
                continue;
            }
            
            brushesRect[brushesRect.Count - 1].GetChild(0).GetChild(0).GetComponent<Image>().sprite = tem.brushes[tem.selectedBrushes].uiSprites[i];
            int index = i;
            brushesRect[brushesRect.Count - 1].GetComponent<Button>().onClick.AddListener(() => {
                SelectSubBrush(index);
            });
        }

        SelectSubBrush(tem.selectedSubBrushIndex);
    }

    public void SelectSubBrush (int id) {
        brushesRect[tem.selectedSubBrushIndex + 1].GetComponent<Image>().sprite = normalBrushSprite;
        brushesRect[tem.selectedSubBrushIndex + 1].GetChild(0).GetChild(0).GetComponent<Image>().color = normalBrushColor;
        tem.selectedSubBrushIndex = id;
        brushesRect[tem.selectedSubBrushIndex + 1].GetComponent<Image>().sprite = selectedBrushSprite;
        brushesRect[tem.selectedSubBrushIndex + 1].GetChild(0).GetChild(0).GetComponent<Image>().color = selectedBrushColor;
    }

    public void SelectBrush (int id) {
        brushesRect[tem.selectedBrushes + 1].GetComponent<Image>().sprite = normalBrushSprite;
        brushesRect[tem.selectedBrushes + 1].GetChild(0).GetChild(0).GetComponent<Image>().color = normalBrushColor;
        tem.selectedBrushes = id;
        brushesRect[tem.selectedBrushes + 1].GetComponent<Image>().sprite = selectedBrushSprite;
        brushesRect[tem.selectedBrushes + 1].GetChild(0).GetChild(0).GetComponent<Image>().color = selectedBrushColor;
    }
    #endregion

    #region Mobile Menu
    public void ChangeRenderingMode () {
        TerrainEditorManager.inst.CycleRenderingMode();

        renderingModeImage.sprite = renderingModeSprites[TerrainEditorManager.inst.renderingMode];
    }

    //Toggle Move
    //Toggle Z Drag
    //Other buttons
    public void ToggleMobileMode (int modeID) {
        foreach(Image img in modeTogglesImages) {
            img.sprite = normalToolSprite;
        }
        foreach(Image img in modeTogglesIcons) {
            img.color = normalToolColor;
        }
        
        if(TerrainEditorManager.inst.selectedMobileTool == 0) {
            TerrainEditorManager.inst.selectedMobileTool = modeID + 1;

            modeTogglesImages[modeID].sprite = selectedToolSprite;
            modeTogglesIcons[modeID].color = selectedToolColor;
        } else if(TerrainEditorManager.inst.selectedMobileTool == modeID + 1) {
            TerrainEditorManager.inst.selectedMobileTool = 0;
        } else {
            TerrainEditorManager.inst.selectedMobileTool = modeID + 1;

            modeTogglesImages[modeID].sprite = selectedToolSprite;
            modeTogglesIcons[modeID].color = selectedToolColor;
        }
    }
    #endregion

    #region Entity Menu
    public void ToggleEntityMode (int modeID) {
        foreach(Image img in entityMenuButtonImages) {
            img.sprite = normalToolSprite;
        }
        foreach(Image img in entityMenuButtonIcons) {
            img.color = normalToolColor;
        }

        if(TerrainEditorManager.inst.selectedEntityTool == modeID + 1) {
            TerrainEditorManager.inst.selectedEntityTool = 0;
        } else {
            TerrainEditorManager.inst.selectedEntityTool = modeID + 1;

            entityMenuButtonImages[modeID].sprite = selectedToolSprite;
            entityMenuButtonIcons[modeID].color = selectedToolColor;
        }
    }
    #endregion
}
