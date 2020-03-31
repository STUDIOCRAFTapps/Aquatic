using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour {

    #region Header and Init
    public static TerrainManager inst;
    public Dictionary<Vector2Int, DataChunk> chunks;
    public Queue<DataChunk> unusedChunks;
    public Queue<Vector2Int> chunkToReload;
    public Queue<int> mobileChunkToReload;

    [Header("Reference")]
    public TileCollectionGroup tiles;
    public Transform mobileRoot;
    public Transform terrainRoot;

    [Header("Parameters")]
    public TerrainLayerParameters[] layerParameters;
    public float pixelPerTile = 16f;
    public int chunkSize = 16;
    public float tileScale = 1f;
    public Vector2Int loadRadius;
    public float unloadTimer = 5f;
    public float autoSaveTimeLimit = 10f;
    public float regionAutoSaveTimeLimit = 10f;
    public int chunksPerRegionSide = 4;
    public float outOfBoundsRefreshInterval = 0.2f;

    [HideInInspector] public int currentMobileIndex = 0;

    [HideInInspector] public float worldToPixel;
    [HideInInspector] public float pixelToWorld;

    private void Awake () {
        if(inst == null) {
            inst = this;
        }

        currentMobileIndex = PlayerPrefs.GetInt("currentMobileIndex", 0);

        chunks = new Dictionary<Vector2Int, DataChunk>();
        unusedChunks = new Queue<DataChunk>();
        chunkToReload = new Queue<Vector2Int>();
        mobileChunkToReload = new Queue<int>();
        tiles.BuildDictionaries();

        worldToPixel = (1f / tileScale) * pixelPerTile;
        pixelToWorld = 1f / worldToPixel;
    }

    private void LateUpdate () {
        while(chunkToReload?.Count > 0) {
            VisualChunkManager.inst?.LoadChunkAt(chunkToReload.Dequeue());
        }
        while(mobileChunkToReload?.Count > 0) {
            if(VisualChunkManager.inst != null) {
                int key = mobileChunkToReload.Dequeue();
                if(VisualChunkManager.inst.mobileChunkPool.ContainsKey(key)) {
                    MobileChunk mc = VisualChunkManager.inst.mobileChunkPool[key];
                    VisualChunkManager.inst.BuildMobileChunk(mc);
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.L)) {
            CompleteSave();
        }
    }

    private void FixedUpdate () {
        AutoSaves();
    }

    void AutoSaves () {
        foreach(KeyValuePair<Vector2Int, DataChunk> kvp in chunks) {
            if(Time.time - kvp.Value.timeOfLastAutosave > autoSaveTimeLimit) {
                kvp.Value.timeOfLastAutosave = Time.time;
                DataChunkSaving.inst.SaveChunk(kvp.Value);
            }
        }
        foreach(KeyValuePair<int, MobileChunk> kvp in VisualChunkManager.inst.mobileChunkPool) {
            if(Time.time - kvp.Value.timeOfLastAutosave > autoSaveTimeLimit) {
                kvp.Value.timeOfLastAutosave = Time.time;
                DataChunkSaving.inst.SaveChunk(kvp.Value.mobileDataChunk);
            }
        }
        EntityRegionManager.inst.CheckForAutosaves();
    }

    void CompleteSave () {
        foreach(KeyValuePair<Vector2Int, DataChunk> kvp in chunks) {
            DataChunkSaving.inst.SaveChunk(kvp.Value);
        }
        foreach(KeyValuePair<int, MobileChunk> kvp in VisualChunkManager.inst.mobileChunkPool) {
            DataChunkSaving.inst.SaveChunk(kvp.Value.mobileDataChunk);
        }
        EntityRegionManager.inst.SaveAllRegions();
    }

    private void OnApplicationQuit () {
        CompleteSave();
    }
    #endregion

    #region Default Chunks
    public bool GetChunkAtPosition (Vector2Int position, out DataChunk dataChunk) {
        if(chunks.ContainsKey(position)) {
            dataChunk = chunks[position];
            return true;
        } else {
            dataChunk = null;
            return false;
        }
    }

    public DataChunk GetNewDataChunk (Vector2Int chunkPosition) {
        DataChunk dataChunk;
        if(unusedChunks.Count <= 0) {
            dataChunk = new DataChunk(chunkSize);
        } else {
            dataChunk = unusedChunks.Dequeue();
        }
        dataChunk.Init(chunkPosition);
        chunks.Add(chunkPosition, dataChunk);

        return dataChunk;
    }

    public void SetDataChunkAsUnused (Vector2Int chunkPosition) {
        if(chunks.ContainsKey(chunkPosition)) {
            DataChunk dataChunk = chunks[chunkPosition];
            chunks.Remove(chunkPosition);
            unusedChunks.Enqueue(dataChunk);
        }
    }

    public void RefreshSurroundingChunks (Vector2Int chunkPosition, int radius = 2) {
        for(int x = -(radius - 1); x < radius; x++) {
            for(int y = -(radius - 1); y < radius; y++) {
                if(chunks.ContainsKey(chunkPosition + new Vector2Int(x, y))) {
                    chunks[chunkPosition + new Vector2Int(x, y)].RefreshTiles();
                }
            }
        }
    }

    public void QueueChunkReloadAtTile (int x, int y, TerrainLayers layer) {
        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            if(!chunkToReload.Contains(cpos)) {
                chunkToReload.Enqueue(cpos);
            }
        }
    }
    #endregion

    #region Mobile Chunks
    public MobileChunk GetMobileChunkAtPosition (Vector2 position) {
        foreach(KeyValuePair<int, MobileChunk> kvp in VisualChunkManager.inst.mobileChunkPool) {
            if(!kvp.Value.gameObject.activeInHierarchy) {
                continue;
            }

            float worldSizeX = kvp.Value.mobileDataChunk.restrictedSize.x * tileScale;
            float worldSizeY = kvp.Value.mobileDataChunk.restrictedSize.y * tileScale;

            bool isInRange =
                position.x > kvp.Value.position.x &&
                position.y > kvp.Value.position.y &&
                position.x < kvp.Value.position.x + worldSizeX &&
                position.y < kvp.Value.position.y + worldSizeY;

            if(isInRange) {
                return kvp.Value;
            }
        }
        return null;
    }

    public MobileChunk CreateNewMobileChunk (Vector2Int restrictedSize, Vector3 position) {
        MobileChunk mobileChunk = VisualChunkManager.inst.GetNewMobileChunk(currentMobileIndex);
        currentMobileIndex++;
        PlayerPrefs.SetInt("currentMobileIndex", currentMobileIndex);
        Debug.Log("Shit playerpref system need to be replace by a world based reliable system to distribute uids");

        mobileChunk.SetRestrictedSize(restrictedSize);
        VisualChunkManager.inst.BuildMobileChunk(mobileChunk, position);
        EntityRegionManager.inst.AddMobileChunk(mobileChunk);

        return mobileChunk;
    }

    public void LoadMobileChunkFromUID (int uid) {
        MobileChunk mobileChunk = VisualChunkManager.inst.GetNewMobileChunk(uid);
        if(DataChunkSaving.inst.LoadChunk(mobileChunk.mobileDataChunk)) {
            EntityRegionManager.inst.AddMobileChunk(mobileChunk);
            VisualChunkManager.inst.BuildMobileChunk(mobileChunk);
            mobileChunk.mobileDataChunk.RefreshTiles();
            mobileChunk.RefreshSelectionRect();
        } else {
            VisualChunkManager.inst.UnloadMobileChunk(mobileChunk, true, false);
            return;
        }

        mobileChunk.gameObject.SetActive(false);
        EntityRegionManager.inst.outOfBoundsMobileChunks.Add(mobileChunk);
    }

    public void QueueMobileChunkReload (int uid) {
        if(!mobileChunkToReload.Contains(uid)) {
            mobileChunkToReload.Enqueue(uid);
        }
    }
    #endregion

    #region Tiles
    public bool GetGlobalIDAt (int x, int y, TerrainLayers layer, out int globalID, MobileDataChunk mdc = null) {
        if(mdc != null) {
            globalID = mdc.GetGlobalID(x, y, layer);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            globalID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            return true;
        }
        globalID = 0;
        return false;
    }

    public int GetGlobalIDAt (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(mdc != null) {
            return mdc.GetGlobalID(x, y, layer);
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            return dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
        }
        return 0;
    }

    public bool SetGlobalIDAt (int x, int y, TerrainLayers layer, int globalID, MobileDataChunk mdc = null) {
        if(mdc != null) {
            int oldGID = mdc.GetGlobalID(x, y, layer);
            if(oldGID != 0) {
                tiles.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer, mdc);
            }

            mdc.SetGlobalID(x, y, layer, globalID);
            if(globalID != 0) {
                tiles.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer, mdc);
            }
            RefreshTilesAround(x, y, layer, 3, mdc);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            int oldGID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            if(oldGID != 0) {
                tiles.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer, mdc);
            }

            dataChunk.SetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer, globalID);
            if(globalID != 0) {
                tiles.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer, mdc);
            }
            RefreshTilesAround(x, y, layer);
            return true;
        }
        return false;
    }

    public bool GetBitmaskAt (int x, int y, TerrainLayers layer, out ushort bitmask, MobileDataChunk mdc = null) {
        if(mdc != null) {
            bitmask = mdc.GetBitmask(x, y, layer);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            bitmask = dataChunk.GetBitmask(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            return true;
        }
        bitmask = 0;
        return false;
    }

    public bool SetBitmaskAt (int x, int y, TerrainLayers layer, ushort bitmask, MobileDataChunk mdc = null) {
        if(mdc != null) {
            mdc.SetBitmask(x, y, layer, bitmask);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            dataChunk.SetBitmask(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer, bitmask);
            return true;
        }
        return false;
    }

    public void RefreshTilesAround (int x, int y, TerrainLayers layer, int radius = 3, MobileDataChunk mdc = null) {
        for(int xx = -(radius - 1); xx < radius; xx++) {
            for(int yy = -(radius - 1); yy < radius; yy++) {
                RefreshTileAt(x + xx, y + yy, layer, mdc);
            }
        }
    }

    public bool RefreshTileAt (int x, int y, TerrainLayers layer, MobileDataChunk mdc = null) {
        if(mdc != null) {
            int gid = mdc.GetGlobalID(x, y, layer);
            if(gid != 0) {
                tiles.GetTileAssetFromGlobalID(gid).OnTileRefreshed(new Vector2Int(x, y), layer, mdc);
            }
            QueueMobileChunkReload(mdc.mobileChunk.uid);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            int gid = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            if(gid != 0) {
                tiles.GetTileAssetFromGlobalID(gid).OnTileRefreshed(new Vector2Int(x, y), layer);
            }
            QueueChunkReloadAtTile(x, y, layer);
            return true;
        }
        return false;
    }
    #endregion

    #region Utils
    public Vector2Int GetChunkPositionAtTile (Vector2Int position) {
        return Vector2Int.FloorToInt(new Vector2(position.x / (float)chunkSize, position.y / (float)chunkSize));
    }

    public Vector2Int GetRegionPositionAtTile (Vector2Int position) {
        return Vector2Int.FloorToInt(new Vector2(position.x / ((float)chunkSize * chunksPerRegionSide), position.y / ((float)chunkSize * chunksPerRegionSide)));
    }

    public Vector2Int GetChunkPositionAtTile (int x, int y) {
        return Vector2Int.FloorToInt(new Vector2(x / (float)chunkSize, y / (float)chunkSize));
    }

    public Vector2Int WorldToTile (Vector2 worldPos) {
        return Vector2Int.FloorToInt(worldPos / tileScale);
    }

    public Vector2 TileToWorld (Vector2Int tilePos) {
        return (Vector2)tilePos * tileScale;
    }

    public Vector2Int WorldToChunk (Vector2 worldPos) {
        return GetChunkPositionAtTile(Vector2Int.FloorToInt(worldPos / tileScale));
    }

    public Vector2Int WorldToRegion (Vector2 worldPos) {
        return GetRegionPositionAtTile(Vector2Int.FloorToInt(worldPos / tileScale));
    }

    public Vector2Int GetLocalPositionAtTile (int x, int y, Vector2Int cpos) {
        return new Vector2Int(x - cpos.x * chunkSize, y - cpos.y * chunkSize);
    }

    public bool IsMobileChunkInLoadedChunks (MobileChunk mobileChunk) {
        Vector2Int min = WorldToChunk(mobileChunk.position);
        Vector2Int max = WorldToChunk(mobileChunk.position + (Vector3)mobileChunk.boxCollider.size);

        for(int x = min.x; x <= max.x; x++) {
            for(int y = min.y; y <= max.y; y++) {
                if(!VisualChunkManager.inst.visualChunkPool.ContainsKey(new Vector2Int(x, y))) {
                    return false;
                }
            }
        }
        return true;
    }

    public static int Hash2D (int x, int y) {
        unchecked {
            int hash = x.GetHashCode() * 486187739;
            hash = Combine(hash * 486187739, y.GetHashCode());
            return hash;
        }
    }

    static int Combine (int h1, int h2) {
        unchecked {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
    #endregion
}
