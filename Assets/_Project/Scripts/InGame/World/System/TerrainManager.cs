using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Transports;
using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;

public class TerrainManager : NetworkedBehaviour {

    #region Header and Init
    public static TerrainManager inst;
    public Dictionary<long, DataChunk> chunks;
    public Queue<DataChunk> unusedChunks;
    public Queue<int> mobileChunkToReload;
    ConcurrentQueue<Action> mainThreadCallbacks;
    ConcurrentQueue<ThreadedJob> jobsToRun;

    public Dictionary<long, ChunkJobManager> chunkJobsManager;
    public Dictionary<long, Vector2Int> chunkToReloadVisual;

    [Header("Reference")]
    public GeneralAsset generalAsset;
    public Transform mobileRoot;
    public Transform terrainRoot;

    [Header("Parameters")]
    public TerrainLayerParameters[] layerParameters;
    public float pixelPerTile = 16f;
    public int chunkSize = 16;
    public Vector2Int loadRadius;
    public float unloadTimer = 5f;
    public int chunksPerRegionSide = 4;
    public float outOfBoundsRefreshInterval = 0.2f;

    [HideInInspector] public int currentMobileIndex = 0;

    [HideInInspector] public float worldToPixel;
    [HideInInspector] public float pixelToWorld;
    [HideInInspector] public static float invChunkSize;

    private void Awake () {
        if(inst == null) {
            inst = this;
        }

        currentMobileIndex = PlayerPrefs.GetInt("currentMobileIndex", 0);

        mainThreadCallbacks = new ConcurrentQueue<Action>();
        jobsToRun = new ConcurrentQueue<ThreadedJob>();
        
        chunks = new Dictionary<long, DataChunk>();
        unusedChunks = new Queue<DataChunk>();
        chunkToReloadVisual = new Dictionary<long, Vector2Int>();
        chunkJobsManager = new Dictionary<long, ChunkJobManager>();
        mobileChunkToReload = new Queue<int>();
        GeneralAsset.inst = generalAsset;
        generalAsset.Build();

        worldToPixel = pixelPerTile;
        pixelToWorld = 1f / worldToPixel;
        invChunkSize = 1f / chunkSize;
    }

    const int jobsPerFrame = 2;
    private void Update () {
        while(mainThreadCallbacks.Count > 0) {
            mainThreadCallbacks.TryDequeue(out Action result);
            result();
        }

        int jobCount = 0;
        while(jobsToRun.Count > 0) {
            if(jobCount > jobsPerFrame) {
                break;
            }
            jobsToRun.TryDequeue(out ThreadedJob job);
            job.Run();
            jobCount++;
        }
    }

