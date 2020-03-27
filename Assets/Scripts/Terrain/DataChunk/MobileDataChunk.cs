using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileDataChunk : DataChunk {

    #region Header and Init
    public MobileChunk mobileChunk;
    public Vector2Int restrictedSize;

    public MobileDataChunk (int chunkSize) : base(chunkSize) {
    }

    new public void Init (Vector2Int restrictedSize) {
        this.restrictedSize = restrictedSize;
        chunkPosition = Vector2Int.zero;
        globalIDPalette.Clear();
        timeOfLastAutosave = Time.time;
    }
    #endregion

    #region Tile Editing / Reading
    override public void RefreshTiles () {
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    if(tileData[(TerrainLayers)l][x][y].gid == 0) {
                        continue;
                    }

                    BaseTileAsset tileAsset = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(GetGlobalID(x, y, (TerrainLayers)l));
                    tileAsset.OnTileRefreshed(new Vector2Int(x, y), (TerrainLayers)l, this);
                }
            }
        }
        TerrainManager.inst.QueueMobileChunkReload(mobileChunk.uid);
    }

    override public int GetGlobalID (int x, int y, TerrainLayers layer) {
        if(x < 0 || y < 0 || x >= restrictedSize.x || y >= restrictedSize.y) {
            return 0;
        }
        return tileData[layer][x][y].gid;
    }
    #endregion
}
