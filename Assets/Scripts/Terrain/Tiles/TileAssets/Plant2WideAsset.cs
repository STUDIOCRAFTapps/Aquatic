using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Plant2WideAsset", menuName = "Terrain/Tiles/Plant2WideAsset")]
public class Plant2WideAsset : BaseTileAsset {

    [Header("Custom")]
    public int repeatingPatternCount = 3;
    

    /// <summary>
    /// Called whenever this tile is placed.
    /// </summary>
    public override void OnPlaced (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(!IsPresenceValid(x, y, layer, mdc)) {
            TerrainManager.inst.SetGlobalIDAt(x, y, layer, 0, mdc);
            return;
        }
    }

    public virtual bool IsPresenceValid (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetGlobalIDAt(x, y - 1, layer, out int underTile, mdc);
        TerrainManager.inst.GetGlobalIDAt(x - 1, y, layer, out int leftTile, mdc);
        TerrainManager.inst.GetGlobalIDAt(x + 1, y, layer, out int rightTile, mdc);

        return (underTile != 0) && (leftTile != globalID) && (rightTile != globalID);
    }

    /// <summary>
    /// Called whenever this tile or tiles near it change.
    /// </summary>
    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        if(!IsPresenceValid(position.x, position.y, layer, mdc)) {
            TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, 0, mdc);
            return;
        }

        ushort mask = 0;
        
        bool connectTop = TerrainManager.inst.GetGlobalIDAt(position.x, position.y + 1, layer, mdc) == globalID;
        bool connectBottom = TerrainManager.inst.GetGlobalIDAt(position.x, position.y - 1, layer, mdc) == globalID;

        if(!connectTop && !connectBottom) {
            mask = (ushort)(repeatingPatternCount + 2);
        } else
        if(!connectTop && connectBottom) {
            mask = 0;
        } else
        if(connectTop && !connectBottom) {
            mask = (ushort)(repeatingPatternCount + 1);
        } else {
            UnityEngine.Random.InitState(TerrainManager.Hash2D(position.x, position.y));
            mask = (ushort)(1 + UnityEngine.Random.Range(0, repeatingPatternCount));
        }

        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile.
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        return textureBaseIndex + bitmask;
    }
}
