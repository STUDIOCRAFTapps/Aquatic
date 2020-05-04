using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "BaseTile", menuName = "Terrain/Tiles/BaseTile")]
public class BaseTileAsset : ScriptableObject {

    [Header("Presentation")]
    public string fullName;
    public Sprite uiSprite;
    public Sprite previewSprite;

    [Header("Tile Data")]
    [NonSerialized] public int textureBaseIndex;
    public string id;
    [HideInInspector] [NonSerialized] public int globalID = -1;
    [HideInInspector] [NonSerialized] public TileCollection collection;

    [Header("Mesh Data")]
    public TerrainLayers defaultPlacingLayer;
    public bool allowsConnection = true;
    public bool connectsByDefault = true;
    public bool hasTextures = true;
    public Sprite[] textures;
    public Vector3[] verts = new Vector3[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(0, 1), new Vector3(1, 1) };
    public Vector2[] uvs = new Vector2[] { new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f) }; 

    [Header("Collision")]
    public bool hasCollision = true;
    public Bounds2D[] collisionBoxes = new Bounds2D[] { new Bounds2D(Vector2.zero, Vector2.one)};
    public float mass = 2f;

    /// <summary>
    /// Called whenever this tile is placed.
    /// </summary>
    public virtual void OnPlaced (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        ParticleManager.inst.PlayTilePlace(new Vector2Int(x, y), this, mdc);
    }

    /// <summary>
    /// Called whenever this tile is breaked.
    /// </summary>
    public virtual void OnBreaked (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        ParticleManager.inst.PlayTileBreak(new Vector2Int(x, y), this, mdc);
    }

    /// <summary>
    /// Called whenever this tile or tiles near it change.
    /// </summary>
    public virtual void OnTileRefreshed (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(mdc == null) {
            TerrainManager.inst.QueueChunkReloadAtTile(position.x, position.y, layer);
        } else {
            TerrainManager.inst.QueueMobileChunkReload(mdc.mobileChunk.uid);
        }
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding to this tile.
    /// </summary>
    public virtual int GetTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return textureBaseIndex;
    }

    /// <summary>
    /// Returns an uv used for tiles animation by the shader. Sets the X component to the number of frames and Y to the speed of the frames
    /// </summary>
    public virtual Vector2 GetAnimationUV (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return new Vector2(1, 0f);
    }

    /// <summary>
    /// Returns the index in the global texture array corresponding the overlay on a tile. Return -1 if the tile shouldn't have an overlay.
    /// </summary>
    public virtual int GetOverlayTextureIndex (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return -1;
    }

    /// <summary>
    /// An animation UV for overlays
    /// </summary>
    public virtual Vector2 GetOverlayAnimationUV (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        return new Vector2(1, 0f);
    }

    /// <summary>
    /// Returns the ZOffset that should be added to overlays when building the verts for the mesh
    /// </summary>
    public virtual float GetOverlayZOffset () {
        return 0f;
    }

    /// <summary>
    /// Returns 0 or 1 whether the location is considered as a tile this current tile should attach with (1) or not (0).
    /// </summary>
    public virtual byte DoConnectTo (Vector2Int position, TerrainLayers layer, MobileDataChunk mdc = null) {
        TerrainManager.inst.GetGlobalIDAt(position.x, position.y, layer, out int targetID, mdc);
        if(targetID == 0) {
            return 0;
        }
        if(targetID == globalID) {
            return 1;
        }
        if(TerrainManager.inst.tiles.GetTileAssetFromGlobalID(targetID).allowsConnection) {
            if(TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).connectsByDefault) {
                return 1;
            }
        }
        return 0;
    }

    public virtual bool DoSpawnPropOnPlayMode () {
        return false;
    }
}
