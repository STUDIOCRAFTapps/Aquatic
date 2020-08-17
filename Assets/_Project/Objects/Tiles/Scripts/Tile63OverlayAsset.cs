using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Tile63Overlay", menuName = "Terrain/Tiles/Tile63Overlay")]
public class Tile63OverlayAsset : BaseTileAsset {

    [Header("Custom")]
    public int overlayCount = 2;
    public float overlayChances = 0.2f;
    public float overlayZOffset = -0.001f;
    public bool generateOverlayOnPost47 = false;
    
    //A dictionary to match bitmask code with the correct texture
    public static Dictionary<ushort, int> maskIndex = new Dictionary<ushort, int>() {
        {0,15},{2,20},{8,32},{10,26},{11,23},{16,30},{18,24},{22,21},{24,31},{26,25},{27,43},{30,42},{31,22},
        {64,0},{66,10},{72,6},{74,16},{75,34},{80,4},{82,14},{86,35},{88,5},{90,18},{91,37},{94,38},{95,8},
        {104,3},{106,44},{107,13},{120,33},{122,45},{123,17},{126,40},{127,7},{208,1},{210,41},{214,11},
        {216,36},{218,46},{219,39},{222,19},{223,9},{248,2},{250,28},{251,27},{254,29},{255,12},{256,57},
        {257,49},{258,56},{259,48},{260,58},{261,50},{262,55},{263,47},{264,61},{265,52},{266,60},{267,51},
        {268,62},{269,53},{270,59},{271,54}
    };
    //A dictionary to match bitmask code with the correct texture
    //secondMaskIndex (256++)
    //If should connect (The adjacent block is a first layer tile) = 1
    //Top = +1
    //Left = +2
    //Right = +4
    //Bottom = +8
    //+256
    public static Vector2Int[][] checkPosition = new Vector2Int[][] {
        new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(-1, 2), new Vector2Int(1, 2)}, //Top (index: 0)
        new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(-2, 1)}, //Left (index: 1)
        new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(2, 1)}, //Right (index: 2)
        new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(1, -2)} //Bottom (index: 3)
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
        byte topLeft = 0, topRight = 0, bottomRight = 0, bottomLeft = 0;
        if(top == 1 && left == 1)
            topLeft = (byte)(DoConnectTo(position + ul, layer, mdc) & top & left);
        if(top == 1 && right == 1)
            topRight = (byte)(DoConnectTo(position + ur, layer, mdc) & top & right);
        if(bottom == 1 && right == 1)
            bottomRight = (byte)(DoConnectTo(position + dr, layer, mdc) & bottom & right);
        if(bottom == 1 && left == 1)
            bottomLeft = (byte)(DoConnectTo(position + dl, layer, mdc) & bottom & left);
        ushort mask = (ushort)(
            (1 * topLeft) + (2 * top) + (4 * topRight) + (8 * left) + (16 * right) +
            (32 * bottomLeft) + (64 * bottom) + (128 * bottomRight)
        );

        //47-62
        if(mask == 255) {
            mask = 256;
            for(int i = 0; i < checkPosition.Length; i++) {
                bool isAllWall = true;
                for(int l = 0; l < checkPosition[i].Length; l++) {
                    if(!(DoConnectTo(position + checkPosition[i][l], layer, mdc) == 1)) {
                        isAllWall = false;
                        continue;
                    }
                }
                mask += (ushort)((1 << i) * (isAllWall ? 0 : 1));
            }
        }

        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile (takes into account the bitmask)
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        if(maskIndex.TryGetValue(bitmask, out int value)) {
            return textureBaseIndex + maskIndex[bitmask];
        } else {
            return textureBaseIndex + maskIndex[0];
        }
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding the overlay on a tile. Return -1 if the tile shouldn't have an overlay.
    /// </summary>
    public override int GetOverlayTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {

        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        if(!maskIndex.TryGetValue(bitmask, out int value)) {
            return -1;
        }
        if(!generateOverlayOnPost47 && maskIndex[bitmask] >= 47) {
            return -1;
        }

        UnityEngine.Random.InitState(TerrainManager.Hash2D(y*2, x));
        if(UnityEngine.Random.Range(0f, 1f) > overlayChances) {
            return -1;
        }
        ushort offset = (ushort)(UnityEngine.Random.Range(0, overlayCount));

        return textureBaseIndex + 63 + offset;
    }

    /// <summary>
    /// Returns the ZOffset that should be added to overlays when building the verts for the mesh
    /// </summary>
    public override float GetOverlayZOffset () {
        return overlayZOffset;
    }
}