    private void LateUpdate () {
        if(VisualChunkManager.inst != null) {
            foreach(KeyValuePair<long, Vector2Int> kvp in chunkToReloadVisual) {
                VisualChunkManager.inst?.LoadChunkAt(kvp.Value);
            }
            chunkToReloadVisual.Clear();
            while(mobileChunkToReload?.Count > 0) {
                if(VisualChunkManager.inst != null) {
                    int key = mobileChunkToReload.Dequeue();
                    if(VisualChunkManager.inst.mobileChunkPool.ContainsKey(key)) {
                        MobileChunk mc = VisualChunkManager.inst.mobileChunkPool[key];
                        VisualChunkManager.inst.BuildMobileChunk(mc);
                    }
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.L)) {
            GameManager.inst.CompleteSave();
        }
    }

    private void FixedUpdate () {
        GameManager.inst.AutoSaves();
    }
    #endregion

    #region Multithread Chunk Loading
    public void EnqueueMainThreadCallbacks (Action callback) {
        mainThreadCallbacks.Enqueue(callback);
    }

    public void EnqueueJobToRun (ThreadedJob job) {
        jobsToRun.Enqueue(job);
    }

    // The chunks dictionnairy should be used for doing all your reads and writes for tile editing and entities.
    // Since chunks are loaded in threads, sometimes the chunks may not be present even if they have been requested
    // by the chunk loader. To know in what state they should be at an exact moment, this function may be called.
    public JobState GetChunkLoadState (Vector2Int chunkPosition) {
        long key = Hash.longFrom2D(chunkPosition);
        if(chunkJobsManager.TryGetValue(key, out ChunkJobManager cjm)) {
            return cjm.targetLoadState;
        } else {
            return JobState.Unloaded;
        }
    }

    public void StartNewChunkJobAt (Vector2Int chunkPosition, JobState newLoadState, bool isReadonlyChunk, Action job, Action callback, Action cancelCallback, bool runImidialty) {
        long key = Hash.longFrom2D(chunkPosition);
        if(chunkJobsManager.TryGetValue(key, out ChunkJobManager cjm)) {
            cjm.StartNewJob(newLoadState, isReadonlyChunk, job, callback, cancelCallback, runImidialty);
        } else {
            ChunkJobManager newCjm = new ChunkJobManager(chunkPosition);
            chunkJobsManager.Add(key, newCjm);
            newCjm.StartNewJob(newLoadState, isReadonlyChunk, job, callback, cancelCallback, runImidialty);
        }
    }

    public void RemoveJobManager (Vector2Int chunkPosition) {
        RemoveJobManager(Hash.longFrom2D(chunkPosition));
    }

    public void RemoveJobManager (long key) {
        if(chunkJobsManager.ContainsKey(key)) {
            chunkJobsManager.Remove(key);
        }
    }

    public void ForceUnloadJobManager (Vector2Int chunkPosition) {
        ForceUnloadJobManager(Hash.longFrom2D(chunkPosition));
    }

    public void ForceUnloadJobManager (long key) {
        if(chunkJobsManager.TryGetValue(key, out ChunkJobManager cjm)) {
            cjm.ForceUnload();
        }
    }
    #endregion

    #region Default Chunks
    public bool GetChunkAtPosition (Vector2Int position, out DataChunk dataChunk) {
        if(chunks.TryGetValue(Hash.longFrom2D(position), out DataChunk value)) {
            dataChunk = value;
            return true;
        } else {
            dataChunk = null;
            return false;
        }
    }

    public DataChunk GetNewDataChunk (Vector2Int chunkPosition, bool doAddToChunkDictionnairy) {
        DataChunk dataChunk;
        if(unusedChunks.Count <= 0) {
            dataChunk = new DataChunk(/*chunkSize*/);
        } else {
            dataChunk = unusedChunks.Dequeue();
        }
        dataChunk.Init(chunkPosition);
        if(doAddToChunkDictionnairy) {
            AddDataChunkToDictionnairy(dataChunk);
        }

        return dataChunk;
    }

    public void AddDataChunkToDictionnairy (DataChunk dataChunk) {
        chunks.Add(Hash.longFrom2D(dataChunk.chunkPosition), dataChunk);
    }

    public void RemoveDataChunkFromDictionnairy (Vector2Int chunkPosition, bool doEnqueueToUnusedQueue) {
        long key = Hash.longFrom2D(chunkPosition);
        if(chunks.TryGetValue(key, out DataChunk dataChunk)) {
            chunks.Remove(key);
        }
        if(doEnqueueToUnusedQueue) {
            EnqueueDataChunkToUnusedQueue(dataChunk);
        }
    }

    public void EnqueueDataChunkToUnusedQueue (DataChunk dataChunk) {
        // This should NOT be needed but I will leave it there for the time being
        long key = Hash.longFrom2D(dataChunk.chunkPosition);
        if(chunks.ContainsKey(key)) {
            chunks.Remove(key);
        }
        
        unusedChunks.Enqueue(dataChunk);
    }

    public void RefreshSurroundingChunks (Vector2Int chunkPosition, bool cropMiddleForCenter = true) {
        int index = 0;
        for(int y = -1; y < 2; y++) {
            for(int x = -1; x < 2; x++) {
                if(chunks.TryGetValue(Hash.longFrom2D(chunkPosition + new Vector2Int(x, y)), out DataChunk value)) {
                    if(x == 0 && y == 0 && cropMiddleForCenter) {
                        value.RefreshTiles(index, false);
                    } else {
                        value.RefreshTiles(index);
                    }
                }
                index++;
            }
        }
    }

    public void QueueChunkReloadAtTile (int x, int y, TerrainLayers layer) {
        Vector2Int cpos = GetChunkPositionAtTile(x, y);
        long hashKey = Hash.longFrom2D(cpos);
        if(chunkToReloadVisual.ContainsKey(hashKey)) {
            return;
        }

        long key = Hash.hVec2Int(cpos.x, cpos.y);
        bool valid = 
            !NetworkAssistant.inst.IsServer ||
            (ChunkLoader.inst.loadCounters.ContainsKey(key) &&
            ChunkLoader.inst.loadCounters[key].loaders.Contains(NetworkAssistant.inst.ClientID));

        if(!valid) {
            return;
        }

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            chunkToReloadVisual.Add(hashKey, cpos);
        }
    }
    #endregion

