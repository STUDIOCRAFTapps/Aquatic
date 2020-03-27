using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualChunkManager : MonoBehaviour {

    #region Header and Init
    public static VisualChunkManager inst;

    [Header("Parameters")]
    public GameObject chunkPrefab;
    public GameObject mobileChunkPrefab;
    public GameObject chunkLayerPrefab;
    public Material baseMaterial;
    
    Queue<VisualChunk> unusedVisualChunkPool;
    public Dictionary<Vector2Int, VisualChunk> visualChunkPool;
    Queue<MobileChunk> unusedMobileChunkPool;
    public Dictionary<int, MobileChunk> mobileChunkPool;

    [HideInInspector] public Material globalMaterial;

    private void Awake () {
        if(inst == null) {
            inst = this;
        }
        
        unusedVisualChunkPool = new Queue<VisualChunk>();
        unusedMobileChunkPool = new Queue<MobileChunk>();
        visualChunkPool = new Dictionary<Vector2Int, VisualChunk>();
        mobileChunkPool = new Dictionary<int, MobileChunk>();

        globalMaterial = new Material(baseMaterial);
        if(TerrainManager.inst.tiles.textures == null) {
            Debug.LogError("The texture hasn't been built. This may be caused by the TerrainManager getting initied after the VisualChunkManager.");
        } else {
            globalMaterial.SetTexture("_MainTex", TerrainManager.inst.tiles.textures);
        }

        //DEBUG
        //Repair load chunk using physicalmobilechunk

        //TerrainManager.inst.LoadMobileChunkFromUID(14);
        /*MobileDataChunk dataChunkTest = TerrainManager.inst.GetNewMobileDataChunk(new Vector2Int(0, 0));
        dataChunkTest.uid = 14;
        LoadMobileChunk(dataChunkTest, Vector2.zero);
        DataChunkSaving.inst.LoadChunk(dataChunkTest);
        dataChunkTest.RefreshTiles();
        TerrainManager.inst.QueueMobileChunkReload(dataChunkTest);*/
        /*for(int x = 0; x < 8; x++) {
            for(int y = 0; y < 8; y++) {
                dataChunkTest.SetGlobalID(x, y, TerrainLayers.Ground, 1);
            }
        }
        for(int x = 0; x < 8; x++) {
            for(int y = 4; y < 8; y++) {
                dataChunkTest.SetGlobalID(x, y, TerrainLayers.Ground, 0);
            }
        }*/
        /*for(int x = 0; x < 8; x++) {
            for(int y = 0; y < 8; y++) {
                dataChunkTest.SetGlobalID(x, y, TerrainLayers.WaterBackground, 5);
            }
        }
        for(int x = 0; x < 8; x++) {
            dataChunkTest.SetGlobalID(x, 7, TerrainLayers.WaterSurface, 6);
        }
        for(int y = 1; y < 7; y++) {
            dataChunkTest.SetGlobalID(5, y, TerrainLayers.Ground, 3);
        }*/

        /*dataChunkTest.RefreshTiles();
        LoadMobileChunk(dataChunkTest, new Vector2(-5.87f, 6.5f));
        DataChunkSaving.inst.SaveChunk(dataChunkTest);
        */
        //DataChunkSaving.inst.LoadChunk(dataChunkTest);
        //LoadChunkAt(new Vector2Int(0, 0));
        //DataChunkSaving.inst.SaveChunk(dataChunkTest);
    }
    #endregion

    #region Load and Unloads
    public void LoadChunkAt (Vector2Int chunkPosition) {
        VisualChunk vc = GetNewVisualChunk(chunkPosition);

        if(!TerrainManager.inst.GetChunkAtPosition(chunkPosition, out DataChunk dataChunk)) {
            Debug.LogError($"A data chunk wasn't found at {chunkPosition}");

            unusedVisualChunkPool.Enqueue(vc);
            return;
        }
        vc.BuildMeshes(dataChunk);
    }

    public void UnloadChunkAt (Vector2Int chunkPosition) {
        VisualChunk vc = visualChunkPool[chunkPosition];

        vc.gameObject.SetActive(false);
        visualChunkPool.Remove(chunkPosition);
        unusedVisualChunkPool.Enqueue(vc);
    }

    public void BuildMobileChunk (MobileChunk mobileChunk, Vector3 position) {
        mobileChunk.position = position;
        mobileChunk.previousPosition = position;
        mobileChunk.transform.position = position;
        mobileChunk.BuildMeshes();
    }

    public void BuildMobileChunk (MobileChunk mobileChunk) {
        mobileChunk.BuildMeshes();
    }

    public void DeleteMobileChunk (MobileChunk mc) {
        mc.gameObject.SetActive(false);
        mobileChunkPool.Remove(mc.uid);
        unusedMobileChunkPool.Enqueue(mc);
        DataChunkSaving.inst.DeleteMobileChunk(mc.mobileDataChunk);
        EntityRegionManager.inst.RemoveMobileChunk(mc);

        if(EntityRegionManager.inst.outOfBoundsMobileChunks.Contains(mc)) {
            EntityRegionManager.inst.outOfBoundsMobileChunks.Remove(mc);
        }
    }

    public void UnloadMobileChunk (MobileChunk mc, bool removeFromRegions = false, bool save = true) {
        mc.gameObject.SetActive(false);
        mobileChunkPool.Remove(mc.uid);
        unusedMobileChunkPool.Enqueue(mc);

        if(save) {
            DataChunkSaving.inst.SaveChunk(mc.mobileDataChunk);
        }

        if(removeFromRegions) {
            EntityRegionManager.inst.RemoveMobileChunk(mc);
        }

        if(EntityRegionManager.inst.outOfBoundsMobileChunks.Contains(mc)) {
            EntityRegionManager.inst.outOfBoundsMobileChunks.Remove(mc);
        }
    }
    #endregion

    #region Get New Chunks
    VisualChunk GetNewVisualChunk (Vector2Int chunkPosition) {
        VisualChunk unusedChunk;

        if(visualChunkPool.ContainsKey(chunkPosition)) {
            unusedChunk = visualChunkPool[chunkPosition];
        } else if(unusedVisualChunkPool.Count > 0) {
            unusedChunk = unusedVisualChunkPool.Dequeue();
            unusedChunk.gameObject.SetActive(true);
            visualChunkPool.Add(chunkPosition, unusedChunk);
        } else {
            unusedChunk = Instantiate(chunkPrefab, TerrainManager.inst.terrainRoot).GetComponent<VisualChunk>();
            visualChunkPool.Add(chunkPosition, unusedChunk);
        }
        
        unusedChunk.Initiate(chunkPosition, true);

        return unusedChunk;
    }

    public MobileChunk GetNewMobileChunk (int uid) {
        MobileChunk unusedChunk;
        
        if(unusedMobileChunkPool.Count > 0) {
            unusedChunk = unusedMobileChunkPool.Dequeue();
            unusedChunk.gameObject.SetActive(true);
            mobileChunkPool.Add(uid, unusedChunk);
        } else {
            unusedChunk = Instantiate(mobileChunkPrefab, TerrainManager.inst.mobileRoot).GetComponent<MobileChunk>();
            mobileChunkPool.Add(uid, unusedChunk);
        }
        unusedChunk.uid = uid;
        unusedChunk.Initiate();

        return unusedChunk;
    }
    #endregion

    #region Other Commands
    public void ToggleAllSelectionRects (bool doSetActive) {
        foreach(KeyValuePair<int, MobileChunk> kvp in mobileChunkPool) {
            kvp.Value.selectionRect.gameObject.SetActive(doSetActive);
        }
    }
    #endregion
}

[System.Serializable]
public class TerrainLayerParameters {
    public TerrainLayers layer;

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;
}
