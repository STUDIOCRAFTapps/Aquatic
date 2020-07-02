using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "HorizontalLog", menuName = "Terrain/Tiles/HorizontalLogAsset")]
public class HorizontalLogAsset : BaseTileAsset {
    
    //A dictionary to match bitmask code with the correct texture
    /*public static Dictionary<ushort, int> maskIndex = new Dictionary<ushort, int>() {
        {0,15},{2,20},{8,32},{10,26},{11,23},{16,30},{18,24},{22,21},{24,31},{26,25},{27,43},{30,42},{31,22},
        {64,0},{66,10},{72,6},{74,16},{75,34},{80,4},{82,14},{86,35},{88,5},{90,18},{91,37},{94,38},{95,8},
        {104,3},{106,44},{107,13},{120,33},{122,39},{123,17},{126,40},{127,7},{208,1},{210,41},{214,11},{216,36},
        {218,46},{219,39},{222,19},{223,9},{248,2},{250,28},{251,27},{254,29},{255,12}
    };*/

    public static Vector2Int ul = new Vector2Int(-1, 1);
    public static Vector2Int ur = new Vector2Int(1, 1);
    public static Vector2Int dl = new Vector2Int(-1, -1);
    public static Vector2Int dr = new Vector2Int(1, -1);

    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        bool top = DoConnectTo(position + Vector2Int.up, layer, mdc) == 1;
        bool left = DoConnectTo(position + Vector2Int.left, layer, mdc) == 1;
        bool right = DoConnectTo(position + Vector2Int.right, layer, mdc) == 1;
        bool bottom = DoConnectTo(position + Vector2Int.down, layer, mdc) == 1;

        bool topLeft = DoConnectTo(position + ul, layer, mdc) == 1;
        bool topRight = DoConnectTo(position + ur, layer, mdc) == 1;
        bool bottomLeft = DoConnectTo(position + dl, layer, mdc) == 1;
        bool bottomRight = DoConnectTo(position + dr, layer, mdc) == 1;

        ushort mask = 0;

        // Paterns;
        // If one or both of two bottom/top corner is TRUE, then they are both true, meaning it can be compressed to a single byte

        if(left || right) { // Horizontal
            if(left && right) {
                if((top && !(topLeft || topRight)) || (bottom && !(bottomLeft || bottomRight))) {
                    mask = 16;
                } else {
                    mask = 2;
                }
            } else if(left) {
                if(top && bottom) {
                    mask = 13;
                } else if(top) {
                    mask = 18;
                } else if(bottom) {
                    mask = 8;
                } else {
                    mask = 3;
                }
            } else {
                if(top && bottom) {
                    mask = 12;
                } else if(top) {
                    mask = 17;
                } else if(bottom) {
                    mask = 7;
                } else {
                    mask = 1;
                }
            }
        } else { // Vertical
            if(top && bottom) {
                if((topLeft || topRight) && (bottomLeft || bottomRight)) {
                    mask = 10;
                } else if(topLeft || topRight) {
                    mask = 11;
                } else if(bottomLeft || bottomRight) {
                    mask = 6;
                } else {
                    mask = 9;
                }
            } else if(bottom) {
                if(bottomLeft || bottomRight) {
                    mask = 5;
                } else {
                    mask = 4;
                }
            } else if(top) {
                if(topLeft || topRight) {
                    mask = 15;
                } else {
                    mask = 14;
                }
            } else {
                mask = 0;
            }
        }


        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile (takes into account the bitmask)
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        return textureBaseIndex + bitmask;//maskIndex[bitmask];
    }
}