    #region Mobile Chunks
    public MobileChunk GetMobileChunkAtPosition (Vector2 position) {
        foreach(KeyValuePair<int, MobileChunk> kvp in VisualChunkManager.inst.mobileChunkPool) {
            if(!kvp.Value.gameObject.activeInHierarchy) {
                continue;
            }

            float worldSizeX = kvp.Value.mobileDataChunk.restrictedSize.x;
            float worldSizeY = kvp.Value.mobileDataChunk.restrictedSize.y;

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
        if(WorldSaving.inst.LoadMobileChunkFile(mobileChunk.mobileDataChunk, GameManager.inst.currentDataLoadMode)) {
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

    public bool GetGlobalIDBitmaskAt (int x, int y, TerrainLayers layer, out int globalID, out int bitmask) {
        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            globalID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            bitmask = dataChunk.GetBitmask(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            return true;
        }

        globalID = 0;
        bitmask = 0;
        return false;
    }

    public bool SetGlobalIDAt (int x, int y, TerrainLayers layer, int globalID, MobileDataChunk mdc = null, bool replicateOnClients = false) {

        if(mdc != null) {
            int oldGID = mdc.GetGlobalID(x, y, layer);
            if(oldGID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer, mdc);
            }

            mdc.SetGlobalID(x, y, layer, globalID);
            if(globalID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer, mdc);
            }
            RefreshTilesAround(x, y, layer, 3, mdc);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            int oldGID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            if(oldGID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer, mdc);
            }

            dataChunk.SetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer, globalID);
            if(globalID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer, mdc);
            }
            RefreshTilesAround(x, y, layer);

