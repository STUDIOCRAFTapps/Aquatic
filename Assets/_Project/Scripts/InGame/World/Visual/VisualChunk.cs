using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualChunk : MonoBehaviour {
    [HideInInspector] public VisualChunkLayer[] layers;
    [HideInInspector] public List<GameObject> propList;

    public Vector2Int position { get; private set; }
    MeshData meshData = new MeshData();

    DataChunk dataChunk;
    MobileDataChunk mdc;

    public void Initiate (Vector2Int chunkPosition, bool changePosition) {
        if(propList == null) {
            propList = new List<GameObject>();
        } else {
            foreach(GameObject prop in propList) {
                Destroy(prop);
            }
            propList.Clear();
        }

        // Create layer object once
        int chunkSize = TerrainManager.inst.chunkSize;
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
            transform.localScale = new Vector3(1, 1, 1f);

            meshData.Initiate();
        }

        if(changePosition) {
            transform.position = new Vector3(
            chunkPosition.x * TerrainManager.inst.chunkSize,
            chunkPosition.y * TerrainManager.inst.chunkSize);
        }

        position = chunkPosition;
    }

    public void BuildMeshes (DataChunk dataChunk, MobileDataChunk mobileDataChunk = null) {
        int chunkSize = TerrainManager.inst.chunkSize;
        this.dataChunk = dataChunk;
        mdc = mobileDataChunk;

        ReadOnlyNodeChunk nc = PathgridManager.inst.GetNodeChunkAt(position);
        for(int l = 0; l < layers.Length; l++) {
            meshData.Clear();
            for(int x = 0; x < chunkSize; x++) {
                for(int y = 0; y < chunkSize; y++) {
                    int worldX = x + (position.x * TerrainManager.inst.chunkSize);
                    int worldY = y + (position.y * TerrainManager.inst.chunkSize);

                    int gid = dataChunk.GetGlobalID(x, y, (TerrainLayers)l);

                    BaseTileAsset tileAsset = (gid != 0)? GeneralAsset.inst.GetTileAssetFromGlobalID(gid):null;
                    if((TerrainLayers)l == TerrainLayers.Ground && nc != null) {
                        Vector2Int tilePos = new Vector2Int(worldX, worldY);
                        bool isSolid = gid != 0 && tileAsset.hasCollision;
                        nc.nodeGrid[x][y].SetData(!isSolid, tilePos, tilePos.x, tilePos.y, 0);
                    }
                    
                    if(gid == 0) {
                        continue;
                    }
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
        int ti = tileAsset.GetTextureIndex(
            x + (position.x * TerrainManager.inst.chunkSize),
            y + (position.y * TerrainManager.inst.chunkSize),
            layer, mdc
        );
        if(ti < 0) {
            if(tileAsset.DoSpawnPropOnPlayMode()) {
                ((PropTileAsset)tileAsset).TrySpawnProp(x, y, this);
            }

            return;
        }

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

    public void AddPropToChunk (Vector3 localPosition, GameObject prefab) {
        GameObject newProp = Instantiate(prefab, transform);
        newProp.transform.localPosition = localPosition;
        propList.Add(newProp);
    }
}

[System.Serializable]
public class VisualChunkLayer {
    public TerrainLayerParameters parameters;

    public GameObject layerObject;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
}
