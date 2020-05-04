using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "CollectableTileAsset", menuName = "Terrain/Tiles/CollectableTileAsset")]
public class CollectableTileAsset : BaseTileAsset {

    [Header("Custom")]
    public float frameSpeed = 1;

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
        return textureBaseIndex;
    }

    /// <summary>
    /// Returns an uv used for tiles animation by the shader. Sets the X component to the number of frames and Y to the speed of the frames
    /// </summary>
    public override Vector2 GetAnimationUV (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return new Vector2(textures.Length, frameSpeed);
    }


    public virtual void OnCollect (int x, int y) {
        TerrainManager.inst.SetGlobalIDAt(x, y, TerrainLayers.Ground, 0);
        ParticleManager.inst.PlayTileBreak(new Vector2Int(x, y), this);
    }
}