            if(NetworkAssistant.inst.IsServer && replicateOnClients) {
                InvokeClientRpcOnEveryone(SetGlobalIDAtClient, x, y, layer, globalID);
            }

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
                GeneralAsset.inst.GetTileAssetFromGlobalID(gid).OnTileRefreshed(new Vector2Int(x, y), layer, mdc);
            }
            QueueMobileChunkReload(mdc.mobileChunk.uid);
            return true;
        }

        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            int gid = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            if(gid != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(gid).OnTileRefreshed(new Vector2Int(x, y), layer);
            }
            QueueChunkReloadAtTile(x, y, layer);
            return true;
        }
        return false;
    }
    #endregion

    #region Network
    #region Tiles
    [ServerRPC(RequireOwnership = false)]
    private void SetGlobalIDAtServer (int x, int y, TerrainLayers layer, int globalID) {
        if(NetworkAssistant.inst.serverPlayerData[ExecutingRpcSender].permissions.editingPermissions != EditingPermissions.EditAllowed) {
            return;
        }
        if(NetworkAssistant.inst.serverPlayerData[ExecutingRpcSender].permissions.inGameEditingPermissions != InGameEditingPermissions.FlyEdit && 
            GameManager.inst.engineMode == EngineModes.Play) {
            return;
        }

        if(!IsHost) {
            Vector2Int cpos = GetChunkPositionAtTile(x, y);

            if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
                int oldGID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
                if(oldGID != 0) {
                    GeneralAsset.inst.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer);
                }

                dataChunk.SetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer, globalID);
                if(globalID != 0) {
                    GeneralAsset.inst.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer);
                }
                RefreshTilesAround(x, y, layer);
            }
        }

        InvokeClientRpcOnEveryoneExcept(SetGlobalIDAtClient, ExecutingRpcSender, x, y, layer, globalID);
    }

    [ClientRPC]
    private void SetGlobalIDAtClient (int x, int y, TerrainLayers layer, int globalID) {
        Vector2Int cpos = GetChunkPositionAtTile(x, y);

        if(GetChunkAtPosition(cpos, out DataChunk dataChunk)) {
            int oldGID = dataChunk.GetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer);
            if(oldGID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(oldGID).OnBreaked(x, y, layer);
            }

            dataChunk.SetGlobalID(x - cpos.x * chunkSize, y - cpos.y * chunkSize, layer, globalID);
            if(globalID != 0) {
                GeneralAsset.inst.GetTileAssetFromGlobalID(globalID).OnPlaced(x, y, layer);
            }
            RefreshTilesAround(x, y, layer);
        }
    }
    #endregion

    #region Tools
    public void UseToolNetworkRequest (BaseToolAsset bta, Action<BitWriter> encodeAction) {
        if(!NetworkAssistant.inst.IsClient) {
            return;
        }

        //Note: Tool's action should now only be executed once the server validates the action, except for brush action with a diameter of 3 or less (< 9 tile edits)
        if(NetworkAssistant.inst.clientPlayerDataCopy.permissions.editingPermissions != EditingPermissions.EditAllowed) {
            return;
        }
        if(NetworkAssistant.inst.clientPlayerDataCopy.permissions.inGameEditingPermissions != InGameEditingPermissions.FlyEdit &&
            GameManager.inst.engineMode == EngineModes.Play) {
            return;
        }

        using(PooledBitStream stream = PooledBitStream.Get()) {
            using(PooledBitWriter writer = PooledBitWriter.Get(stream)) {
                writer.WriteInt32(bta.gid);
                encodeAction(writer);
                stream.Position = 0;
                InvokeServerRpcPerformance(UseToolServer, stream, "Tile");
            }
        }
    }

    [ServerRPC(RequireOwnership = false)]
    void UseToolServer (ulong clientId, Stream stream) {
        if(NetworkAssistant.inst.serverPlayerData[ExecutingRpcSender].permissions.editingPermissions != EditingPermissions.EditAllowed) {
            return;
        }
        if(NetworkAssistant.inst.serverPlayerData[ExecutingRpcSender].permissions.inGameEditingPermissions != InGameEditingPermissions.FlyEdit &&
            GameManager.inst.engineMode == EngineModes.Play) {
            return;
        }

        InvokeClientRpcOnEveryonePerformance(UseToolClient, stream, "Tile");
    }

    [ClientRPC()]
    void UseToolClient (ulong clientId, Stream stream) {
        using(PooledBitReader reader = PooledBitReader.Get(stream)) {
            GeneralAsset.inst.GetToolAssetFromGlobalID(reader.ReadInt32()).DecodeAction(reader);
        }
    }
    #endregion

    #region Default Chunks
    [ClientRPC]
    private void GetChunkClient (int chunkPositionX, int chunkPositionY, DataChunk chunk) {
        ChunkLoader.inst.GetChunkFromServerAt(new Vector2Int(chunkPositionX, chunkPositionY), chunk);
    }

    [ClientRPC]
    private void GetChunkClient (ulong clientId, Stream stream) {
        using(PooledBitReader reader = PooledBitReader.Get(stream)) {
            int chunkPositionX = reader.ReadInt32();
            int chunkPositionY = reader.ReadInt32();
            Vector2Int chunkPosition = new Vector2Int(chunkPositionX, chunkPositionY);
            DataChunk dataChunk = GetNewDataChunk(chunkPosition, false);
            WorldSaving.inst.ReadChunkFromNetworkStream(dataChunk, reader);

            ChunkLoader.inst.GetChunkFromServerAt(new Vector2Int(chunkPositionX, chunkPositionY), dataChunk);
        }
    }

    public void SendChunkToClient (ulong clientID, int chunkPositionX, int chunkPositionY, DataChunk chunk) {
        if(ChunkLoader.inst.loadCounters.TryGetValue(Hash.hVec2Int(chunkPositionX, chunkPositionY), out ChunkLoadCounter clc)) {
            if(clc.lastSendTime.ContainsKey(clientID)) {
                if(NetworkAssistant.inst.Time - clc.lastSendTime[clientID] < unloadTimer - 0.2f) {
                    // Sending too soond

                    return;
                }

                clc.lastSendTime[clientID] = NetworkAssistant.inst.Time;
            } else {
                clc.lastSendTime.Add(clientID, NetworkAssistant.inst.Time);
            }
        }

        using(PooledBitStream stream = PooledBitStream.Get())
        using(PooledBitWriter writer = PooledBitWriter.Get(stream)) {
            writer.WriteInt32(chunkPositionX);
            writer.WriteInt32(chunkPositionY);
            WorldSaving.inst.WriteChunkToNetworkStream(chunk, writer);

            InvokeClientRpcOnClientPerformance(GetChunkClient, clientID, stream, "Chunk");
        }
    }
    #endregion
    #endregion

    #region Utils
    public Vector2Int GetChunkPositionAtTile (Vector2Int position) {
        return Vector2Int.FloorToInt(new Vector2(position.x / (float)chunkSize, position.y / (float)chunkSize));
    }

    public Vector2Int GetRegionPositionAtTile (Vector2Int position) {
        return Vector2Int.FloorToInt(new Vector2(position.x / ((float)chunkSize * chunksPerRegionSide), position.y / ((float)chunkSize * chunksPerRegionSide)));
    }

    public static Vector2Int GetChunkPositionAtTile (int x, int y) {
        return Vector2Int.FloorToInt(new Vector2(x * invChunkSize, y * invChunkSize));
    }

    public Vector2Int WorldToTile (Vector2 worldPos) {
        return Vector2Int.FloorToInt(worldPos);
    }

    public Vector2 TileToWorld (Vector2Int tilePos) {
        return (Vector2)tilePos;
    }

    public Vector2Int WorldToChunk (Vector2 worldPos) {
        return GetChunkPositionAtTile(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }

    public Vector2Int WorldToRegion (Vector2 worldPos) {
        return GetRegionPositionAtTile(Vector2Int.FloorToInt(worldPos));
    }

    public Vector2Int GetLocalPositionAtTile (int x, int y, Vector2Int cpos) {
        return new Vector2Int(x - cpos.x * chunkSize, y - cpos.y * chunkSize);
    }

    public bool IsMobileChunkInLoadedChunks (MobileChunk mobileChunk) {
        Vector2Int min = WorldToChunk(mobileChunk.position);
        Vector2Int max = WorldToChunk(mobileChunk.position + (Vector3)mobileChunk.boxCollider.size);

        for(int x = min.x; x <= max.x; x++) {
            for(int y = min.y; y <= max.y; y++) {
                if(!chunks.ContainsKey(Hash.hVec2Int(x, y))) {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsEntityInLoadedChunks (Entity entity) {
        Vector2Int min = WorldToChunk(entity.entityData.position + entity.asset.loadBoxOffset - (entity.asset.loadBoxSize * 0.5f));
        Vector2Int max = WorldToChunk(entity.entityData.position + entity.asset.loadBoxOffset + (entity.asset.loadBoxSize * 0.5f));

        for(int x = min.x; x <= max.x; x++) {
            for(int y = min.y; y <= max.y; y++) {
                if(!chunks.ContainsKey(Hash.hVec2Int(x, y))) {
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
