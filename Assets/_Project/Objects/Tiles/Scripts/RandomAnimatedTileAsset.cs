using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "RandomAnimatedTileAsset", menuName = "Terrain/Tiles/RandomAnimatedTileAsset")]
public class RandomAnimatedTileAsset : BaseTileAsset {

    [Header("Custom")]
    public int frameCount = 1;
    public float frameSpeed = 1;
    public int variationCount = 3;
    public bool mustStandOnGround = true;

    /// <summary>
    /// Called whenever this tile is placed.
    /// </summary>
    public override void OnPlaced (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(!IsPresenceValid(x, y, layer, mdc)) {
            TerrainManager.inst.SetGlobalIDAt(x, y, layer, 0, mdc);
            return;
        } else {
            base.OnPlaced(x, y, layer, mdc);
        }
    }

    public virtual bool IsPresenceValid (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(!mustStandOnGround) {
            return true;
        }

        TerrainManager.inst.GetGlobalIDAt(x, y - 1, layer, out int underTile, mdc);

        return (underTile != 0) && underTile != globalID;
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
        UnityEngine.Random.InitState(TerrainManager.Hash2D(position.x, position.y));
        mask = (ushort)(UnityEngine.Random.Range(0, variationCount) * frameCount);

        TerrainManager.inst.SetBitmaskAt(position.x, position.y, layer, mask, mdc);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile.
    /// </summary>
    public override int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetBitmaskAt(x, y, layer, out ushort bitmask, mdc);
        return textureBaseIndex + bitmask;
    }

    /// <summary>
    /// Returns an uv used for tiles animation by the shader. Sets the X component to the number of frames and Y to the speed of the frames
    /// </summary>
    public override Vector2 GetAnimationUV (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return new Vector2(frameCount, frameSpeed);
    }
}
