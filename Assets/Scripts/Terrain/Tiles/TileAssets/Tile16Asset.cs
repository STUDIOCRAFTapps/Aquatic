using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Tile16", menuName = "Terrain/Tiles/Tile16")]
public class Tile16Asset : BaseTileAsset {
    
    //A dictionary to match bitmask code with the correct texture
    public static Dictionary<ushort, int> maskIndex = new Dictionary<ushort, int>() {
        {0, 1}, {1, 11}, {2, 3}, {3, 15}, {4, 2}, {5, 14}, {6, 8}, {7, 12},
        {8, 10}, {9, 9}, {10, 7}, {11, 4}, {12, 6}, {13, 5}, {14, 13}, {15, 0},
    };

    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        byte top = DoConnectTo(position + Vector2Int.up, layer, mdc);
        byte left = DoConnectTo(position + Vector2Int.left, layer, mdc);
        byte right = DoConnectTo(position + Vector2Int.right, layer, mdc);
        byte bottom = DoConnectTo(position + Vector2Int.down, layer, mdc);
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
}
