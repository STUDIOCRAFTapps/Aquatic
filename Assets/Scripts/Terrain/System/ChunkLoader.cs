using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ChunkLoader : MonoBehaviour {

    public Transform player;
    public Dictionary<Vector2Int, ChunkLoadCounter> loadCounters;

    public static ChunkLoader inst;

    bool hasMovedSinceInit = false;
    Vector2Int previousChunkPos;
    ChunkLoadBounds previousLoadBounds;

    void Awake () {
        loadCounters = new Dictionary<Vector2Int, ChunkLoadCounter>();

        if(inst == null) {
            inst = this;
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
            LoadChunkAt(new Vector2Int(x, y));
        });
        ExecuteInOldBoundOnly(newBounds, previousLoadBounds, (x, y) => {
            UnloadChunkAt(new Vector2Int(x, y));
        });

        previousLoadBounds = newBounds;
    }
    #endregion

    void LoadChunkAt (Vector2Int chunkPosition) {
        bool doGenerate = false;
        if(loadCounters.ContainsKey(chunkPosition)) {
            if(loadCounters[chunkPosition].loadCount == 0) {
                if(loadCounters[chunkPosition].timer.isRunning) {
                    loadCounters[chunkPosition].timer.Cancel();
                    loadCounters[chunkPosition].timer = new CancellableTimer();
                    loadCounters[chunkPosition].loadCount++;
                } else {
                    doGenerate = true;
                }
            } else {
                loadCounters[chunkPosition].loadCount++;
                
                if(!TerrainManager.inst.chunks.ContainsKey(chunkPosition)) {
                    doGenerate = true;
                }
            }
        } else {
            doGenerate = true;
        }

        if(doGenerate) {
            if(!loadCounters.ContainsKey(chunkPosition)) {
                loadCounters.Add(chunkPosition, new ChunkLoadCounter());
            }
            loadCounters[chunkPosition].loadCount++;
            
            DataChunk dataChunk = TerrainManager.inst.GetNewDataChunk(chunkPosition);
            if(!DataChunkSaving.inst.LoadChunk(dataChunk)) {
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
                //dataChunk.ClearTiles();
            }
            TerrainManager.inst.RefreshSurroundingChunks(chunkPosition, 2);

            VisualChunkManager.inst.LoadChunkAt(chunkPosition);
        }
        
        EntityRegionManager.inst.LoadRegionAtChunk(chunkPosition);
    }

    void UnloadChunkAt (Vector2Int chunkPosition) {
        if(loadCounters.ContainsKey(chunkPosition)) {
            int loadCount = loadCounters[chunkPosition].loadCount;

            if(loadCount - 1 == 0) {
                loadCounters[chunkPosition].loadCount = 0;
                if(loadCounters[chunkPosition].timer == null) {
                    loadCounters[chunkPosition].timer = new CancellableTimer();
                }
                loadCounters[chunkPosition].timer.Start(TerrainManager.inst.unloadTimer, () => {
                    if(TerrainManager.inst.chunks.ContainsKey(chunkPosition)) {
                        DataChunkSaving.inst.SaveChunk(TerrainManager.inst.chunks[chunkPosition]);
                        TerrainManager.inst.SetDataChunkAsUnused(chunkPosition);
                        VisualChunkManager.inst.UnloadChunkAt(chunkPosition);

                        EntityRegionManager.inst.UnloadRegionAtChunk(chunkPosition);
                    }
                });
            } else if(loadCount - 1 >= 0) {
                loadCounters[chunkPosition].loadCount--;
            }
        }
    }

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
