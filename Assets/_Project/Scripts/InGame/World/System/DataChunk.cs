using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.IO;
using System;

public class DataChunk : IDisposable {

    #region Header and Init
    public Vector2Int chunkPosition;
    const int defaultChunkSize = 16;
    public int chunkSize = 16;
    public Dictionary<TerrainLayers, Tile[][]> tileData;

    public List<int> globalIDPalette;
    public HashSet<int> globalIDHashs;

    public float timeOfLastAutosave = 0f;

    bool[] hasLayerBeenEdited;

    public DataChunk() {
        this.chunkSize = defaultChunkSize;
        tileData = new Dictionary<TerrainLayers, Tile[][]>();
        globalIDPalette = new List<int>();
        globalIDHashs = new HashSet<int>();

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            tileData[(TerrainLayers)l] = new Tile[chunkSize][];
            for(int x = 0; x < chunkSize; x++) {
                tileData[(TerrainLayers)l][x] = new Tile[chunkSize];
                for(int y = 0; y < chunkSize; y++) {
                    tileData[(TerrainLayers)l][x][y] = new Tile();
                }
            }
        }

        hasLayerBeenEdited = new bool[TerrainManager.inst.layerParameters.Length];
    }

    virtual public void Init (Vector2Int chunkPosition) {
        this.chunkPosition = chunkPosition;
        globalIDPalette.Clear();
        globalIDHashs.Clear();
        timeOfLastAutosave = Time.time;

        for(int i = 0; i < hasLayerBeenEdited.Length; i++) {
            hasLayerBeenEdited[i] = false;
        }
    }

    public void ClearTiles () {
        globalIDPalette.Clear();
        globalIDHashs.Clear();

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            hasLayerBeenEdited[l] = false;
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    SetGlobalID(x, y, (TerrainLayers)l, 0);
                    SetBitmask(x, y, (TerrainLayers)l, 0);
                }
            }
        }
    }

    public void ClearTilesLayer (int l) {
        hasLayerBeenEdited[l] = false;
        for(int x = 0; x < chunkSize; x++) {
            for(int y = 0; y < chunkSize; y++) {
                SetGlobalID(x, y, (TerrainLayers)l, 0);
                SetBitmask(x, y, (TerrainLayers)l, 0);
            }
        }
    }

    public void RefreshLayerEdit () {
        globalIDPalette.Clear();
        globalIDHashs.Clear();

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!HasLayerBeenEdited((TerrainLayers)l)) {
                continue;
            }

            bool isAllEmpty = true;
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    int gid = tileData[(TerrainLayers)l][x][y].gid;

                    if(gid != 0) {
                        isAllEmpty = false;

                        if(!globalIDHashs.Contains(gid)) {
                            globalIDHashs.Add(gid);
                            globalIDPalette.Add(gid);
                        }
                    }
                }
            }

            if(isAllEmpty) {
                hasLayerBeenEdited[l] = false;
            }
        }
    }

    public void ClearLayerEdits () {
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            hasLayerBeenEdited[l] = false;
        }
    }
    #endregion

    #region Tile Editing / Reading

    public void SetGlobalID (int x, int y, TerrainLayers layer, int gid) {
        hasLayerBeenEdited[(int)layer] = hasLayerBeenEdited[(int)layer] || gid != 0;

        if(gid != 0 && !globalIDHashs.Contains(gid)) {
            if(globalIDHashs.Count >= 255) {
                Debug.LogError($"Palette Count exceeded 255 from the DataChunk at {chunkPosition}");
                return;
            }
            globalIDPalette.Add(gid);
            globalIDHashs.Add(gid);
        }
        tileData[layer][x][y].gid = gid;
    }

    virtual public int GetGlobalID (int x, int y, TerrainLayers layer) {
        if(x < 0 || y < 0 || x >= chunkSize || y >= chunkSize) {
            return 0;
        }
        return tileData[layer][x][y].gid;
    }

    public void SetBitmask (int x, int y, TerrainLayers layer, ushort bitmask) {
        tileData[layer][x][y].bitmask = bitmask;
    }

    public ushort GetBitmask (int x, int y, TerrainLayers layer) {
        return tileData[layer][x][y].bitmask;
    }

    public bool HasLayerBeenEdited (TerrainLayers layer) {
        return hasLayerBeenEdited[(int)layer];
    }
    #endregion

    #region Refreshes
    public virtual void RefreshTiles () {
        RefreshTiles(4, false);
    }

    public void RefreshTiles (int pattern, bool cropMiddle = true) {
        int border = 2;

        int min_x = (refreshPatterns[pattern * 4 + 0] == 0) ? 0 : chunkSize - border;
        int max_x = (refreshPatterns[pattern * 4 + 1] == 0) ? chunkSize : border;
        int min_y = (refreshPatterns[pattern * 4 + 2] == 0) ? 0 : chunkSize - border;
        int max_y = (refreshPatterns[pattern * 4 + 3] == 0) ? chunkSize : border;

        bool isChunkUp = TerrainManager.inst.chunks.ContainsKey(Hash.longFrom2D(chunkPosition + Vector2Int.up));
        bool isChunkDown = TerrainManager.inst.chunks.ContainsKey(Hash.longFrom2D(chunkPosition + Vector2Int.down));
        bool isChunkLeft = TerrainManager.inst.chunks.ContainsKey(Hash.longFrom2D(chunkPosition + Vector2Int.left));
        bool isChunkRight = TerrainManager.inst.chunks.ContainsKey(Hash.longFrom2D(chunkPosition + Vector2Int.right));

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = min_x; x < max_x; x++) {
                for(int y = min_y; y < max_y; y++) {
                    // No need to refresh the inside of a full chunk
                    if(cropMiddle && pattern == 4 && (x >= border && x < chunkSize - border) && (y >= border && y < chunkSize - border)) {
                        continue;
                    } else if(pattern == 4) {
                        if(!isChunkUp && y >= chunkSize - border) {
                            continue;
                        }
                        if(!isChunkDown && y < border) {
                            continue;
                        }
                        if(!isChunkLeft && x < border) {
                            continue;
                        }
                        if(!isChunkRight && x >= chunkSize - border) {
                            continue;
                        }
                    }
                    if(tileData[(TerrainLayers)l][x][y].gid == 0) {
                        continue;
                    }
                    BaseTileAsset tileAsset = GeneralAsset.inst.GetTileAssetFromGlobalID(GetGlobalID(x, y, (TerrainLayers)l));
                    tileAsset.OnTileRefreshed(new Vector2Int(x + chunkPosition.x * chunkSize, y + chunkPosition.y * chunkSize), (TerrainLayers)l);
                }
            }
            TerrainManager.inst.QueueChunkReloadAtTile(chunkPosition.x * chunkSize, chunkPosition.y * chunkSize, (TerrainLayers)l);
        }
    }

    //x - min/max, y - min/max
    static readonly int[] refreshPatterns = {
        -1,  0, -1,  0, //bottom-left
         0,  0, -1,  0, //bottom
         0,  1, -1,  0, //bottom-right
        -1,  0,  0,  0, //left
         0,  0,  0,  0, //center
         0,  1,  0,  0, //right
        -1,  0,  0,  1, //top-left
         0,  0,  0,  1, //top
         0,  1,  0,  1, //top-right
    };
    #endregion

    public void Dispose () {
        TerrainManager.inst.EnqueueDataChunkToUnusedQueue(this);
    }
}

// Using gid instead of palette indexs if to make everything much much simple in-game at the expense of the saving system
public struct Tile {
    public int gid;
    public ushort bitmask;
}
