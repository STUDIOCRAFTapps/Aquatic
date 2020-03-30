using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataChunk {

    #region Header and Init
    public Vector2Int chunkPosition;
    public int chunkSize = 16;
    public Dictionary<TerrainLayers, Tile[][]> tileData;
    public List<int> globalIDPalette;
    public float timeOfLastAutosave = 0f;

    bool[] hasLayerBeenEdited;

    public DataChunk(int chunkSize) {
        this.chunkSize = chunkSize;
        tileData = new Dictionary<TerrainLayers, Tile[][]>();
        globalIDPalette = new List<int>();

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
        timeOfLastAutosave = Time.time;

        for(int i = 0; i < hasLayerBeenEdited.Length; i++) {
            hasLayerBeenEdited[i] = false;
        }
    }

    public void ClearTiles () {
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

    virtual public void RefreshTiles () {
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    if(tileData[(TerrainLayers)l][x][y].gid == 0) {
                        continue;
                    }

                    BaseTileAsset tileAsset = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(GetGlobalID(x, y, (TerrainLayers)l));
                    tileAsset.OnTileRefreshed(new Vector2Int(x + chunkPosition.x * chunkSize, y + chunkPosition.y * chunkSize), (TerrainLayers)l);
                }
            }
        }
    }
    #endregion

    #region Tile Editing / Reading

    public void SetGlobalID (int x, int y, TerrainLayers layer, int gid) {
        hasLayerBeenEdited[(int)layer] = true;

        if(gid != 0 && !globalIDPalette.Contains(gid)) {
            globalIDPalette.Add(gid);
            if(globalIDPalette.Count >= 256) {
                Debug.LogError($"Palette Count exceeded 255 from the DataChunk at {chunkPosition}");
            }
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
}

// Using gid instead of palette indexs if to make everything much much simple in-game at the expense of the saving system
// which is a good choice imo
public struct Tile {
    public int gid;
    public ushort bitmask;
}
