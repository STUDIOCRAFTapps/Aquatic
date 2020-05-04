using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SideFlowingWater", menuName = "Terrain/Tiles/SideFlowingWater")]
public class SideFlowingWater : BaseTileAsset {

    [Header("Custom")]
    public int frameCount = 1;
    public float frameSpeed = 1;
    public bool isFlowingLeft = true;
    public DownFlowingWater replaceBy;
    public SideFlowingWater counterPart;

    public override void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        base.OnTileRefreshed(position, layer, mdc);

        bool top = DoConnectTo(position + Vector2Int.up, layer, mdc) == 1;
        bool left = DoConnectTo(position + Vector2Int.left, layer, mdc) == 1;
        bool right = DoConnectTo(position + Vector2Int.right, layer, mdc) == 1;
        bool bottom = DoConnectTo(position + Vector2Int.down, layer, mdc) == 1;
        ushort mask = 2;

        if(left && !right && !top && !bottom) {
            mask = 0;
        } else if(!left && right && !top && !bottom) {
            mask = 1;
        } else if(left && right && !top && !bottom) {
            left = TerrainManager.inst.GetGlobalIDAt(position.x - 1, position.y, layer, mdc) == counterPart.globalID;
            right = TerrainManager.inst.GetGlobalIDAt(position.x + 1, position.y, layer, mdc) == counterPart.globalID;
            if(left && right) {
                TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, counterPart.globalID, mdc);
                return;
            }
            mask = 2;
        } else if(left && right) {
            TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, replaceBy.globalID, mdc);
            return;
        } else if(top && bottom && left && right) {
            TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, replaceBy.globalID, mdc);
            return;
        } else if(top && bottom && (left || right)) {
            if(left == isFlowingLeft) {
                TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, counterPart.globalID, mdc);
                return;
            }
            mask = 5;
        } else if(!top && bottom && (left || right)) {
            if(left == isFlowingLeft) {
                TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, counterPart.globalID, mdc);
                return;
            }
            mask = 3;
        } else if(top && !bottom && (left || right)) {
            if(left != isFlowingLeft) {
                TerrainManager.inst.SetGlobalIDAt(position.x, position.y, layer, counterPart.globalID, mdc);
                return;
            }
            mask = 4;
        }

        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile (takes into account the bitmask)
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        return textureBaseIndex + bitmask * frameCount;
    }

    /// <summary>
    /// Returns an uv used for tiles animation by the shader. Sets the X component to the number of frames and Y to the speed of the frames
    /// </summary>
    public override Vector2 GetAnimationUV (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return new Vector2(frameCount, frameSpeed);
    }
}
