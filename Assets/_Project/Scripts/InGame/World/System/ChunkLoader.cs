using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;

public class ChunkLoader : MonoBehaviour {

    public static ChunkLoader inst;
    public bool unloadOnlyMode = false; 
    
    public Dictionary<long, ChunkLoadCounter> loadCounters;
    public Dictionary<long, ChunkLoadTask> chunkToLoad;
    public Dictionary<long, Vector2Int> chunkToUnload;


    public Dictionary<ulong, bool> hasMovedSinceInit;
    public Dictionary<ulong, Transform> playerCenters;
    public Dictionary<ulong, Vector2Int> previousChunkPos;
    public Dictionary<ulong, ChunkLoadBounds> previousLoadBounds;

    public struct ChunkLoadTask {
        public Vector2Int position;
        public ulong clientID;

        public ChunkLoadTask (Vector2Int position, ulong clientID) {
            this.position = position;
            this.clientID = clientID;
        }
    }

    #region Adding / Removing Players
    public void PlayerJoins (ulong clientID, Transform center) {

        if(!NetworkAssistant.inst.IsServer) {
            if(clientID != NetworkAssistant.inst.ClientID) {
                return;
            }
        }

        Vector2Int centerChunkPos = TerrainManager.inst.WorldToChunk(center.position);
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(centerChunkPos, TerrainManager.inst.loadRadius);

        playerCenters.Add(clientID, center);
        previousChunkPos.Add(clientID, centerChunkPos);
        previousLoadBounds.Add(clientID, newBounds);
        hasMovedSinceInit.Add(clientID, false);
    }

    public void PlayerLeaves (ulong clientID) {
        if(!NetworkAssistant.inst.IsServer) {
            if(clientID != NetworkAssistant.inst.ClientID) {
                return;
            }
        }

        playerCenters.Remove(clientID);
        previousChunkPos.Remove(clientID);
        previousLoadBounds.Remove(clientID);
        hasMovedSinceInit.Remove(clientID);
    }
    #endregion

    #region Monobehaviour
    void Awake () {
        loadCounters = new Dictionary<long, ChunkLoadCounter>();
        chunkToLoad = new Dictionary<long, ChunkLoadTask>();
        chunkToUnload = new Dictionary<long, Vector2Int>();

        playerCenters = new Dictionary<ulong, Transform>();
        previousChunkPos = new Dictionary<ulong, Vector2Int>();
        previousLoadBounds = new Dictionary<ulong, ChunkLoadBounds>();
        hasMovedSinceInit = new Dictionary<ulong, bool>();

        if(NetworkAssistant.inst.playerToAddToChunkLoader.Count > 0) {
            foreach(KeyValuePair<ulong, Transform> kvp in NetworkAssistant.inst.playerToAddToChunkLoader) {
                PlayerJoins(kvp.Key, kvp.Value);
            }
            NetworkAssistant.inst.playerToAddToChunkLoader.Clear();
        }

        unloadOnlyMode = !NetworkAssistant.inst.IsServer;

        if(inst == null) {
            inst = this;
        }
    }
    
    private void Update () {
        if(chunkToLoad.Count > 0) {
            long firstKey = chunkToLoad.First().Key;
            LoadChunkAt(chunkToLoad[firstKey].clientID, chunkToLoad[firstKey].position);
            chunkToLoad.Remove(firstKey);
        }
        if(chunkToUnload.Count > 0) {
            long firstKey = chunkToUnload.First().Key;
            UnloadChunkAt(chunkToUnload[firstKey], false);
            chunkToUnload.Remove(firstKey);
        }
    }

    void FixedUpdate () {
        foreach(KeyValuePair<ulong, Transform> kvp in playerCenters) {
            TrackPlayerMovement(kvp.Key);
        }
    }
    #endregion

    #region Move Tracking
    void TrackPlayerMovement (ulong clientID) {
        Vector2Int currentChunkPos = TerrainManager.inst.WorldToChunk(playerCenters[clientID].position);

        if(!hasMovedSinceInit[clientID]) {
            hasMovedSinceInit[clientID] = true;

            previousChunkPos[clientID] = currentChunkPos;
            LoadBounds(clientID, currentChunkPos);
            return;
        }

        if(currentChunkPos != previousChunkPos[clientID]) {
            MoveLoadBounds(clientID, currentChunkPos);
            previousChunkPos[clientID] = currentChunkPos;
        }
    }

