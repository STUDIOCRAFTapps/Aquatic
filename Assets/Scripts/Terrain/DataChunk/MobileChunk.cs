using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileChunk : MonoBehaviour {
    public VisualChunk visualChunk;
    public BoxCollider2D boxCollider;
    public SpriteRenderer selectionRect;
    new public RigidbodyPixel rigidbody;

    public Vector3 position;
    public Vector3 previousPosition;

    public int uid;
    public MobileDataChunk mobileDataChunk;

    public float timeOfLastAutosave {
        get {
            return mobileDataChunk.timeOfLastAutosave;
        }
        set {
            mobileDataChunk.timeOfLastAutosave = value;
        }
    }

    public void Initiate () {
        visualChunk.Initiate(Vector2Int.zero, false);
        rigidbody.velocity = Vector2.zero;
        if(rigidbody == null) {
            rigidbody = GetComponent<RigidbodyPixel>();
        }
        mobileDataChunk = new MobileDataChunk(TerrainManager.inst.chunkSize);
        mobileDataChunk.mobileChunk = this;
    }

    public void SetRestrictedSize (Vector2Int restrictedSize) {
        boxCollider.offset = (Vector2)restrictedSize * 0.5f;
        boxCollider.size = restrictedSize;
        selectionRect.size = restrictedSize;

        mobileDataChunk.Init(restrictedSize);
    }

    public void RefreshSelectionRect () {
        selectionRect.size = mobileDataChunk.restrictedSize;

    }

    public void BuildMeshes () {
        int chunkSize = TerrainManager.inst.chunkSize;

        float totalMass = 0f;
        for(int x = 0; x < mobileDataChunk.restrictedSize.x; x++) {
            for(int y = 0; y < mobileDataChunk.restrictedSize.y; y++) {
                int gid = mobileDataChunk.GetGlobalID(x, y, TerrainLayers.Ground);

                if(gid != 0) {
                    totalMass += TerrainManager.inst.tiles.GetTileAssetFromGlobalID(gid).mass;
                }
            }
        }
        rigidbody.mass = totalMass;

        visualChunk.BuildMeshes(mobileDataChunk, mobileDataChunk);
    }

    public void UpdatePositionData (Vector3 newPosition) {
        previousPosition = position;
        position = newPosition;

        if(TerrainManager.inst.WorldToChunk(position + Vector3.one * 0.001f) != TerrainManager.inst.WorldToChunk(previousPosition + Vector3.one * 0.001f)) {
            if(!EntityRegionManager.inst.MoveMobileChunk(this, previousPosition)) {

                Debug.Log($"A mobile chunk with the UID {uid} vanished out of bounds. An attempt was saved it in an unloaded region.");
                Debug.Log($"{TerrainManager.inst.WorldToChunk(position)}, prev : {TerrainManager.inst.WorldToChunk(position)}");
                EntityRegionManager.inst.LoadRegionAtChunk(TerrainManager.inst.WorldToChunk(previousPosition));
                EntityRegionManager.inst.MoveMobileChunk(this, previousPosition);
                EntityRegionManager.inst.UnloadRegionAtChunk(TerrainManager.inst.WorldToChunk(previousPosition));
                return;
            }
        }

        if(!TerrainManager.inst.IsMobileChunkInLoadedChunks(this)) {
            gameObject.SetActive(false);
            EntityRegionManager.inst.outOfBoundsMobileChunks.Add(this);

            position = previousPosition;
            transform.position = position;
        }
    }
}
