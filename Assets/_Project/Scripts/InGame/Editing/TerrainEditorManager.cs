using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TerrainEditorManager : MonoBehaviour {

    #region Header
    public TerrainEditorUI tEditUI;
    public GameObject uiGroup;
    new public Camera camera;
    public static TerrainEditorManager inst;
    public GameObject previewObjectPrefab;
    public Sprite emptyPreview;
    public float previewZ = -5f;
    public GameObject chunkSelection;
    public GameObject chunkCollider;
    public SpriteRenderer chunkSelectionSprite;
    public SpriteRenderer chunkColliderSprite;
    public RectTransform zOrderingUI;
    public RectTransform[] draggingKnobs;

    public bool enableEditor = true;
    public int selectedSidebar = 0;
    public int renderingMode = 0;
    public TerrainLayers selectedLayer = TerrainLayers.Ground;
    public int selectedMaterialID = 1;
    public int selectedTool = 0;
    public int selectedBrushes = 0;
    public int selectedSubBrushIndex = 0;

    public int selectedMobileTool = 0;

    bool lockedTool0 = false;
    bool lockedTool1 = false;
    bool wasEnabled = false;
    Vector2Int initPos;
    MobileDataChunk lastMdc;
    bool alreadyBeenPressed = false;
    int tempMat = 1;
    MobileChunk selectedMC;
    bool freezeMC = false;
    public bool isDraggingMobile = false;
    public Vector2 draggingMobileDelta;

    public int selectedEntityID = 1;
    public int selectedEntityTool = 0;
    [HideInInspector] public Entity selectedE;
    RigidbodyPixel selectedERigibody;
    public bool isDraggingEntity = false;
    public Vector2 draggingEntityDelta;
    public DNAEditorManager dnaEditor;

    public BaseToolAsset[] tools;
    public BaseBrushAsset[] brushes;

    public List<GameObject> previewObjectList;
    public Queue<GameObject> previewObjectQueue;
    #endregion

    #region Monobehaviour
    void Start () {
        if(inst == null) {
            inst = this;
        }
        dnaEditor.Init();

        uiGroup.SetActive(false);

        previewObjectList = new List<GameObject>();
        previewObjectQueue = new Queue<GameObject>();

        for(int i = 0; i < brushes.Length; i++) {
            brushes[i].BuildBrushes();
        }
    }

    void Update () {
        ClearAllPreviewObject();

        if(Input.GetKeyDown(KeyCode.G)) {
            enableEditor = !enableEditor;
            UpdateAllMenu();
        }

        if(enableEditor && !wasEnabled) {
            uiGroup.SetActive(true);
            wasEnabled = true;
        }
        if(!enableEditor && wasEnabled) {
            uiGroup.SetActive(false);
            wasEnabled = false;
            ClearAllPreviewObject();
        }

        if(!enableEditor) {
            return;
        }
        AnyMode();

        if(selectedSidebar == 0) {
            ToolAndBrushMode();
        } else if(selectedSidebar == 1) {
            MobileMode();
        } else if(selectedSidebar == 2) {
            EntityMode();
        }



        Vector2 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetKeyDown(KeyCode.Alpha1)) {
            pathFS = worldPos;
        }
        if(Input.GetKeyDown(KeyCode.Alpha2)) {
            pathFE = worldPos;
        }
        if(Input.GetKey(KeyCode.Alpha3)) {
            PathRequestManager.inst.RequestPath(new PathRequest(pathFS, pathFE, (System.Action<List<Vector2>, bool>)OnPathReceived), Vector2Int.one);
        }

        DrawPath();
    }

    Vector2 pathFS;
    Vector2 pathFE;
    List<Vector2> path;

    public void OnPathReceived (List<Vector2> waypoints, bool pathSuccessful) {
        if(pathSuccessful) {
            path = waypoints;
        } else {
            path = null;
        }
    }

    void DrawPath () {
        if(path == null) {
            return;
        }
        if(path.Count <= 0) {
            Debug.Log(path.Count);
            return;
        }
        
        for(int i = 0; i < path.Count - 1; i++) {
            Debug.DrawLine(path[i], path[i+1], Color.red);
        }
    }
    #endregion

    #region Modes
    void AnyMode () {
        bool pointerOverUI = EventSystem.current.IsPointerOverGameObject();
        
        chunkSelection.SetActive(selectedMC != null || (selectedSidebar == 1 && selectedMobileTool == 4 && !pointerOverUI) || selectedE != null);
        chunkCollider.SetActive(selectedMC != null);
        dnaEditor.gameObject.SetActive(selectedE != null && selectedEntityTool == 4);

        foreach(RectTransform knb in draggingKnobs) {
            knb.gameObject.SetActive(selectedMC != null && selectedMobileTool == 3);
        }
    }

    Vector3 zOrderButtonsWorldPos;
    Vector2 createDragStart;
    bool isDraggingMobileCreation;

    void MobileMode () {
        bool pointerOverUI = EventSystem.current.IsPointerOverGameObject();

        Vector2 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        MobileDataChunk mdc = TerrainManager.inst.GetMobileChunkAtPosition(worldPos)?.mobileDataChunk;

        // SELECT
        if(!pointerOverUI) {
            if(mdc != null) {
                if(Input.GetMouseButtonDown(0)) {
                    freezeMC = false;
                    selectedMC = mdc.mobileChunk;

                    zOrderButtonsWorldPos = chunkSelection.transform.position +
                        TerrainManager.inst.TileToWorld(selectedMC.mobileDataChunk.restrictedSize).y * Vector3.up;
                }
            }
            if(Input.GetMouseButtonDown(1)) {
                selectedMC = null;
            }
        }


        // DRAG START-END
        if(selectedMobileTool == 1 && !pointerOverUI) {
            if(!isDraggingMobile && mdc != null && Input.GetMouseButtonDown(0)) {
                draggingMobileDelta = (Vector2)selectedMC.transform.position - worldPos;
                isDraggingMobile = true;
                EntityRegionManager.inst.RemoveMobileChunk(selectedMC);
            }
        }
        if(isDraggingMobile && (Input.GetMouseButtonUp(0) || pointerOverUI)) {
            if(selectedMC != null) {
                Vector3 destination = worldPos + draggingMobileDelta;
                if(Input.GetKey(KeyCode.LeftShift)) {
                    destination = (Vector2)Vector2Int.RoundToInt(destination);
                }
                destination.z = selectedMC.transform.position.z;
                selectedMC.transform.position = destination;

                EntityRegionManager.inst.AddMobileChunk(selectedMC);
            }

            isDraggingMobile = false;
        } else if(pointerOverUI) {
            isDraggingMobile = false;
        }


        // WHILE DRAG
        if(isDraggingMobile && selectedMC != null) {
            float blend = 1f - Mathf.Pow(1f - 0.5f, Time.deltaTime * 45f);
            Vector3 destination = worldPos + draggingMobileDelta;
            if(Input.GetKey(KeyCode.LeftShift)) {
                destination = (Vector2)Vector2Int.RoundToInt(destination);
            }
            destination.z = selectedMC.transform.position.z;

            Vector2 newPosition = Vector3.Lerp(selectedMC.transform.position, destination, blend);
            selectedMC.UpdatePositionData(newPosition);
            selectedMC.transform.position = newPosition;
            selectedMC.rigidbody.velocity = Vector2.zero;
            selectedMC.rigidbody.disableForAFrame = true;
        }


        // SELECT OUTLINE & Z ORDER UI
        if(selectedMC != null) {
            chunkSelection.transform.position = selectedMC.transform.position;
            chunkCollider.transform.localPosition = selectedMC.boxCollider.offset - selectedMC.boxCollider.size * 0.5f;
            chunkCollider.transform.localPosition += Vector3.back * 5.065f; 
            chunkSelectionSprite.size = TerrainManager.inst.TileToWorld(selectedMC.mobileDataChunk.restrictedSize);
            chunkColliderSprite.size = selectedMC.boxCollider.size;
            
            if(selectedMobileTool == 2) {
                float blend = 1f - Mathf.Pow(1f - 0.6f, Time.deltaTime * 80f);
                zOrderButtonsWorldPos = Vector3.Lerp(
                    camera.ScreenToWorldPoint(zOrderingUI.position),
                    (Vector3)((Vector2)chunkSelection.transform.position) +
                    TerrainManager.inst.TileToWorld(selectedMC.mobileDataChunk.restrictedSize).y * Vector3.up,
                    blend
                );
                zOrderingUI.position = camera.WorldToScreenPoint(zOrderButtonsWorldPos);
            }
        }
        zOrderingUI.gameObject.SetActive(selectedMC != null && selectedMobileTool == 2);


        // KNOB MANAGEMENT
        if(selectedMC != null && selectedMobileTool == 3) {
            Vector2 corner = (Vector2)selectedMC.transform.position + selectedMC.boxCollider.offset - selectedMC.boxCollider.size * 0.5f;
            Vector2 size = selectedMC.boxCollider.size;
            draggingKnobs[0].position = camera.WorldToScreenPoint(new Vector2(corner.x, corner.y));
            draggingKnobs[1].position = camera.WorldToScreenPoint(new Vector2(corner.x + size.x, corner.y));
            draggingKnobs[2].position = camera.WorldToScreenPoint(new Vector2(corner.x, corner.y + size.y));
            draggingKnobs[3].position = camera.WorldToScreenPoint(new Vector2(corner.x + size.x, corner.y + size.y));
        }

        if(selectedMobileTool == 4) {
            selectedMC = null;

            if(Input.GetMouseButtonDown(0)) {
                isDraggingMobileCreation = true;
                createDragStart = worldPos;
            }
            if(Input.GetMouseButtonUp(0)) {
                if(isDraggingMobileCreation && !pointerOverUI) {
                    Vector2 min = Vector2Int.FloorToInt(new Vector2(Mathf.Min(createDragStart.x, worldPos.x), Mathf.Min(createDragStart.y, worldPos.y)));
                    Vector2 max = Vector2Int.CeilToInt(new Vector2(Mathf.Max(createDragStart.x, worldPos.x), Mathf.Max(createDragStart.y, worldPos.y)));
                    max -= min;
                    max.x = Mathf.Clamp(max.x, 1, TerrainManager.inst.chunkSize);
                    max.y = Mathf.Clamp(max.y, 1, TerrainManager.inst.chunkSize);
                    max += min;

                    MobileChunk mc = TerrainManager.inst.CreateNewMobileChunk(Vector2Int.CeilToInt(max - min), new Vector3(min.x, min.y, 0f));
                    for(int x = 0; x < Vector2Int.CeilToInt(max - min).x; x++) {
                        for(int y = 0; y < Vector2Int.CeilToInt(max - min).y; y++) {
                            mc.mobileDataChunk.SetGlobalID(x, y, TerrainLayers.Background, 0);
                            mc.mobileDataChunk.SetGlobalID(x, y, TerrainLayers.Ground, 1);
                            mc.mobileDataChunk.SetGlobalID(x, y, TerrainLayers.WaterBackground, 0);
                            mc.mobileDataChunk.SetGlobalID(x, y, TerrainLayers.WaterSurface, 0);
                        }
                    }
                    mc.mobileDataChunk.RefreshTiles();
                }

                isDraggingMobileCreation = false;
            }

            if(!isDraggingMobileCreation) {
                chunkSelection.transform.position = (Vector2)Vector2Int.RoundToInt(worldPos - Vector2.one * 0.5f);
                chunkSelectionSprite.size = Vector2.one;
            } else {
                Vector2 min = Vector2Int.FloorToInt(new Vector2(Mathf.Min(createDragStart.x, worldPos.x), Mathf.Min(createDragStart.y, worldPos.y)));
                Vector2 max = Vector2Int.CeilToInt(new Vector2(Mathf.Max(createDragStart.x, worldPos.x), Mathf.Max(createDragStart.y, worldPos.y)));
                max -= min;
                max.x = Mathf.Clamp(max.x, 1, TerrainManager.inst.chunkSize);
                max.y = Mathf.Clamp(max.y, 1, TerrainManager.inst.chunkSize);
                max += min;
                chunkSelection.transform.position = min;
                chunkSelectionSprite.size = max - min;
            }
        }

        // PHYSICS FREEZING
        if(freezeMC && selectedMC != null) {
            selectedMC.rigidbody.velocity = Vector2.zero;
            selectedMC.rigidbody.disableForAFrame = true;
        }
    }

    void ToolAndBrushMode () {
        bool pointerOverUI = EventSystem.current.IsPointerOverGameObject();
        if(Input.GetMouseButtonDown(0) && pointerOverUI) {
            lockedTool0 = true;
        }
        if(Input.GetMouseButtonUp(0) && lockedTool0) {
            lockedTool0 = false;
        }
        if(Input.GetMouseButtonDown(1) && pointerOverUI) {
            lockedTool1 = true;
        }
        if(Input.GetMouseButtonUp(1) && lockedTool1) {
            lockedTool1 = false;
        }
        if(pointerOverUI || lockedTool0 || lockedTool1) {
            return;
        }

        if(Input.GetMouseButtonDown(1)) {
            tempMat = selectedMaterialID;
            selectedMaterialID = 0;
        }

        // Preparing data
        Vector2 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        ToolUseInfo info = new ToolUseInfo {
            layer = selectedLayer,
            materialID = selectedMaterialID,
            initPos = initPos,
            brushes = brushes[selectedBrushes],
            brushIndex = selectedSubBrushIndex
        };
        Vector2Int tilePos;

        MobileDataChunk mdc = TerrainManager.inst.GetMobileChunkAtPosition(worldPos)?.mobileDataChunk;
        if(mdc != null) {
            info.mdc = mdc;
            tilePos = TerrainManager.inst.WorldToTile(worldPos - (Vector2)mdc.mobileChunk.position);
        } else {
            tilePos = TerrainManager.inst.WorldToTile(worldPos);
        }
        if(Input.GetMouseButtonUp(1)) {
            selectedMaterialID = tempMat;
        }

        if(mdc == null && renderingMode == 1) {
            return;
        }

        if((mdc == null && lastMdc != null) || (mdc != null && lastMdc == null)) {
            initPos = tilePos;
        }
        lastMdc = mdc;

        info.currentPos = tilePos;

        if((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && !alreadyBeenPressed) {
            alreadyBeenPressed = true;
            initPos = tilePos;
            info.initPos = tilePos;

            tools[selectedTool].OnPressed(info);
        } else if((Input.GetMouseButtonUp(0) && !Input.GetMouseButton(1)) || (Input.GetMouseButtonUp(1) && !Input.GetMouseButton(0))) {
            alreadyBeenPressed = false;
            tools[selectedTool].OnReleased(info);
        } else if(Input.GetMouseButton(0) || Input.GetMouseButton(1)) {
            tools[selectedTool].OnHold(info);
        } else {
            tools[selectedTool].OnSelection(info);
        }

        
    }

    void EntityMode () {
        bool pointerOverUI = EventSystem.current.IsPointerOverGameObject();
        
        Vector2 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int tilePos = TerrainManager.inst.WorldToTile(worldPos);
        Entity entity = EntityManager.inst.GetEntityAtPoint(worldPos);

        //SELECT
        if(!pointerOverUI) {
            if(entity != null) {
                if(Input.GetMouseButtonDown(0)) {
                    selectedE = entity;
                    selectedERigibody = selectedE.GetComponent<RigidbodyPixel>();

                    // Prepare DNA here
                    DNAEditorManager.inst.Setup(selectedE);
                }
            }
            if(Input.GetMouseButtonDown(1)) {
                selectedE = null;
            }
        }

        // DRAG START-END
        if(selectedEntityTool == 3 && !pointerOverUI) {
            if(!isDraggingEntity && entity != null && Input.GetMouseButtonDown(0)) {
                draggingEntityDelta = (Vector2)selectedE.transform.position - worldPos;
                isDraggingEntity = true;
                //EntityRegionManager.inst.RemoveMobileChunk(selectedMC);
            }
        }
        if(isDraggingEntity && (Input.GetMouseButtonUp(0) || pointerOverUI)) {
            if(selectedE != null) {
                Vector3 destination = worldPos + draggingEntityDelta;
                if(Input.GetKey(KeyCode.LeftShift)) {
                    destination = (Vector2)Vector2Int.RoundToInt(destination);
                }
                destination.z = selectedE.transform.position.z;
                selectedE.transform.position = destination;

                //EntityRegionManager.inst.AddMobileChunk(selectedE);
            }

            isDraggingEntity = false;
        } else if(pointerOverUI) {
            isDraggingEntity = false;
        }

        // WHILE DRAG
        if(isDraggingEntity && selectedE != null) {
            float blend = 1f - Mathf.Pow(1f - 0.5f, Time.deltaTime * 45f);
            Vector3 destination = worldPos + draggingEntityDelta;
            if(Input.GetKey(KeyCode.LeftShift)) {
                destination = (Vector2)Vector2Int.RoundToInt(destination);
            }
            destination.z = selectedE.transform.position.z;

            selectedE.transform.position = Vector3.Lerp(selectedE.transform.position, destination, blend);
            if(selectedERigibody != null) {
                selectedERigibody.velocity = Vector2.zero;
                selectedERigibody.disableForAFrame = true;
            }
        }

        //SELECTION
        if(selectedE != null) {
            if(selectedERigibody != null) {
                chunkSelection.transform.position = selectedE.transform.position + (Vector3)(selectedERigibody.box.offset - (selectedERigibody.box.size * 0.5f));
                chunkSelectionSprite.size = selectedERigibody.box.size;
            } else {
                chunkSelection.transform.position = selectedE.transform.position + (Vector3)(selectedE.asset.loadBoxOffset - (selectedE.asset.loadBoxSize * 0.5f));
                chunkSelectionSprite.size = selectedE.asset.loadBoxSize;
            }
        }

        if(selectedEntityTool == 1 && !pointerOverUI) {
            if(Input.GetMouseButtonDown(0)) {
                Entity spawnEntity = EntityManager.inst.Spawn(worldPos, selectedEntityID);
                if(spawnEntity != null) {
                    selectedE = spawnEntity;
                    selectedERigibody = selectedE.GetComponent<RigidbodyPixel>();
                    draggingEntityDelta = (Vector2)selectedE.transform.position - worldPos/* + selectedERigibody.box.offset*/;
                    isDraggingEntity = true;
                }
            }
        }

        if(selectedEntityTool == 2 && !pointerOverUI) {
            if(Input.GetMouseButtonDown(0)) {
                Entity deleteEntity = EntityManager.inst.GetEntityAtPoint(worldPos);
                if(selectedE == deleteEntity) {
                    selectedE = null;
                }
                if(deleteEntity != null) {
                    EntityManager.inst.Kill(deleteEntity);
                }
            }
        }

        /*if(Input.GetMouseButtonDown(0)) {
            if(selectedEntityTool == 2) {
                Entity entity = 
                if(entity != null) {
                    EntityManager.inst.Kill(entity);
                }
            }
            if(selectedEntityTool == 1) {
                EntityManager.inst.Spawn(tilePos, new EntityString("default", "salmod"));
            }
        }*/
    }
    #endregion

    #region Preview
    void ClearAllPreviewObject () {
        foreach(GameObject previewObject in previewObjectList) {
            previewObjectQueue.Enqueue(previewObject);
            previewObject.SetActive(false);
        }
        previewObjectList.Clear();
    }

    public void PreviewTileAt (int x, int y, int globalID, MobileDataChunk mdc = null) {
        GameObject previewObject;

        if(previewObjectQueue.Count > 0) {
            previewObject = previewObjectQueue.Dequeue();
        } else {
            previewObject = Instantiate(previewObjectPrefab, transform);
        }

        previewObject.SetActive(true);
        if(mdc != null) {
            previewObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, previewZ) + mdc.mobileChunk.position;
        } else {
            previewObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, previewZ);
        }
        if(globalID != 0) {
            previewObject.GetComponent<SpriteRenderer>().sprite = GeneralAsset.inst.GetTileAssetFromGlobalID(globalID).previewSprite;
        } else {
            previewObject.GetComponent<SpriteRenderer>().sprite = emptyPreview;
        }

        previewObjectList.Add(previewObject);
    }
    #endregion

    #region Events 
    public void CycleRenderingMode () {
        // Cycle bettween 0 - 2
        renderingMode++;
        if(renderingMode >= 3) {
            renderingMode = 0;
        }

        if(renderingMode == 0) {
            TerrainManager.inst.terrainRoot.gameObject.SetActive(true);
            TerrainManager.inst.mobileRoot.gameObject.SetActive(true);
            VisualChunkManager.inst.ToggleAllSelectionRects(false);
        } else if(renderingMode == 1) {
            TerrainManager.inst.terrainRoot.gameObject.SetActive(false);
            TerrainManager.inst.mobileRoot.gameObject.SetActive(true);
            VisualChunkManager.inst.ToggleAllSelectionRects(true);
        } else if(renderingMode == 2) {
            TerrainManager.inst.terrainRoot.gameObject.SetActive(true);
            TerrainManager.inst.mobileRoot.gameObject.SetActive(false);
            VisualChunkManager.inst.ToggleAllSelectionRects(false);
        }
    }

    public void SetSelectedSidebar (int selectedSidebar) {
        this.selectedSidebar = selectedSidebar;
        UpdateAllMenu();
    }

    public void UpdateAllMenu () {
        selectedMC = null;
        chunkSelection.SetActive(false);
    }

    public void OrderZForward () {
        if(selectedMC != null && selectedMobileTool == 2) {
            selectedMC.transform.position += Vector3.back; //Because Z order are technically inverted
        }
    }

    public void OrderZBackward () {
        if(selectedMC != null && selectedMobileTool == 2) {
            selectedMC.transform.position += Vector3.forward; //Because Z order are technically inverted
        }
    }

    public void OrderZReset () {
        if(selectedMC != null && selectedMobileTool == 2) {
            selectedMC.transform.position += Vector3.back * selectedMC.transform.position.z; //Neutralizes the z axis
        }
    }

    public void EndDragKnob () {
        freezeMC = false;
    }

    public void DragKnob (int id) {
        if(selectedMC != null && selectedMobileTool == 3) {
            Vector2 chunkPos = selectedMC.transform.position;
            Vector2 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
            selectedMC.boxCollider.size += Vector2.one * PhysicsPixel.inst.errorHandler * 4f;
            worldPos = new Vector2(
                Mathf.Clamp(worldPos.x, chunkPos.x, chunkPos.x + selectedMC.mobileDataChunk.restrictedSize.x),
                Mathf.Clamp(worldPos.y, chunkPos.y, chunkPos.y + selectedMC.mobileDataChunk.restrictedSize.y)
            );
            if(Input.GetKey(KeyCode.LeftShift)) {
                worldPos -= chunkPos;
                worldPos = Vector2Int.RoundToInt(worldPos);
                worldPos += chunkPos;
            }
            Vector2 corner = (Vector2)selectedMC.transform.position + selectedMC.boxCollider.offset - selectedMC.boxCollider.size * 0.5f;
            Vector2 size = selectedMC.boxCollider.size;

            if(id == 0) {
                Vector2 oppWorldPos = new Vector2(corner.x + size.x, corner.y + size.y);
                selectedMC.boxCollider.size = new Vector2(Mathf.Max(oppWorldPos.x - worldPos.x, 0f), Mathf.Max(oppWorldPos.y - worldPos.y, 0f));
                size = selectedMC.boxCollider.size;
                selectedMC.boxCollider.offset = new Vector2(worldPos.x - chunkPos.x + size.x * 0.5f, worldPos.y - chunkPos.y + size.y * 0.5f);
            } else if(id == 1) {
                Vector2 oppWorldPos = new Vector2(corner.x, corner.y + size.y);
                selectedMC.boxCollider.size = new Vector2(Mathf.Max(worldPos.x - oppWorldPos.x, 0f), Mathf.Max(oppWorldPos.y - worldPos.y, 0f));
                size = selectedMC.boxCollider.size;
                selectedMC.boxCollider.offset = new Vector2(oppWorldPos.x - chunkPos.x + size.x * 0.5f, worldPos.y - chunkPos.y + size.y * 0.5f);
            } else if(id == 2) {
                Vector2 oppWorldPos = new Vector2(corner.x + size.x, corner.y);
                selectedMC.boxCollider.size = new Vector2(Mathf.Max(oppWorldPos.x - worldPos.x, 0f), Mathf.Max(worldPos.y - oppWorldPos.y, 0f));
                size = selectedMC.boxCollider.size;
                selectedMC.boxCollider.offset = new Vector2(worldPos.x - chunkPos.x + size.x * 0.5f, oppWorldPos.y - chunkPos.y + size.y * 0.5f);
            } else if(id == 3) {
                Vector2 oppWorldPos = new Vector2(corner.x, corner.y);
                selectedMC.boxCollider.size = new Vector2(Mathf.Max(worldPos.x - oppWorldPos.x, 0f), Mathf.Max(worldPos.y - oppWorldPos.y, 0f));
                size = selectedMC.boxCollider.size;
                selectedMC.boxCollider.offset = new Vector2(oppWorldPos.x - chunkPos.x + size.x * 0.5f, oppWorldPos.y - chunkPos.y + size.y * 0.5f);
            }
            selectedMC.boxCollider.size -= Vector2.one * PhysicsPixel.inst.errorHandler * 4f;
        }
        freezeMC = true;
    }

    public void DeleteMobileChunk () {
        if(selectedMC != null) {
            VisualChunkManager.inst.DeleteMobileChunk(selectedMC);

            if(selectedMobileTool != 0) {
                tEditUI.ToggleMobileMode(0);
            }

            UpdateAllMenu();
        }
    }
    #endregion
}

public struct ToolUseInfo {
    public Vector2Int initPos;
    public Vector2Int currentPos;
    public TerrainLayers layer;
    public int materialID;
    public BaseBrushAsset brushes;
    public int brushIndex;
    public MobileDataChunk mdc;
}