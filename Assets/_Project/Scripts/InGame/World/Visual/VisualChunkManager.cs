using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VisualChunkManager : MonoBehaviour {

    #region Header and Init
    public static VisualChunkManager inst;

    [Header("Parameters")]
    public GameObject chunkPrefab;
    public GameObject mobileChunkPrefab;
    public GameObject chunkLayerPrefab;
    public Material baseMaterial;
    
    Queue<VisualChunk> unusedVisualChunkPool;
    public Dictionary<long, VisualChunk> visualChunks;
    Queue<MobileChunk> unusedMobileChunkPool;
    public Dictionary<int, MobileChunk> mobileChunkPool;

    [HideInInspector] public Material globalMaterial;

    private void Awake () {
        if(inst == null) {
            inst = this;
        }
        
        unusedVisualChunkPool = new Queue<VisualChunk>();
        unusedMobileChunkPool = new Queue<MobileChunk>();
        visualChunks = new Dictionary<long, VisualChunk>();
        mobileChunkPool = new Dictionary<int, MobileChunk>();

        globalMaterial = new Material(baseMaterial);
        if(GeneralAsset.inst.textures == null) {
            Debug.LogError("The texture hasn't been built. This may be caused by the TerrainManager getting initied after the VisualChunkManager.");
        } else {
            globalMaterial.SetTexture("_MainTex", GeneralAsset.inst.textures);
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
        if(!TerrainManager.inst.GetChunkAtPosition(chunkPosition, out DataChunk dataChunk)) {
            return;
        }
        VisualChunk vc = GetNewVisualChunk(chunkPosition);
        vc.BuildMeshes(dataChunk);
        vc.name = $"Visual Chunk {chunkPosition.ToString()}";
    }

    public void UnloadChunkAt (Vector2Int chunkPosition) {
        long hash = Hash.longFrom2D(chunkPosition);
        if(!visualChunks.TryGetValue(hash, out VisualChunk vc)) {
            return;
        }

        vc.gameObject.SetActive(false);
        PathgridManager.inst.SetNodeChunkAsUnused(chunkPosition);
        visualChunks.Remove(Hash.longFrom2D(chunkPosition));
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
        WorldSaving.inst.DeleteMobileChunk(mc.mobileDataChunk);
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
            WorldSaving.inst.SaveMobileChunk(mc.mobileDataChunk);
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

        if(visualChunks.TryGetValue(Hash.longFrom2D(chunkPosition), out VisualChunk visualChunk)) {
            unusedChunk = visualChunk;
        } else if(unusedVisualChunkPool.Count > 0) {
            unusedChunk = unusedVisualChunkPool.Dequeue();
            unusedChunk.gameObject.SetActive(true);
            visualChunks.Add(Hash.longFrom2D(chunkPosition), unusedChunk);
        } else {
            unusedChunk = Instantiate(chunkPrefab, TerrainManager.inst.terrainRoot).GetComponent<VisualChunk>();
            visualChunks.Add(Hash.longFrom2D(chunkPosition), unusedChunk);
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

    public VisualChunk GetVisualChunkAt (Vector2Int chunkPos) {
        if(visualChunks.TryGetValue(Hash.longFrom2D(chunkPos), out VisualChunk visualChunk)) {
            return visualChunk;
        } else {
            return null;
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos () {
        if(!Application.isPlaying) {
            return;
        }

        foreach(KeyValuePair<long, ChunkLoadCounter> kvp in ChunkLoader.inst.loadCounters) {
            bool isTimerRunning = kvp.Value.timer == null ? false : kvp.Value.timer.isRunning;
            if(TerrainManager.inst.chunkJobsManager.TryGetValue(kvp.Key, out ChunkJobManager cjm)) {
                int loadJobCount = 0, unloadJobCount = 0, saveJobCount = 0, runningJobCount = 0;
                for(int i = 0; i < cjm.jobs.Count; i++) {
                    if(cjm.jobs[i].loadState == JobState.Loaded) {
                        loadJobCount++;
                    } else if(cjm.jobs[i].loadState == JobState.Unloaded) {
                        unloadJobCount++;
                    } else if(cjm.jobs[i].loadState == JobState.Saving) {
                        saveJobCount++;
                    } else if(cjm.jobs[i].IsRunning()) {
                        runningJobCount++;
                    }
                }
                Handles.Label((kvp.Value.position + Vector2.up) * 16,
                    $"LoadC: {kvp.Value.loaders.Count}\nTimerR?: {isTimerRunning}\nJobsC: {cjm.jobs.Count}" +
                    $"\nLState? {cjm.targetLoadState == JobState.Loaded}\nL{loadJobCount} U{unloadJobCount} S{saveJobCount}" +
                    $" R{runningJobCount}");
            } else {
                Handles.Label((kvp.Value.position + Vector2.up) * 16, $"LoadC: {kvp.Value.loaders.Count}\nTimerR?: {isTimerRunning}");
            }
        }
        foreach(KeyValuePair<long, ChunkJobManager> kvp in TerrainManager.inst.chunkJobsManager) {
            if(!ChunkLoader.inst.loadCounters.ContainsKey(kvp.Key)) {
                ChunkJobManager cjm = kvp.Value;
                int loadJobCount = 0, unloadJobCount = 0, saveJobCount = 0, runningJobCount = 0;
                for(int i = 0; i < cjm.jobs.Count; i++) {
                    if(cjm.jobs[i].loadState == JobState.Loaded) {
                        loadJobCount++;
                    } else if(cjm.jobs[i].loadState == JobState.Unloaded) {
                        unloadJobCount++;
                    } else if(cjm.jobs[i].loadState == JobState.Saving) {
                        saveJobCount++;
                    } else if(cjm.jobs[i].IsRunning()) {
                        runningJobCount++;
                    }
                }
                Handles.Label((kvp.Value.position + Vector2.up) * 16,
                    $"JobsC: {cjm.jobs.Count}" +
                    $"\nLState? {cjm.targetLoadState == JobState.Loaded}\nL{loadJobCount} U{unloadJobCount} S{saveJobCount}" +
                    $" R{runningJobCount}");
            }
        }
    }
#endif
}

[System.Serializable]
public class TerrainLayerParameters {
    public TerrainLayers layer;

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;
}
