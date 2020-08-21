using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;

public class ChunkLoader : MonoBehaviour {

    public static ChunkLoader inst;
    public bool unloadOnlyMode = false; // The unload only mode will prevent this chunk loader instance 
    const int taskPerFrame = 2;         // from loading chunks itself. It will instead wait for the server
                                        // to send a chunk to load
    
    // Per chunk data
    public Dictionary<long, ChunkLoadCounter> loadCounters;

    // Per player data
    public Dictionary<ulong, ChunkLoaderEntity> loaderEntities;
    

    #region Monobehaviour
    void Awake () {
        loadCounters = new Dictionary<long, ChunkLoadCounter>();
        loaderEntities = new Dictionary<ulong, ChunkLoaderEntity>();


        // If entities have joined before the chunk loader was present, they must be added
        if(NetworkAssistant.inst.playerToAddToChunkLoader.Count > 0) {
            foreach(KeyValuePair<ulong, Transform> kvp in NetworkAssistant.inst.playerToAddToChunkLoader) {
                AddPlayer(kvp.Key, kvp.Value);
            }
            NetworkAssistant.inst.playerToAddToChunkLoader.Clear();
        }

        unloadOnlyMode = !NetworkAssistant.inst.IsServer;

        if(inst == null) {
            inst = this;
        }
    }

    void FixedUpdate () {
        foreach(KeyValuePair<ulong, ChunkLoaderEntity> kvp in loaderEntities) {
            TrackPlayerMovement(kvp.Value);
        }
    }
    #endregion

    #region Adding & Removing Players
    public void AddPlayer (ulong clientID, Transform center) {

        // If this instance of a chunk loader isn't on a server, other client shouldn't be taken into account
        if(!NetworkAssistant.inst.IsServer) {
            if(clientID != NetworkAssistant.inst.ClientID) {
                return;
            }
        }

        loaderEntities.Add(clientID, new ChunkLoaderEntity(clientID, center));
    }

    public void RemovePlayer (ulong clientID) {

        // If this instance of a chunk loader isn't on a server, other client shouldn't be taken into account
        if(!NetworkAssistant.inst.IsServer) {
            if(clientID != NetworkAssistant.inst.ClientID) {
                return;
            }
        }

        loaderEntities[clientID].OnRemove();
        loaderEntities.Remove(clientID);
    }
    #endregion

    #region Managing Load Counters
    bool TryLoadAt (ulong clientID, Vector2Int chunkPosition) {

        long key = Hash.longFrom2D(chunkPosition);
        bool isChunkLoaded = TerrainManager.inst.GetChunkLoadState(chunkPosition) == JobState.Loaded;
        bool doLoadChunk = false;
        bool increaseCounter = false;

        // First, we must check if there is already a load counter in place for this chunk
        ChunkLoadCounter loadCounter;
        if(loadCounters.TryGetValue(key, out loadCounter)) {

            // A load counter has been found.
            if(loadCounter.loaders.Count == 0) {

                // No one is using it. A timer should be running to unload it.
                if(loadCounter.timer != null && loadCounter.timer.isRunning) {

                    // A timer was running to unload the chunk.
                    // Cancel the timer. No need to generate chunk, it should already be there.

                    // THIS IS A VALID STATE
                    loadCounter.timer.Cancel();
                    loadCounter.timer = null;
                    doLoadChunk = false;
                    increaseCounter = true;

                    if(!isChunkLoaded) {
                        //Debug.LogError("A timer was running to unload a chunk that seems to be already unloaded.");
                    }
                } else {

                    // No timers are running.
                    // There is something wrong with the system.
                    if(isChunkLoaded) {
                        doLoadChunk = false;
                        increaseCounter = false;
                        Debug.LogError("A load counter of 0 where a chunk should be loaded but no deletion timer currently running has been found.");
                    } else {
                        doLoadChunk = true;
                        increaseCounter = false;
                        Debug.LogError("A load counter of 0 where a chunk should be unloaded and no deletion timer currently running has been found.");
                    }
                }
            } else {

                // According to the counter, the chunk is already loader by other clients.
                if(isChunkLoaded) {

                    // THIS IS A VALID STATE
                    doLoadChunk = false;
                    increaseCounter = true;
                } else {
                    doLoadChunk = true;
                    increaseCounter = true;
                    Debug.LogError("A load counter above 0 where a chunk should be loaded, but it isn't was found. An attempt to load the chunk was made.");
                }
            }
        } else {

            // No load counter has been found, let's create one.
            if(!isChunkLoaded) {
                // THIS IS A VALID STATE
                doLoadChunk = true;
                increaseCounter = true;

                loadCounter = new ChunkLoadCounter(chunkPosition);
                loadCounters.Add(key, loadCounter);
            } else {
                doLoadChunk = false;
                //increaseCounter = true;

                //loadCounter = new ChunkLoadCounter(chunkPosition);
                //loadCounters.Add(key, loadCounter);
                Debug.LogError("A chunk with no counter despite being loaded has been found");
            }
        }

        // Error verification
        if(isChunkLoaded && doLoadChunk) {
            doLoadChunk = false;
            Debug.LogError("An attempt to generate a chunk where there is already one has been caught. There is something wrong with the state machine.");
        }

        // Incrementing counter
        if(increaseCounter) {
            if(loadCounter.loaders.Contains(clientID)) {
                doLoadChunk = false;
                //Debug.LogError($"Client {clientID} is already loading that chunk.");
            } else {
                loadCounter.loaders.Add(clientID);
            }
        }

        return doLoadChunk;
    }