    void MoveLoadBounds (ulong clientID, Vector2Int centerChunkPos) {
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(centerChunkPos, TerrainManager.inst.loadRadius);

        ExecuteInNewBoundOnly(newBounds, previousLoadBounds[clientID], (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            long key = Hash.hVec2Int(pos);
            if(chunkToUnload.ContainsKey(key)) {
                chunkToUnload.Remove(key);
            }
            if(!chunkToLoad.ContainsKey(key)) {
                chunkToLoad.Add(key, new ChunkLoadTask(pos, clientID));
            }
        });

        ExecuteInOldBoundOnly(newBounds, previousLoadBounds[clientID], (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            long key = Hash.hVec2Int(pos);
            if(chunkToLoad.ContainsKey(key)) {
                chunkToLoad.Remove(key);
            }
            if(!chunkToUnload.ContainsKey(key)) {
                chunkToUnload.Add(key, pos);
            }
        });

        previousLoadBounds[clientID] = newBounds;
    }

    void LoadBounds (ulong clientID, Vector2Int centerChunkPos) {
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(centerChunkPos, TerrainManager.inst.loadRadius);

        ExecuteInBound(newBounds, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            long key = Hash.hVec2Int(pos);
            if(chunkToUnload.ContainsKey(key)) {
                chunkToUnload.Remove(key);
            }
            if(!chunkToLoad.ContainsKey(key)) {
                chunkToLoad.Add(key, new ChunkLoadTask(pos, clientID));
            }
        });
    }
    #endregion

    #region Load & Unload
    // Returns true if a visual chunk has been or should be loaded
    void LoadChunkAt (ulong clientID, Vector2Int chunkPosition) {
        long key = Hash.hVec2Int(chunkPosition);
        bool doLoadFromFileOrGenerate = false;
        ChunkLoadCounter loadCounter;

        bool chunkLoadedAtPosition = TerrainManager.inst.GetChunkLoadState(chunkPosition) == JobState.Loaded;
        
        #region Manage Load Counters
        if(loadCounters.TryGetValue(key, out ChunkLoadCounter lc)) {
            loadCounter = lc;

            if(loadCounter.loadCount == 0) { // About to be deleted
                if(loadCounter.timer.isRunning) {
                    loadCounter.timer.Cancel(); // Cancel the deletion
                    loadCounter.timer = null;
                } else {
                    if(chunkLoadedAtPosition) {
                        Debug.Log("A loaded chunk with a loadCount of 0 but with no deletion timer currently running has been found");
                    } else {
                        Debug.LogError("A none loaded chunk with a loadCount of 0 and no deletion timer currently running has been found" +
                            "\nIf there is no chunk, the loadCount object should have already been deleted once the timer stopped" +
                            "\nSince there is no timer running, it may have stopped before without deleting the loadCount object");
                        doLoadFromFileOrGenerate = true;
                    }
                }
            } else {
                if(chunkLoadedAtPosition) {
                    Debug.Log("A attempt to generete a chunk where there is already one was made. The counter at that chunk was above 0");
                } else { // No chunk has been found. It must be loaded
                    doLoadFromFileOrGenerate = true;
                }
            }
        } else { // No load counter has been found
            if(!chunkLoadedAtPosition) {
                doLoadFromFileOrGenerate = true;
                loadCounters.Add(key, new ChunkLoadCounter(chunkPosition));
                loadCounter = loadCounters[key];
            } else {
                Debug.Log("A chunk with no counter despite being loaded has been found");
                loadCounters.Add(key, new ChunkLoadCounter(chunkPosition));
                loadCounter = loadCounters[key];
            }
        }
        if(chunkLoadedAtPosition && doLoadFromFileOrGenerate) {
            Debug.Log("An attempt to generate a chunk where there is already one has been caught right before its generation");
            doLoadFromFileOrGenerate = false;
            return;
        }
        #endregion
        loadCounter.loadCount++;

        if(!unloadOnlyMode && (NetworkAssistant.inst.IsServer || (NetworkAssistant.inst.IsHost && NetworkAssistant.inst.ClientID != clientID))) {
            if(TerrainManager.inst.GetChunkAtPosition(chunkPosition, out DataChunk dataChunk)) {
                TerrainManager.inst.SendChunkToClient(clientID, chunkPosition.x, chunkPosition.y, dataChunk);
            }
        }

        // If the chunk's data need to be loaded for real
        if(doLoadFromFileOrGenerate && !unloadOnlyMode) {
            
            // Try to load the data chunk from the files. If this failes, create a brand new chunk
            DataChunk dataChunk = TerrainManager.inst.GetNewDataChunk(chunkPosition, false);
            bool needFullRefresh = false;
            DataLoadMode dlm = GameManager.inst.currentDataLoadMode;
            TerrainManager.inst.StartNewChunkJobAt(
                chunkPosition, 
                JobState.Loaded, 
                GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
                () => { // JOB
                    if(!WorldSaving.inst.LoadChunkFile(dataChunk, dlm)) {
                        #region Generate Empty Chunk
                        for(int x = 0; x < TerrainManager.inst.chunkSize; x++) {
                            for(int y = 0; y < TerrainManager.inst.chunkSize; y++) {
                                float noise = Mathf.Clamp01(
                                    Mathf.PerlinNoise(
                                        (x + chunkPosition.x * TerrainManager.inst.chunkSize) * 0.03f + 90000f,
                                        (y + chunkPosition.y * TerrainManager.inst.chunkSize) * 0.05f + 90000f
                                    ) * 0.8f +
                                    Mathf.PerlinNoise(
                                        (x + chunkPosition.x * TerrainManager.inst.chunkSize) * 0.16f + 90000f,
                                        (y + chunkPosition.y * TerrainManager.inst.chunkSize) * 0.16f + 90000f
                                    ) * 0.2f
                                );

                                dataChunk.SetGlobalID(x, y, TerrainLayers.Ground, noise > 0.5f ? 1 : 0);
                                dataChunk.SetGlobalID(x, y, TerrainLayers.Background, noise > 0.4f ? 2 : 0);
                                dataChunk.SetGlobalID(x, y, TerrainLayers.WaterBackground, 0);
                                dataChunk.SetGlobalID(x, y, TerrainLayers.WaterSurface, 0);
                                dataChunk.SetGlobalID(x, y, TerrainLayers.Overlay, 0);
                            }
                        }
                        needFullRefresh = true;
                        #endregion
                    }
                },
                () => { // CALLBACK
                    TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);

                    if(needFullRefresh) {
                        dataChunk.RefreshTiles();
                    }

                    // This will take care of generating the visual chunks and updating the tile borders.
                    TerrainManager.inst.RefreshSurroundingChunks(chunkPosition);

                    // This will load the region that the chunk is inside if it hasn't already been done.
                    EntityRegionManager.inst.LoadRegionAtChunk(chunkPosition);


                },
                () => { // CALLBACK IF CANCELLED
                    TerrainManager.inst.EnqueueDataChunkToUnusedQueue(dataChunk);
                }
            );
        }
    }

    public void GetChunkAt (Vector2Int chunkPosition, DataChunk dataChunk) {

        dataChunk.chunkPosition = chunkPosition;

        TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);

        // This will take care of generating the visual chunks and updating the tile borders.
        TerrainManager.inst.RefreshSurroundingChunks(chunkPosition);
    }

    void UnloadChunkAt (Vector2Int chunkPosition, bool imidiatly, bool doSave = true) {
        long key = Hash.hVec2Int(chunkPosition);

        if(imidiatly) {
            if(TerrainManager.inst.chunks.TryGetValue(key, out DataChunk dataChunk)) {
                if(TerrainManager.inst.chunkJobsManager.TryGetValue(key, out ChunkJobManager cjm)) {
                    cjm.ForceUnload();
                }

                if(doSave) {
                    WorldSaving.inst.SaveChunk(dataChunk, GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly);
                }
                TerrainManager.inst.SetDataChunkAsUnused(chunkPosition, true);
                VisualChunkManager.inst.UnloadChunkAt(chunkPosition);

                EntityRegionManager.inst.UnloadRegionAtChunk(chunkPosition);
            }
            return;
        }

        if(loadCounters.ContainsKey(key)) {
            int loadCount = loadCounters[key].loadCount;

            if(loadCount - 1 == 0) {
                loadCounters[key].loadCount = 0;
                if(loadCounters[key].timer == null) {
                    loadCounters[key].timer = new CancellableTimer();
                }
                loadCounters[key].timer.Start(TerrainManager.inst.unloadTimer, () => {
                    loadCounters.Remove(key);

                    bool chunkLoadedAtPosition = TerrainManager.inst.GetChunkLoadState(chunkPosition) == JobState.Loaded;
                    if(chunkLoadedAtPosition) {
                        // If the chunk is present data-wise but hasn't been generated yet, 
                        // it will take care of making sure it won't be

                        bool dataChunkExist = TerrainManager.inst.chunks.TryGetValue(Hash.hVec2Int(chunkPosition), out DataChunk dataChunk);
                        if(dataChunkExist) {
                            TerrainManager.inst.SetDataChunkAsUnused(chunkPosition, false);
                        }
                        TerrainManager.inst.StartNewChunkJobAt(
                            chunkPosition,
                            JobState.Unloaded,
                            GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
                            () => { // JOB
                                if(doSave && dataChunkExist) {
                                    WorldSaving.inst.SaveChunk(dataChunk, GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly);
                                }
                            },
                            () => { // CALLBACK
                                if(dataChunkExist) {
                                    TerrainManager.inst.EnqueueDataChunkToUnusedQueue(dataChunk);
                                }
                                EntityRegionManager.inst.UnloadRegionAtChunk(chunkPosition);
                                VisualChunkManager.inst.UnloadChunkAt(chunkPosition);
                            },
                            () => { // CALLBACK IF CANCELLED
                                if(dataChunkExist) {
                                    TerrainManager.inst.EnqueueDataChunkToUnusedQueue(dataChunk);
                                }
                            }
                        );
                    } else {
                        Debug.Log("There was no chunk to unload, they haven't been generated yet?");
                        if(VisualChunkManager.inst.visualChunks.ContainsKey(Hash.hVec2Int(chunkPosition))) {
                            Debug.Log("Visual chunk still remaining? ");
                        }
                        //Debug.LogError("Failed to unload the chunk after the timer runned out, the chunk was not present (Multithreading error. Chunk that hasn't finished loading is being called)");
                    }
                });
            } else if(loadCount - 1 >= 0) {
                loadCounters[key].loadCount--;
            }
        } else {
            if(TerrainManager.inst.chunks.ContainsKey(Hash.hVec2Int(chunkPosition))) {
                Debug.LogError("An attempt to unload a chunk that has no loadCount has been found");
            } else {
                //Debug.LogError("An attempt to unload no chunk has been found");
            }
        }
    }
    #endregion

    #region Load & Unload ALL
    public void UnloadAll (bool doSave = true) {
        foreach(KeyValuePair<long, ChunkLoadCounter> kvp in loadCounters) {
            if(kvp.Value.loadCount > 0) {
                UnloadChunkAt(kvp.Value.position, true, doSave);
            } else {
                kvp.Value.timer.Cancel();
                UnloadChunkAt(kvp.Value.position, true, doSave);
            }
        }
        chunkToLoad.Clear();
        chunkToUnload.Clear();
        loadCounters.Clear();
    }

    public void ReloadAll () {
        foreach(KeyValuePair<ulong, Transform> kvp in playerCenters) {
            Vector2Int currentChunkPos = TerrainManager.inst.WorldToChunk(playerCenters[kvp.Key].position);
            previousChunkPos[kvp.Key] = currentChunkPos;
            ChunkLoadBounds newBounds = new ChunkLoadBounds(
                new Vector2Int(
                    currentChunkPos.x - TerrainManager.inst.loadRadius.x,
                    currentChunkPos.y - TerrainManager.inst.loadRadius.y),
                new Vector2Int(
                    currentChunkPos.x + TerrainManager.inst.loadRadius.x,
                    currentChunkPos.y + TerrainManager.inst.loadRadius.y)
            );

            ExecuteInBound(newBounds, (x, y) => {
                long key = Hash.hVec2Int(x, y);
                if(chunkToUnload.ContainsKey(key)) {
                    chunkToUnload.Remove(key);
                }
                if(chunkToLoad.ContainsKey(key)) {
                    chunkToLoad.Remove(key);
                }
                LoadChunkAt(kvp.Key, new Vector2Int(x, y));
            });
        }
    }
    #endregion

    #region Bounds Execution Utils
    private void ExecuteInBound (ChunkLoadBounds bounds, Action<int, int> action) {
        for(int x = bounds.minimum.x; x < bounds.maximum.x; x++) {
            for(int y = bounds.minimum.y; y < bounds.maximum.y; y++) {
                action.Invoke(x, y);
            }
        }
    }

    private void ExecuteInNewBoundOnly (ChunkLoadBounds newBounds, ChunkLoadBounds oldBounds, Action<int, int> action) {
        for(int x = newBounds.minimum.x; x < newBounds.maximum.x; x++) {
            for(int y = newBounds.minimum.y; y < newBounds.maximum.y; y++) {
                bool isInOldBound = (
                    x >= oldBounds.minimum.x &&
                    x < oldBounds.maximum.x &&
                    y >= oldBounds.minimum.y &&
                    y < oldBounds.maximum.y
                );

                if(!isInOldBound)
                    action.Invoke(x, y);
            }
        }
    }

    private void ExecuteInOldBoundOnly (ChunkLoadBounds newBounds, ChunkLoadBounds oldBounds, Action<int, int> action) {
        for(int x = oldBounds.minimum.x; x < oldBounds.maximum.x; x++) {
            for(int y = oldBounds.minimum.y; y < oldBounds.maximum.y; y++) {
                bool isInNewBound = (
                    x >= newBounds.minimum.x &&
                    x < newBounds.maximum.x &&
                    y >= newBounds.minimum.y &&
                    y < newBounds.maximum.y
                );

                if(!isInNewBound)
                    action.Invoke(x, y);
            }
        }
    }
    #endregion

}
