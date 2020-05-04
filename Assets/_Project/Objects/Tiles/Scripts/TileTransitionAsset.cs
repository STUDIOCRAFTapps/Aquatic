using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "TileTransition", menuName = "Terrain/Tiles/TileTransition")]
public class TileTransitionAsset : BaseTileAsset {

    public BaseTileAsset transitioningTile;
    
    //A dictionary to match bitmask code with the correct texture
    public static Dictionary<ushort, int> maskIndex = new Dictionary<ushort, int>() {
        {0, 1}, {1, 11}, {2, 3}, {3, 15}, {4, 2}, {5, 14}, {6, 8}, {7, 12},
        {8, 10}, {9, 9}, {10, 7}, {11, 4}, {12, 6}, {13, 5}, {14, 13}, {15, 0},
    };

    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        byte top = DoConnectTo(position + Vector2Int.up, transitioningTile.defaultPlacingLayer, mdc);
        byte left = DoConnectTo(position + Vector2Int.left, transitioningTile.defaultPlacingLayer, mdc);
        byte right = DoConnectTo(position + Vector2Int.right, transitioningTile.defaultPlacingLayer, mdc);
        byte bottom = DoConnectTo(position + Vector2Int.down, transitioningTile.defaultPlacingLayer, mdc);
        byte mask = (byte)(
            (1 * top) + (2 * left) + (4 * right) + (8 * bottom)
        );
        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile (takes into account the bitmask)
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        return textureBaseIndex + maskIndex[bitmask];
    }

    /// <summary>
    /// Returns 0 or 1 whether the location is considered as a tile this current tile should attach with (1) or not (0).
    /// </summary>
    public override byte DoConnectTo (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetGlobalIDAt(position.x, position.y, layer, out int targetID, mdc);
        if(targetID == transitioningTile.globalID) {
            return 1;
        }
        return 0;
    }
}
