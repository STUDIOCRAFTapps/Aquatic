using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualChunk : MonoBehaviour {
    [HideInInspector] public VisualChunkLayer[] layers;

    Vector2Int position;
    MeshData meshData = new MeshData();

    DataChunk dataChunk;
    MobileDataChunk mdc;

    public void Initiate (Vector2Int chunkPosition, bool changePosition) {
        // Create layer object once
        if(layers.Length == 0) {
            layers = new VisualChunkLayer[TerrainManager.inst.layerParameters.Length];

            for(int i = 0; i < layers.Length; i++) {
                layers[i] = new VisualChunkLayer();

                GameObject layerObject = Instantiate(VisualChunkManager.inst.chunkLayerPrefab, transform);
                layers[i].layerObject = layerObject;
                layers[i].meshRenderer = layerObject.GetComponent<MeshRenderer>();
                layers[i].meshFilter = layerObject.GetComponent<MeshFilter>();
                layers[i].parameters = TerrainManager.inst.layerParameters[i];

                layers[i].meshRenderer.sortingLayerName = layers[i].parameters.sortingLayerName;
                layers[i].meshRenderer.sortingOrder = layers[i].parameters.sortingOrder;
                layers[i].meshRenderer.material = VisualChunkManager.inst.globalMaterial;

                layers[i].meshFilter.mesh = new Mesh();
            }
            transform.localScale = new Vector3(TerrainManager.inst.tileScale, TerrainManager.inst.tileScale, 1f);

            meshData.Initiate();
        }

        if(changePosition) {
            transform.position = new Vector3(
            chunkPosition.x * TerrainManager.inst.chunkSize * TerrainManager.inst.tileScale,
            chunkPosition.y * TerrainManager.inst.chunkSize * TerrainManager.inst.tileScale);
        }

        position = chunkPosition;
    }

    public void BuildMeshes (DataChunk dataChunk, MobileDataChunk mobileDataChunk = null) {
        int chunkSize = TerrainManager.inst.chunkSize;
        this.dataChunk = dataChunk;
        mdc = mobileDataChunk;

        for(int l = 0; l < layers.Length; l++) {
            meshData.Clear();
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    if(dataChunk.GetGlobalID(x, y, (TerrainLayers)l) == 0) {
                        continue;
                    }
                    BaseTileAsset tileAsset = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(dataChunk.GetGlobalID(x, y, (TerrainLayers)l));
                    if(tileAsset.hasTextures) {
                        AddTileToMeshData(x, y, (TerrainLayers)l, tileAsset);
                        AddOverlayTileToMeshData(x, y, (TerrainLayers)l, tileAsset);
                    }
                }
            }
            meshData.Apply(layers[l].meshFilter.mesh);
        }
    }

    public void AddTileToMeshData (int x, int y, TerrainLayers layer, BaseTileAsset tileAsset) {
        Vector3 vertPos;
        Vector3 vf = new Vector3(0, 0, 0);

        for(int i = 0; i < tileAsset.verts.Length; i++) {
            vertPos = tileAsset.verts[i];
            meshData.verts.Add(new Vector3(
                x + vertPos.x,
                y + vertPos.y,
                vertPos.z
            ));
        }

        meshData.tris.Add(meshData.faceOffset + 2);
        meshData.tris.Add(meshData.faceOffset + 1);
        meshData.tris.Add(meshData.faceOffset + 0);
        meshData.tris.Add(meshData.faceOffset + 3);
        meshData.tris.Add(meshData.faceOffset + 1);
        meshData.tris.Add(meshData.faceOffset + 2);
        meshData.faceOffset += tileAsset.verts.Length;

        int ti = tileAsset.GetTextureIndex(
            x + (position.x * TerrainManager.inst.chunkSize),
            y + (position.y * TerrainManager.inst.chunkSize),
            layer, mdc
        );
        Vector2 animUV = tileAsset.GetAnimationUV(x, y, layer, mdc);
        for(int w = 0; w < tileAsset.uvs.Length; w++) {
            meshData.uvs.Add(new Vector3(tileAsset.uvs[w].x, tileAsset.uvs[w].y, ti));
            meshData.animUVs.Add(animUV);
        }
    }

    public void AddOverlayTileToMeshData (int x, int y, TerrainLayers layer, BaseTileAsset tileAsset) {
        int ti = tileAsset.GetOverlayTextureIndex(
            x + (position.x * TerrainManager.inst.chunkSize),
            y + (position.y * TerrainManager.inst.chunkSize),
            layer, mdc
        );

        if(ti == -1) {
            return;
        }

        Vector3 vertPos;
        Vector3 vf = new Vector3(0, 0, 0);

        for(int i = 0; i < tileAsset.verts.Length; i++) {
            vertPos = tileAsset.verts[i];
            meshData.verts.Add(new Vector3(
                x + vertPos.x,
                y + vertPos.y,
                vertPos.z + tileAsset.GetOverlayZOffset()
            ));
        }

        meshData.tris.Add(meshData.faceOffset + 2);
        meshData.tris.Add(meshData.faceOffset + 1);
        meshData.tris.Add(meshData.faceOffset + 0);
        meshData.tris.Add(meshData.faceOffset + 3);
        meshData.tris.Add(meshData.faceOffset + 1);
        meshData.tris.Add(meshData.faceOffset + 2);
        meshData.faceOffset += tileAsset.verts.Length;
        
        Vector2 animUV = tileAsset.GetOverlayAnimationUV(x, y, layer, mdc);
        for(int w = 0; w < tileAsset.uvs.Length; w++) {
            meshData.uvs.Add(new Vector3(tileAsset.uvs[w].x, tileAsset.uvs[w].y, ti));
            meshData.animUVs.Add(animUV);
        }
    }
}

[System.Serializable]
public class VisualChunkLayer {
    public TerrainLayerParameters parameters;

    public GameObject layerObject;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
}
