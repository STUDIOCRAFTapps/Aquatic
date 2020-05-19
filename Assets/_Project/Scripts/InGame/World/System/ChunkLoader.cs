using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class ChunkLoader : MonoBehaviour {

    public Transform player;
    public Dictionary<long, ChunkLoadCounter> loadCounters;
    public Dictionary<long, Vector2Int> chunkToLoad;
    public Dictionary<long, Action> chunkToUnload;

    public static ChunkLoader inst;

    bool hasMovedSinceInit = false;
    Vector2Int previousChunkPos;
    ChunkLoadBounds previousLoadBounds;

    void Awake () {
        loadCounters = new Dictionary<long, ChunkLoadCounter>();
        chunkToLoad = new Dictionary<long, Vector2Int>();
        chunkToUnload = new Dictionary<long, Action>();

        if(inst == null) {
            inst = this;
        }
    }
    
    private void Update () {
        if(chunkToLoad.Count > 0) {
            long firstKey = chunkToLoad.First().Key;
            LoadChunkAt(chunkToLoad[firstKey]);
            chunkToLoad.Remove(firstKey);
        }
        if(chunkToUnload.Count > 0) {
            long firstKey = chunkToUnload.First().Key;
            chunkToUnload[firstKey]();
            chunkToUnload.Remove(firstKey);
        }
    }

    void FixedUpdate () {
        TrackPlayerMovement();
    }

    #region Move Tracking
    void TrackPlayerMovement () {
        Vector2Int currentChunkPos = TerrainManager.inst.WorldToChunk(player.position);

        if(!hasMovedSinceInit) {
            hasMovedSinceInit = true;

            previousChunkPos = currentChunkPos;
            MoveLoadBounds(currentChunkPos);
            return;
        }

        if(currentChunkPos != previousChunkPos) {
            MoveLoadBounds(currentChunkPos);
            previousChunkPos = currentChunkPos;
        }
    }

    void MoveLoadBounds (Vector2Int centerChunkPos) {
        ChunkLoadBounds newBounds = new ChunkLoadBounds(
            new Vector2Int(
                centerChunkPos.x - TerrainManager.inst.loadRadius.x,
                centerChunkPos.y - TerrainManager.inst.loadRadius.y),
            new Vector2Int(
                centerChunkPos.x + TerrainManager.inst.loadRadius.x,
                centerChunkPos.y + TerrainManager.inst.loadRadius.y)
        );

        ExecuteInNewBoundOnly(newBounds, previousLoadBounds, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            long key = Hash.hVec2Int(pos);
            if(chunkToUnload.ContainsKey(key)) {
                chunkToUnload.Remove(key);
            }
            if(!chunkToLoad.ContainsKey(key)) {
                chunkToLoad.Add(key, pos);
            }
        });

        ExecuteInOldBoundOnly(newBounds, previousLoadBounds, (x, y) => {
            Vector2Int pos = new Vector2Int(x, y);
            long key = Hash.hVec2Int(pos);
            if(chunkToLoad.ContainsKey(key)) {
                chunkToLoad.Remove(key);
            }
            UnloadChunkAt(pos, false);
        });

        previousLoadBounds = newBounds;
    }


    #endregion

    #region Load & Unload
    // Returns true if a visual chunk has been or should be loaded
    void LoadChunkAt (Vector2Int chunkPosition) {
        long key = Hash.hVec2Int(chunkPosition);
        // Manage load counters
        // - If a chunk was running a timer to get deleted, cancel it
        bool doLoadFromFileOrGenerate = false;
        if(loadCounters.TryGetValue(key, out ChunkLoadCounter loadCounter)) {
            if(loadCounter.loadCount == 0) {
                if(loadCounter.timer.isRunning) {
                    loadCounter.timer.Cancel();
                    loadCounter.timer = new CancellableTimer();
                    loadCounter.loadCount++;
                } else {
                    doLoadFromFileOrGenerate = true;
                }
            } else {
                loadCounter.loadCount++;
                
                if(!TerrainManager.inst.chunks.ContainsKey(Hash.hVec2Int(chunkPosition))) {
                    doLoadFromFileOrGenerate = true;
                }
            }
        } else {
            if(!TerrainManager.inst.chunks.ContainsKey(Hash.hVec2Int(chunkPosition))) {
                doLoadFromFileOrGenerate = true;
            } else {
                loadCounters.Add(key, new ChunkLoadCounter(chunkPosition));
            }
        }
        if(TerrainManager.inst.chunks.ContainsKey(Hash.hVec2Int(chunkPosition))) {
            doLoadFromFileOrGenerate = false;
        }

        // If the chunk's data need to be loaded for real!
        if(doLoadFromFileOrGenerate) {

            // Augment the load counter of the chunk once it gets loaded
            if(!loadCounters.ContainsKey(key)) {
                loadCounters.Add(key, new ChunkLoadCounter(chunkPosition));
            }
            loadCounters[key].loadCount++;

            // Try to load the data chunk from the files. If this failes, create a new chunk
            DataChunk dataChunk = TerrainManager.inst.GetNewDataChunk(chunkPosition);
            if(!WorldSaving.inst.LoadChunkFile(dataChunk, GameManager.inst.currentDataLoadMode)) {
                #region Generate Empty Chunk
                for(int x = 0; x < TerrainManager.inst.chunkSize; x++) {
                    for(int y = 0; y < TerrainManager.inst.chunkSize; y++) {
                        dataChunk.SetGlobalID(x, y, TerrainLayers.Ground, Mathf.RoundToInt(Mathf.Clamp01(
                            Mathf.PerlinNoise(
                                (x + chunkPosition.x * TerrainManager.inst.chunkSize) * 0.08f + 90000f,
                                (y + chunkPosition.y * TerrainManager.inst.chunkSize) * 0.08f + 90000f
                            ) * 0.8f +
                            Mathf.PerlinNoise(
                                (x + chunkPosition.x * TerrainManager.inst.chunkSize) * 0.21f + 90000f,
                                (y + chunkPosition.y * TerrainManager.inst.chunkSize) * 0.21f + 90000f
                            ) * 0.2f
                        )));
                        dataChunk.SetGlobalID(x, y, TerrainLayers.Background, 0);
                        dataChunk.SetGlobalID(x, y, TerrainLayers.WaterBackground, 0);
                        dataChunk.SetGlobalID(x, y, TerrainLayers.WaterSurface, 0);
                    }
                }
                dataChunk.RefreshTiles();
                #endregion
            }

            // This will take care of generating the visual chunks later.
            TerrainManager.inst.RefreshSurroundingChunks(chunkPosition);

            EntityRegionManager.inst.LoadRegionAtChunk(chunkPosition);
        }
    }

    void UnloadChunkAt (Vector2Int chunkPosition, bool imidiatly, bool doSave = true) {
        long key = Hash.hVec2Int(chunkPosition);
        if(imidiatly) {
            if(TerrainManager.inst.chunks.TryGetValue(Hash.hVec2Int(chunkPosition), out DataChunk dataChunk)) {
                if(doSave) {
                    WorldSaving.inst.SaveChunk(dataChunk);
                }
                TerrainManager.inst.SetDataChunkAsUnused(chunkPosition);
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
                    if(!chunkToUnload.ContainsKey(key)) {
                        chunkToUnload.Add(key, () => {
                            if(TerrainManager.inst.chunks.TryGetValue(Hash.hVec2Int(chunkPosition), out DataChunk dataChunk)) {
                                if(doSave) {
                                    WorldSaving.inst.SaveChunk(dataChunk);
                                }
                                TerrainManager.inst.SetDataChunkAsUnused(chunkPosition);
                                VisualChunkManager.inst.UnloadChunkAt(chunkPosition);

                                EntityRegionManager.inst.UnloadRegionAtChunk(chunkPosition);
                            }
                        });
                    }
                });
            } else if(loadCount - 1 >= 0) {
                loadCounters[key].loadCount--;
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
        loadCounters.Clear();
    }

    public void ReloadAll () {
        Vector2Int currentChunkPos = TerrainManager.inst.WorldToChunk(player.position);
        previousChunkPos = currentChunkPos;
        ChunkLoadBounds newBounds = new ChunkLoadBounds(
            new Vector2Int(
                currentChunkPos.x - TerrainManager.inst.loadRadius.x,
                currentChunkPos.y - TerrainManager.inst.loadRadius.y),
            new Vector2Int(
                currentChunkPos.x + TerrainManager.inst.loadRadius.x,
                currentChunkPos.y + TerrainManager.inst.loadRadius.y)
        );

        ExecuteInBound(newBounds, (x, y) => {
            LoadChunkAt(new Vector2Int(x, y));
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
