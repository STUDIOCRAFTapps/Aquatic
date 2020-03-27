using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Tile47", menuName = "Terrain/Tiles/Tile47")]
public class Tile47Asset : BaseTileAsset {
    
    //A dictionary to match bitmask code with the correct texture
    public static Dictionary<ushort, int> maskIndex = new Dictionary<ushort, int>() {
        {0,15},{2,20},{8,32},{10,26},{11,23},{16,30},{18,24},{22,21},{24,31},{26,25},{27,43},{30,42},{31,22},
        {64,0},{66,10},{72,6},{74,16},{75,34},{80,4},{82,14},{86,35},{88,5},{90,18},{91,37},{94,38},{95,8},
        {104,3},{106,44},{107,13},{120,33},{122,39},{123,17},{126,40},{127,7},{208,1},{210,41},{214,11},{216,36},
        {218,46},{219,39},{222,19},{223,9},{248,2},{250,28},{251,27},{254,29},{255,12}
    };

    public static Vector2Int ul = new Vector2Int(-1, 1);
    public static Vector2Int ur = new Vector2Int(1, 1);
    public static Vector2Int dl = new Vector2Int(-1, -1);
    public static Vector2Int dr = new Vector2Int(1, -1);

    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        byte top = DoConnectTo(position + Vector2Int.up, layer, mdc);
        byte left = DoConnectTo(position + Vector2Int.left, layer, mdc);
        byte right = DoConnectTo(position + Vector2Int.right, layer, mdc);
        byte bottom = DoConnectTo(position + Vector2Int.down, layer, mdc);
        byte topLeft = (byte)(DoConnectTo(position + ul, layer, mdc) & top & left);
        byte topRight = (byte)(DoConnectTo(position + ur, layer, mdc) & top & right);
        byte bottomRight = (byte)(DoConnectTo(position + dr, layer, mdc) & bottom & right);
        byte bottomLeft = (byte)(DoConnectTo(position + dl, layer, mdc) & bottom & left);
        byte mask = (byte)(
            (1 * topLeft) + (2 * top) + (4 * topRight) + (8 * left) + (16 * right) +
            (32 * bottomLeft) + (64 * bottom) + (128 * bottomRight)
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