    void TryUnloadAt (ulong clientID, Vector2Int chunkPosition, Action timerCallback) {

        long key = Hash.longFrom2D(chunkPosition);
        bool isChunkLoaded = TerrainManager.inst.GetChunkLoadState(chunkPosition) == JobState.Loaded;

        // First, we must check if there is already a load counter in place for this chunk, as there should be.
        if(!loadCounters.TryGetValue(key, out ChunkLoadCounter loadCounter)) {

            // There isn't any load counter, this is not normal.
            if(TerrainManager.inst.chunks.ContainsKey(Hash.longFrom2D(chunkPosition))) {
                //Debug.LogError("An attempt to unload a chunk that has no load counter was made.");
            } else {
                if(!isChunkLoaded) {
                    //Debug.LogError("An attempt to unload chunk that may not exist but that still has a load counter was made.");
                }
            }
            return;
        }

        if(!loadCounters[key].loaders.Remove(clientID)) {
            Debug.LogWarning($"Removing client failed. {chunkPosition}");
        }

        //THIS IS A VALID STATE
        // If there's nobody loading this chunk anymore, start the timer.
        if(loadCounter.loaders.Count == 0) {

            // This chunk must be unloaded in a certain amout of time. It's time to prepare a timer if there isn't any, and to clear the load counter.
            loadCounters[key].loaders.Clear();
            if(loadCounters[key].timer == null) {
                loadCounters[key].timer = new CancellableTimer();
            }

            // Scheduling a callback for when the timer will end.
            loadCounters[key].timer.Start(TerrainManager.inst.unloadTimer, () => {

                // When a timer ends, the load counter won't be needed.
                loadCounters.Remove(key);

                timerCallback();
            });
        }
    }
    #endregion


    #region Move Tracking
    void TrackPlayerMovement (ChunkLoaderEntity player) {
        player.GetNewBounds();

        if(player.currentChunkPos != player.previousChunkPos) {
            MoveLoadBounds(player);
        }
    }

    void MoveLoadBounds (ChunkLoaderEntity player) {
        ExecuteInNewBoundOnly(player.currentLoadBounds, player.previousLoadBounds, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            LoadChunkAt(player.clientID, pos);
        });

