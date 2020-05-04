using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "PropTileAsset", menuName = "Terrain/Tiles/PropTileAsset")]
public class PropTileAsset : BaseTileAsset {

    [Header("Prop Parameters")]
    public GameObject prefab;
    public Vector3 offset;

    /// <summary>
    /// Called whenever this tile is placed.
    /// </summary>
    public override void OnPlaced (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnPlaced(x, y, layer, mdc);
    }

    /// <summary>
    /// Called whenever this tile or tiles near it change.
    /// </summary>
    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile.
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(GameManager.inst.engineMode == EngineModes.Edit) {
            return textureBaseIndex;
        } else {
            return -1;
        }
    }

    public virtual void TrySpawnProp (int localx, int localy, VisualChunk chunk) {
        chunk.AddPropToChunk(new Vector3(localx + offset.x, localy + offset.y, offset.z), prefab);
        /*if(chunk.GetType() == typeof(MobileChunk)) {
            chunk.AddPropToChunk(new Vector3(x + offset.x, y, offset.z), prefab);
        } else {
            Vector2Int lpos = TerrainManager.inst.GetLocalPositionAtTile(x, y, chunk.position);
            chunk.AddPropToChunk(new Vector3(lpos.x + offset.x, lpos.y + offset.y, offset.z), prefab);
        }*/
    }

    public override bool DoSpawnPropOnPlayMode () {
        if(GameManager.inst.engineMode == EngineModes.Edit) {
            return false;
        } else {
            return true;
        }
    }
}