        ExecuteInOldBoundOnly(player.currentLoadBounds, player.previousLoadBounds, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            UnloadChunkAt(player.clientID, pos);
        });
    }
    #endregion

    #region Load & Unload (Single Chunk, Single Client, Action only)
    void LoadChunkAt (ulong clientID, Vector2Int chunkPosition) {
        
        // Checks if this chunk loader is running on a server, or a host and this load chunk isn't for a the host's client.
        bool clientNeedChunk = !unloadOnlyMode && ((NetworkAssistant.inst.IsServer && !NetworkAssistant.inst.IsHost) || (NetworkAssistant.inst.IsHost && NetworkAssistant.inst.ClientID != clientID));

        // Will attempt to increment the load counter
        bool doLoadChunk = TryLoadAt(clientID, chunkPosition);
        
        bool doSendToClient = true;
        if(clientNeedChunk && doSendToClient) {
            if(TerrainManager.inst.GetChunkAtPosition(chunkPosition, out DataChunk dataChunk)) {
                doSendToClient = false;
                TerrainManager.inst.SendChunkToClient(clientID, chunkPosition.x, chunkPosition.y, dataChunk);
            }
        }

        // If the chunk's data need to be loaded or not.
        if(doLoadChunk && !unloadOnlyMode) {

            // Try to load the data chunk from the files. If this failes, create a brand new chunk
            DataChunk dataChunk = TerrainManager.inst.GetNewDataChunk(chunkPosition, false);
            DataLoadMode dlm = GameManager.inst.currentDataLoadMode;
            bool needFullRefresh = false;
            
            TerrainManager.inst.StartNewChunkJobAt(
                chunkPosition,
                JobState.Loaded,
                GameManager.inst.currentDataLoadMode == DataLoadMode.DefaultElseReadonly,
                () => { // JOB
                    if(!WorldSaving.inst.LoadChunkFile(dataChunk, dlm)) {
                        needFullRefresh = true;
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
                        #endregion
                    }
                },
                () => { // CALLBACK
                    TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);

                    if(needFullRefresh) {
                        dataChunk.RefreshTiles();
                    }

                    // This will take care of refreshing the bitmask of the chunks
                    TerrainManager.inst.RefreshSurroundingChunks(chunkPosition);

                    // This will load the region that the chunk is inside if it hasn't already been done.
                    EntityRegionManager.inst.LoadRegionAtChunk(chunkPosition);

                    if(clientNeedChunk && doSendToClient) {
                        TerrainManager.inst.SendChunkToClient(clientID, chunkPosition.x, chunkPosition.y, dataChunk);
                    }
                },
                () => { // CALLBACK IF CANCELLED
                    TerrainManager.inst.EnqueueDataChunkToUnusedQueue(dataChunk);
                },
                runImidialty: false
            );
        }
    }   

    public void GetChunkFromServerAt (Vector2Int chunkPosition, DataChunk dataChunk) {
        
        ulong clientID = NetworkAssistant.inst.ClientID;
        long key = Hash.longFrom2D(chunkPosition);
        dataChunk.chunkPosition = chunkPosition;

        // If there's no chunk yet at this position, one will be made else, increment
        if(loadCounters.ContainsKey(key)) {
            if(loadCounters[key].timer != null) {
                if(loadCounters[key].timer.isRunning) {
                    loadCounters[key].timer.Cancel();
                    loadCounters[key].timer = null;
                }
            }

            // Add if wasn't there.
            loadCounters[key].loaders.Add(clientID);

            if(TerrainManager.inst.chunks.ContainsKey(key)) {
                return;
            }

            // The chunk can now be set as loaded
            TerrainManager.inst.StartNewChunkJobAt(chunkPosition, JobState.Loaded, GameManager.inst.currentDataLoadMode == DataLoadMode.DefaultElseReadonly, null, null, null, true);
            TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);

            // This will take care of calculating the bitmasks.
            TerrainManager.inst.RefreshSurroundingChunks(chunkPosition, true);
        } else {
            if(TerrainManager.inst.chunks.ContainsKey(key)) {
                return;
            }

            loadCounters.Add(key, new ChunkLoadCounter(chunkPosition));
            loadCounters[key].loaders.Add(clientID);

            // The chunk can now be set as loaded
            TerrainManager.inst.StartNewChunkJobAt(chunkPosition, JobState.Loaded, GameManager.inst.currentDataLoadMode == DataLoadMode.DefaultElseReadonly, null, null, null, true);
            TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);

            // This will take care of calculating the bitmasks.
            TerrainManager.inst.RefreshSurroundingChunks(chunkPosition, true);

            //Since there was no loaders, let's start a timer to unload it.
            UnloadChunkAt(clientID, chunkPosition, false);
        }
    }

    void UnloadChunkAt (ulong clientID, Vector2Int chunkPosition, bool doSave = true) {

        // The validity of the unload process is all determined by the load counter state machine in the method bellow.
        TryUnloadAt(clientID, chunkPosition, () => {
            bool chunkShouldBeLoaded = TerrainManager.inst.GetChunkLoadState(chunkPosition) == JobState.Loaded;

            if(chunkShouldBeLoaded) {

                // If the chunk is present data-wise but hasn't been generated yet, 
                // this will take care of making sure it won't be
                bool dataChunkExist = TerrainManager.inst.chunks.TryGetValue(Hash.longFrom2D(chunkPosition), out DataChunk dataChunk);
                if(dataChunkExist) {
                    TerrainManager.inst.RemoveDataChunkFromDictionnairy(chunkPosition, false);
                }

                DataSaveMode dataSaveMode = GameManager.inst.currentDataSaveMode;

                // A new unload job must be send to take care of the rest of the unload process
                TerrainManager.inst.StartNewChunkJobAt(
                    chunkPosition,
                    JobState.Unloaded,
                    GameManager.inst.currentDataLoadMode == DataLoadMode.DefaultElseReadonly,
                    () => { // JOB
                         if(doSave && dataChunkExist) {
                            WorldSaving.inst.SaveChunk(dataChunk, dataSaveMode);
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
                            //TerrainManager.inst.EnqueueDataChunkToUnusedQueue(dataChunk);
                            TerrainManager.inst.AddDataChunkToDictionnairy(dataChunk);
                        }
                        
                        //To verify; should the data chunk be readed to the dictionnairy or not?
                    },
                    runImidialty: false
                );
            } else {
                if(!unloadOnlyMode) {
                    Debug.Log("There is no chunk to unload, it's already unloaded");
                }
            }
        });
    }

    void UnloadChunkImidiatly (ulong clientID, Vector2Int chunkPosition, bool doSave = true) {
        long key = Hash.longFrom2D(chunkPosition);

        // Unload by force the chunk job managers (This will set the state of the chunk as unloaded)
        TerrainManager.inst.ForceUnloadJobManager(key);

        // Unload data/visual only if the chunk is present
        if(TerrainManager.inst.chunks.TryGetValue(key, out DataChunk dataChunk)) {

            if(doSave) WorldSaving.inst.SaveChunk(dataChunk, GameManager.inst.currentDataSaveMode);
            TerrainManager.inst.RemoveDataChunkFromDictionnairy(chunkPosition, true);
            VisualChunkManager.inst.UnloadChunkAt(chunkPosition);

            EntityRegionManager.inst.UnloadRegionAtChunk(chunkPosition);
        }
    }
    #endregion

    #region Load & Unload (All Chunks, All Clients, Both action and requests)
    public void UnloadAll (bool doSave = true) {

        // This will completly clear all chunks, load/unload request, load counters and timers.
        // A list is being made in case the unload chunk function would edit the load counters dictionnairy
        List<KeyValuePair<long, ChunkLoadCounter>> loadCountersKvp = loadCounters.ToList();
        foreach(KeyValuePair<long, ChunkLoadCounter> kvp in loadCountersKvp) {

            // Unload the chunks. Note that the clientID won't be used since the action will be imediate
            UnloadChunkImidiatly(0, kvp.Value.position, doSave);

            // Cancel all timers that are present and running
            if(kvp.Value.timer != null && kvp.Value.timer.isRunning) {
                kvp.Value.timer.Cancel();
            }
        }
        loadCounters.Clear();
    }

    public void LoadAll () {
        foreach(KeyValuePair<ulong, ChunkLoaderEntity> kvp in loaderEntities) {
            kvp.Value.ReloadSurroundings();
        }
    }
    #endregion

    #region Load & Unload (Chunks in Bounds, Single Client, Request only)
    public void LoadBounds (ulong clientID, ChunkLoadBounds bound) {
        ExecuteInBound(bound, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);

            LoadChunkAt(clientID, pos);
        });
    }

    public void UnloadBounds (ulong clientID, ChunkLoadBounds bound) {
        ExecuteInBound(bound, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);

            UnloadChunkAt(clientID, pos);
        });
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

public class ChunkLoaderEntity {
    public ulong clientID { get; private set; }
    public Transform center { get; private set; }

    public Vector2Int previousChunkPos { get; private set; }
    public ChunkLoadBounds previousLoadBounds { get; private set; }

    public Vector2Int currentChunkPos { get; private set; }
    public ChunkLoadBounds currentLoadBounds { get; private set; }

    public ChunkLoaderEntity (ulong clientID, Transform center) {
        this.center = center;
        this.clientID = clientID;

        OnInit();
    }

    /// <summary>
    /// This is already called by the constructor
    /// </summary>
    public void OnInit () {
        ReloadSurroundings();
    }

    public void OnRemove () {
        Vector2Int newChunkPos = TerrainManager.inst.WorldToChunk(center.position);
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(newChunkPos, TerrainManager.inst.loadRadius);

        ChunkLoader.inst.UnloadBounds(clientID, newBounds);
    }

    public void ReloadSurroundings () {
        Vector2Int newChunkPos = TerrainManager.inst.WorldToChunk(center.position);
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(newChunkPos, TerrainManager.inst.loadRadius);

        currentChunkPos = newChunkPos;
        currentLoadBounds = newBounds;
        previousChunkPos = newChunkPos;
        previousLoadBounds = newBounds;

        ChunkLoader.inst.LoadBounds(clientID, newBounds);
    }

    public void GetNewBounds () {
        Vector2Int newChunkPos = TerrainManager.inst.WorldToChunk(center.position);
        ChunkLoadBounds newBounds = ChunkLoadBounds.BoundsFromRadius(newChunkPos, TerrainManager.inst.loadRadius);

        previousChunkPos = currentChunkPos;
        previousLoadBounds = currentLoadBounds;

        currentChunkPos = newChunkPos;
        currentLoadBounds = newBounds;
    }
}
