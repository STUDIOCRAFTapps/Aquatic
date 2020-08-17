using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using System;
using System.Linq;
using Newtonsoft.Json;

using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;

public enum DataLoadMode {
    TryReadonly,
    Default,
    Readonly
}

public class WorldSaving : MonoBehaviour {

    #region Header and Initiation
    private static WorldSaving _instance;

    public static WorldSaving inst {
        get {
            if(_instance == null) {
                _instance = new GameObject("WorldSaving").AddComponent<WorldSaving>();
                DontDestroyOnLoad(_instance);
                _instance.Init();
            }

            return _instance;
        }
    }

    [Header("Variables")]
    private string saveFolderName = "new_save";
    public int dimension {
        get; private set;
    }

    // Const
    public const string savesFolder = "saves";
    public const string editFolder = "edit";
    public const string playFolder = "play";
    public static readonly string[] dimentions = { "overworld" };

    public const string chunkDataFolder = "chunk_data";
    public const string regionFolder = "entity_regions";
    public const string mobileChunkDataFolder = "mobile_chunk_data";
    public const string entityDataFolder = "entity_data";
    public const string playerDataFolder = "player_data";

    public const string chunkFileEnd = ".cdat";
    public const string regionFileEnd = ".erg";
    public const string mobileChunkDataEnd = ".mdat";
    public const string entityDataEnd = ".edat";

    public const string chunkFileSeparator = "_";
    public const string tileStringSeparator = ":";
    public const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_:";
    public const string regionFileSeparator = "_";

    // Privates
    char s; // Separator char
    string datapath;
    string chunkDatapath;
    string chunkReadonlyDatapath;
    string mobileChunkDatapath;
    string mobileChunkReadonlyDatapath;
    string entityDatapath;
    string entityReadonlyDatapath;
    string regionsDatapath;
    string regionsReadonlyDatapath;
    string playerDatapath;

    //StringBuilder sb;
    Dictionary<char, byte> charToByte;
    List<string> layerNames;
    JsonSerializerSettings jss;

    // Shared Ressources
    ConcurrentQueue<byte[]> bufferPool;
    const int bufferCount = 8;
    public const int bufferSize = 8192;

    public bool clientMode = false;

    void Init () {

        bufferPool = new ConcurrentQueue<byte[]>();
        for(int i = 0; i < bufferCount; i++) {
            bufferPool.Enqueue(new byte[bufferSize]);
        }

        s = Path.DirectorySeparatorChar;
        datapath = Application.persistentDataPath;
        //sb = new StringBuilder();
        jss = new JsonSerializerSettings() {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        charToByte = new Dictionary<char, byte>();
        char[] authorizedChars = authorizedCharsString.ToCharArray();
        for(byte i = 0; i < authorizedChars.Length; i++) {
            charToByte.Add(authorizedChars[i], i);
        }

        layerNames = Enum.GetNames(typeof(TerrainLayers)).ToList();
    }

    public void PrepareNewSave (string folderName) {
        clientMode = false;
        saveFolderName = folderName;
        ComposeDataPaths();
    }

    public void PrepareClient () {
        clientMode = true;
    }

    public void OnReloadEngine () {
        ComposeDataPaths();
    }


    public int GetLayerCount () {
        return layerNames.Count;
    }
    #endregion


    #region Datapaths
    public void SetDimension (int dimension) {
        this.dimension = dimension;
        ComposeDataPaths();
    }

    void ComposeDataPaths () {
        // Engine Mode Folder
        string engmf = (GameManager.inst.engineMode == EngineModes.Edit) ? editFolder : playFolder;

        mobileChunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + mobileChunkDataFolder + s;
        mobileChunkReadonlyDatapath = datapath + s + savesFolder + s + saveFolderName + s + editFolder + s + dimentions[dimension] + s + mobileChunkDataFolder + s;
        if(!Directory.Exists(mobileChunkDatapath)) {
            Directory.CreateDirectory(mobileChunkDatapath);
        }

        chunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + chunkDataFolder + s;
        chunkReadonlyDatapath = datapath + s + savesFolder + s + saveFolderName + s + editFolder + s + dimentions[dimension] + s + chunkDataFolder + s;
        if(!Directory.Exists(chunkDatapath)) {
            Directory.CreateDirectory(chunkDatapath);
        }

        entityDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + entityDataFolder + s;
        entityReadonlyDatapath = datapath + s + savesFolder + s + saveFolderName + s + editFolder + s + dimentions[dimension] + s + entityDataFolder + s;
        if(!Directory.Exists(entityDatapath)) {
            Directory.CreateDirectory(entityDatapath);
        }

        regionsDatapath = datapath + s + savesFolder + s + inst.saveFolderName + s + engmf + s + dimentions[dimension] + s + regionFolder + s;
        regionsReadonlyDatapath = datapath + s + savesFolder + s + inst.saveFolderName + s + editFolder + s + dimentions[dimension] + s + regionFolder + s;
        if(!Directory.Exists(regionsDatapath)) {
            Directory.CreateDirectory(regionsDatapath);
        }

        playerDatapath = datapath + s + savesFolder + s + saveFolderName + s + playerDataFolder + s;
        if(!Directory.Exists(playerDatapath)) {
            Directory.CreateDirectory(playerDatapath);
        }
    }

    public string GetChunkDirectory (DataChunk dataChunk, bool useReadonlyPath) {
        StringBuilder sb = new StringBuilder();
        sb.Append(useReadonlyPath ? chunkReadonlyDatapath : chunkDatapath);
        sb.Append(dataChunk.chunkPosition.x);
        sb.Append(chunkFileSeparator);
        sb.Append(dataChunk.chunkPosition.y);
        sb.Append(chunkFileEnd);

        return sb.ToString();
    }

    public string GetMobileChunkDirectory (MobileDataChunk mobileDataChunk, bool useReadonlyPath) {
        StringBuilder sb = new StringBuilder();
        sb.Append(useReadonlyPath ? mobileChunkReadonlyDatapath : mobileChunkDatapath);
        sb.Append(mobileDataChunk.mobileChunk.uid);
        sb.Append(mobileChunkDataEnd);

        return sb.ToString();
    }

    public string GetEntityDirectory (int uid, bool useReadonlyPath) {
        StringBuilder sb = new StringBuilder();
        sb.Append(useReadonlyPath ? entityReadonlyDatapath : entityDatapath);
        sb.Append(uid);
        sb.Append(entityDataEnd);

        return sb.ToString();
    }

    public string GetRegionDirectory (EntityRegion entityRegion, bool useReadonlyPath) {
        StringBuilder sb = new StringBuilder();
        sb.Append(useReadonlyPath ? regionsReadonlyDatapath : regionsDatapath);
        sb.Append(entityRegion.regionPosition.x);
        sb.Append(regionFileSeparator);
        sb.Append(entityRegion.regionPosition.y);
        sb.Append(regionFileEnd);

        return sb.ToString();
    }

    public string GetPlayerDirectory (int uid) {
        StringBuilder sb = new StringBuilder();
        sb.Append(playerDatapath);
        sb.Append(uid);
        sb.Append(entityDataEnd);

        return sb.ToString();
    }
    #endregion

    #region Front-End Functions
    #region Save
    public void SaveChunk (DataChunk source, bool useReadonlyPath) {
        if(clientMode) {
            return;
        }

        //Serialize and save.
        PooledBitStream bitStream = ThreadSafeBitStream.GetStreamSafe();
        PooledBitWriter bw = ThreadSafeBitStream.GetWriterSafe(bitStream);
        SerializeToStream(bw, source);

        using(FileStream fs = new FileStream(GetChunkDirectory(source, useReadonlyPath), FileMode.Create)) {
            fs.Write(bitStream.GetBuffer(), 0, (int)bitStream.Length);
        }

        bw.DisposeWriterSafe();
        bitStream.DisposeStreamSafe();
    }

    public void WriteChunkToNetworkStream (DataChunk dataChunk, PooledBitWriter writer) {
        SerializeToNetworkStream(writer, dataChunk);
    }

    public void SaveMobileChunk (MobileDataChunk dataChunk) {
        if(clientMode) {
            return;
        }

        //Serialize and save.
        PooledBitStream bitStream = ThreadSafeBitStream.GetStreamSafe();
        PooledBitWriter bw = ThreadSafeBitStream.GetWriterSafe(bitStream);
        SerializeToStream(bw, dataChunk);

        using(FileStream fs = new FileStream(GetChunkDirectory(dataChunk, false), FileMode.Create)) {
            fs.Write(bitStream.GetBuffer(), 0, (int)bitStream.Length);
        }

        bw.DisposeWriterSafe();
        bitStream.DisposeStreamSafe();
    }

    public void SaveEntity (Entity entity) {
        if(clientMode) {
            return;
        }

        //Serialize and save.
        using(FileStream fs = new FileStream(GetEntityDirectory(entity.entityData.uid, false), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(StreamWriter sw = new StreamWriter(defs))
        using(JsonWriter jw = new JsonTextWriter(sw)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);

            serializer.Serialize(jw, new EntityDataWrapper(entity.entityData));
        }
    }

    public void SavePlayer (PlayerStatus status, int uid) {
        if(clientMode) {
            return;
        }

        //Serialize and save.
        using(FileStream fs = new FileStream(GetPlayerDirectory(uid), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(StreamWriter sw = new StreamWriter(defs))
        using(JsonWriter jw = new JsonTextWriter(sw)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            serializer.Serialize(jw, status);
        }
    }

    public void SaveRegionFile (EntityRegion entityRegion) {
        if(clientMode) {
            return;
        }

        byte[] buffer = GetByteBuffer();
        using(FileStream fs = new FileStream(GetRegionDirectory(entityRegion, false), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(MemoryStream ms = new MemoryStream(buffer))
        using(BinaryWriter bw = new BinaryWriter(ms)) {
            SerializeRegionToStream(bw, entityRegion);
            defs.Write(buffer, 0, (int)ms.Position);
        }
        ReturnByteBuffer(buffer);
    }
    #endregion

    #region Load
    public bool LoadChunkFile (DataChunk dataChunk, DataLoadMode loadMode) {
        if(clientMode) {
            return false;
        }

        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = string.Empty;
        if(loadMode == DataLoadMode.Default || loadMode == DataLoadMode.TryReadonly)
            chunkPath = GetChunkDirectory(dataChunk, false);
        if(loadMode == DataLoadMode.Readonly)
            chunkPath = GetChunkDirectory(dataChunk, true);

        bool hasSucceded;
        if(!File.Exists(chunkPath)) {
            if(loadMode == DataLoadMode.TryReadonly) {
                return LoadChunkFile(dataChunk, DataLoadMode.Readonly);
            } else {
                return false;
            }
        } else {
            try {
                PooledBitStream bitStream = ThreadSafeBitStream.GetStreamSafe();

                byte[] buffer = GetByteBuffer();
                using(FileStream fs = new FileStream(chunkPath, FileMode.Open)) {
                    fs.Read(buffer, 0, (int)fs.Length);
                    bitStream.Write(buffer, 0, (int)fs.Length);
                    bitStream.Position = 0;
                }
                ReturnByteBuffer(buffer);
                PooledBitReader bw = ThreadSafeBitStream.GetReaderSafe(bitStream);
                hasSucceded = DeserializeFromStream(bw, dataChunk);

                bw.DisposeReaderSafe();
                bitStream.DisposeStreamSafe();

            } catch(Exception e) {
                Debug.Log("Failed to load chunk: " + e.ToString());
                hasSucceded = false;
            }
        }

        if(!hasSucceded) {
            Debug.LogError($"A chunk at {dataChunk.chunkPosition} failed to be deserialized. Its file was deleted.");
            //File.Delete(chunkPath);
        }

        if(!hasSucceded && loadMode == DataLoadMode.TryReadonly) {
            return LoadChunkFile(dataChunk, DataLoadMode.Readonly);
        } else {
            return hasSucceded;
        }
    }

    public bool ReadChunkFromNetworkStream (DataChunk dataChunk, PooledBitReader reader) {
        
        bool hasSucceded = DeserializeFromNetworkStream(reader, dataChunk);
        return hasSucceded;
    }

    public bool LoadMobileChunkFile (MobileDataChunk dataChunk, DataLoadMode loadMode) {
        if(clientMode) {
            return false;
        }

        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = string.Empty;
        if(loadMode == DataLoadMode.Default || loadMode == DataLoadMode.TryReadonly)
            chunkPath = GetMobileChunkDirectory(dataChunk, false);
        if(loadMode == DataLoadMode.Readonly)
            chunkPath = GetMobileChunkDirectory(dataChunk, true);

        bool hasSucceded;
        if(!File.Exists(chunkPath)) {
            if(loadMode == DataLoadMode.TryReadonly) {
                return LoadMobileChunkFile(dataChunk, DataLoadMode.Readonly);
            } else {
                return false;
            }
        } else {
            PooledBitStream bitStream = ThreadSafeBitStream.GetStreamSafe();
            PooledBitReader bw = ThreadSafeBitStream.GetReaderSafe(bitStream);

            hasSucceded = DeserializeFromStream(bw, dataChunk);
            byte[] buffer = GetByteBuffer();
            using(FileStream fs = new FileStream(chunkPath, FileMode.Open)) {
                fs.Write(bitStream.GetBuffer(), 0, (int)fs.Length);
            }
            ReturnByteBuffer(buffer);

            bw.DisposeReaderSafe();
            bitStream.DisposeStreamSafe();
        }
        
        if(!hasSucceded) {
            Debug.LogError($"A mobile chunk with uid {dataChunk.mobileChunk.uid} failed to be deserialized. Its file was deleted.");
            File.Delete(chunkPath);
        }

        if(!hasSucceded && loadMode == DataLoadMode.TryReadonly) {
            return LoadMobileChunkFile(dataChunk, DataLoadMode.Readonly);
        } else {
            return hasSucceded;
        }
    }

    public bool LoadEntityFile (int uid, int gid, EntityData entityData, DataLoadMode loadMode) {
        if(clientMode) {
            return false;
        }

        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string entityPath = string.Empty;
        if(loadMode == DataLoadMode.Default || loadMode == DataLoadMode.TryReadonly)
            entityPath = GetEntityDirectory(uid, false);
        if(loadMode == DataLoadMode.Readonly)
            entityPath = GetEntityDirectory(uid, true);

        bool hasSucceded;
        if(!File.Exists(entityPath)) {
            if(loadMode == DataLoadMode.TryReadonly) {
                return LoadEntityFile(uid, gid, entityData, DataLoadMode.Readonly);
            } else {
                entityData = null;
                return false;
            }
        } else {
            using(FileStream fs = new FileStream(entityPath, FileMode.Open))
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
            using(StreamReader sr = new StreamReader(defs))
            using(JsonReader jr = new JsonTextReader(sr)) {
                JsonSerializer serializer = JsonSerializer.Create(jss);

                EntityDataWrapper edw = new EntityDataWrapper(entityData);
                serializer.Populate(jr, edw);
                //entityData = serializer.Deserialize<EntityDataWrapper>(jr).entityData;
                hasSucceded = true;
            }
        }

        if(!hasSucceded) {
            Debug.LogError($"An entity with uid {uid} failed to be deserialized. Its file was deleted.");
            File.Delete(entityPath);
        }
        if(!hasSucceded && loadMode == DataLoadMode.TryReadonly) {
            return LoadEntityFile(uid, gid, entityData, DataLoadMode.Readonly);
        } else {
            return hasSucceded;
        }
        
    }

    public bool LoadRegionFile (EntityRegion entityRegion, DataLoadMode loadMode) {
        if(clientMode) {
            return false;
        }

        string regionPath = string.Empty;
        if(loadMode == DataLoadMode.Default || loadMode == DataLoadMode.TryReadonly)
            regionPath = GetRegionDirectory(entityRegion, false);
        if(loadMode == DataLoadMode.Readonly)
            regionPath = GetRegionDirectory(entityRegion, true);

        bool hasSucceded;
        if(!File.Exists(regionPath)) {
            if(loadMode == DataLoadMode.TryReadonly) {
                return LoadRegionFile(entityRegion, DataLoadMode.Readonly);
            } else {
                return false;
            }
        } else {
            try {
                byte[] buffer = GetByteBuffer();
                using(FileStream fs = new FileStream(regionPath, FileMode.Open))
                using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
                using(MemoryStream ms = new MemoryStream(buffer))
                using(BinaryReader br = new BinaryReader(ms)) {
                    int rlength = defs.Read(buffer, 0, buffer.Length);
                    hasSucceded = DeserializeStreamToRegion(br, entityRegion);
                }
                ReturnByteBuffer(buffer);
            } catch(System.Exception e) {
                Debug.Log("Failed to load chunk: " + e.ToString());
                hasSucceded = false;
            }
        }

        if(!hasSucceded) {
            Debug.LogError($"A region at {entityRegion.regionPosition} failed to be deserialized. Its file was deleted.");
            File.Delete(regionPath);
        }

        if(!hasSucceded && loadMode == DataLoadMode.TryReadonly) {
            return LoadRegionFile(entityRegion, DataLoadMode.Readonly);
        } else {
            return hasSucceded;
        }
    }

    public bool LoadPlayerFile (int uid, out PlayerStatus playerData) {
        if(clientMode) {
            playerData = null;
            return false;
        }

        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string entityPath = GetPlayerDirectory(uid);

        bool hasSucceded;
        if(!File.Exists(entityPath)) {
            Debug.Log("failed loading player data");
            playerData = null;
            return false;
        } else {
            using(FileStream fs = new FileStream(entityPath, FileMode.Open))
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
            using(StreamReader sr = new StreamReader(defs))
            using(JsonReader jr = new JsonTextReader(sr)) {
                JsonSerializer serializer = JsonSerializer.Create(jss);

                playerData = serializer.Deserialize<PlayerStatus>(jr);
                hasSucceded = true;
            }
        }

        if(!hasSucceded) {
            Debug.LogError($"A player with uid {uid} failed to be deserialized. Its file was deleted.");
            File.Delete(entityPath);
        }
        return hasSucceded;
    }
    #endregion

    #region Delete
    public void DeleteChunk (DataChunk target) {
        if(clientMode) {
            return;
        }

        string chunkPath = GetChunkDirectory(target, false);
        if(File.Exists(chunkPath)) {
            File.Delete(chunkPath);
        }

    }

    public void DeleteMobileChunk (MobileDataChunk target) {
        if(clientMode) {
            return;
        }

        string chunkPath = GetMobileChunkDirectory(target, false);
        if(File.Exists(chunkPath)) {
            File.Delete(chunkPath);
        }
        
    }

    public void DeleteEntity (Entity entity) {
        if(clientMode) {
            return;
        }

        string entityPath = GetEntityDirectory(entity.entityData.uid, false);
        if(File.Exists(entityPath)) {
            File.Delete(entityPath);
        }

    }
    #endregion

    #region Delete Directory
    public void ClearPlayFolders () {
        ClearFolder(datapath + s + savesFolder + s + saveFolderName + s + playFolder);
    }

    public static void ClearFolder (string directory) {
        if(!Directory.Exists(directory)) {
            return;
        }

        DirectoryInfo dir = new DirectoryInfo(directory);

        foreach(FileInfo fi in dir.GetFiles()) {
            fi.Delete();
        }
        foreach(DirectoryInfo di in dir.GetDirectories()) {
            ClearFolder(di.FullName);
            di.Delete();
        }
    }
    #endregion
    #endregion


    #region Stream Functions
    #region SerializeToStream
    void SerializeToStream (BitWriter ms, DataChunk dataChunk) {
        dataChunk.RefreshLayerEdit();

        SerializePalette(ms, dataChunk);

        SerializeTileData(ms, dataChunk);
    }

    void SerializeToNetworkStream (PooledBitWriter ms, DataChunk dataChunk) {
        dataChunk.RefreshLayerEdit();

        SerializePaletteNetwork(ms, dataChunk);

        SerializeTileDataNetwork(ms, dataChunk);
    }

    void SerializeToStream (BitWriter ms, MobileDataChunk mobileDataChunk) {
        SerializeMobileInfos(ms, mobileDataChunk);

        SerializePalette(ms, mobileDataChunk);

        SerializeTileData(ms, mobileDataChunk);
    }

    void SerializeRegionToStream (BinaryWriter ms, EntityRegion entityRegion) {
        for(int x = 0; x < TerrainManager.inst.chunksPerRegionSide; x++) {
            for(int y = 0; y < TerrainManager.inst.chunksPerRegionSide; y++) {
                ms.Write((ushort)(entityRegion.subRegions[x][y].mobileChunkUIDs.Count));
                ms.Write((ushort)(entityRegion.subRegions[x][y].entitiesUIDs.Count));

                for(int i = 0; i < entityRegion.subRegions[x][y].mobileChunkUIDs.Count; i++) {
                    ms.Write(entityRegion.subRegions[x][y].mobileChunkUIDs[i]);
                }
                foreach(KeyValuePair<int, EntityUIDAssetPair> kvp in entityRegion.subRegions[x][y].entitiesUIDs) {
                    ms.Write(kvp.Value.uid);
                    EntityAsset ea = GeneralAsset.inst.GetEntityAssetFromGlobalID(kvp.Value.gid);
                    ms.Write(ea.collection.parent.id);
                    ms.Write(ea.id);
                }
            }
        }
    }
    #endregion

    #region DeserializeFromStream
    bool DeserializeFromStream (BitReader ms, DataChunk dataChunk) {
        if(!DeserializePalette(ms, dataChunk)) {
            Debug.Log("Palette has issues");
            return false;
        }

        if(!DeserializeTileData(ms, dataChunk)) {
            Debug.Log("Tile has issues");
            return false;
        }

        CleanPalette(dataChunk);

        return true;
    }

    bool DeserializeFromNetworkStream (PooledBitReader ms, DataChunk dataChunk) {
        if(!DeserializePaletteNetwork(ms, dataChunk)) {
            Debug.Log("Palette has issues");
            return false;
        }

        if(!DeserializeTileDataNetwork(ms, dataChunk)) {
            Debug.Log("Tile has issues");
            return false;
        }

        CleanPalette(dataChunk);

        return true;
    }

    bool DeserializeFromStream (BitReader ms, MobileDataChunk mobileDataChunk) {
        DeserializeMobileInfos(ms, mobileDataChunk);

        if(!DeserializePalette(ms, mobileDataChunk)) {
            Debug.Log("Failed palette");
            return false;
        }

        if(!DeserializeTileData(ms, mobileDataChunk)) {
            Debug.Log("Failed tile data");
            return false;
        }

        CleanPalette(mobileDataChunk);

        return true;
    }

    bool DeserializeStreamToRegion (BinaryReader ms, EntityRegion entityRegion) {
        for(int x = 0; x < TerrainManager.inst.chunksPerRegionSide; x++) {
            for(int y = 0; y < TerrainManager.inst.chunksPerRegionSide; y++) {
                int mobileChunkInRegion = ms.ReadUInt16();
                int entitiesInRegion = ms.ReadUInt16();
                if(mobileChunkInRegion < 0 && entitiesInRegion < 0) {
                    return false;
                }

                int i = 0;
                for(; i < mobileChunkInRegion; i++) {
                    entityRegion.subRegions[x][y].mobileChunkUIDs.Add(ms.ReadInt32());
                }
                i = 0;
                for(; i < entitiesInRegion; i++) {
                    int uid = ms.ReadInt32();
                    if(GeneralAsset.inst.GetGlobalIDFromEntityString(new EntityString(ms.ReadString(), ms.ReadString()), out int gid)) {
                        entityRegion.subRegions[x][y].entitiesUIDs.Add(uid, new EntityUIDAssetPair(uid, gid));
                    }
                }
            }
        }

        return true;
    }
    #endregion
    #endregion

    #region Palette
    void SerializePalette (BitWriter ms, DataChunk dataChunk) {
        ms.WriteByte((byte)dataChunk.globalIDPalette.Count);
        for(int i = 0; i < dataChunk.globalIDPalette.Count; i++) {
            //This shouldn't create much allocations, it just reference already written strings
            TileString tileString = GeneralAsset.inst.GetTileStringFromGlobalID(dataChunk.globalIDPalette[i]);

            //Mark the length
            ms.WriteByte((byte)(tileString.nspace.Length + tileString.id.Length + 1));

            //Zero allocation method to write the string to the stream; using a reference array (char -> byte).
            for(int l = 0; l < tileString.nspace.Length; l++) {
                ms.WriteByte(charToByte[tileString.nspace[l]]);
            }
            ms.WriteByte(charToByte[':']);
            for(int l = 0; l < tileString.id.Length; l++) {
                ms.WriteByte(charToByte[tileString.id[l]]);
            }
        }
    }

    void SerializePaletteNetwork (PooledBitWriter ms, DataChunk dataChunk) {
        ms.WriteByte((byte)dataChunk.globalIDPalette.Count);
        for(int i = 0; i < dataChunk.globalIDPalette.Count; i++) {
            ms.WriteInt32(dataChunk.globalIDPalette[i]); // Client/Server should have the same global IDs. They can be sent directly
        }
    }

    bool DeserializePalette (BitReader ms, DataChunk dataChunk) {
        int palCount = ms.ReadByte();
        if(palCount == -1) {
            return false;
        }

        StringBuilder sb = new StringBuilder();
        TileString tileString = new TileString();

        for(int i = 0; i < palCount; i++) {
            byte readLength = (byte)ms.ReadByte();
            int readIndex = 0;
            byte readByte;
            sb.Clear();

            //Read the namespace
            for(; readIndex < readLength; readIndex++) {
                readByte = (byte)ms.ReadByte();
                if(authorizedCharsString[readByte] == ':') {
                    readIndex++;
                    break;
                } else {
                    sb.Append(authorizedCharsString[readByte]);
                }
            }
            if(readIndex >= readLength) {
                return false;
            }
            tileString.nspace = sb.ToString();
            sb.Clear();

            //Read the id
            for(; readIndex < readLength; readIndex++) {
                sb.Append(authorizedCharsString[ms.ReadByte()]);
            }
            tileString.id = sb.ToString();
            if(GeneralAsset.inst.GetGlobalIDFromTileString(tileString, out int gID)) {
                dataChunk.globalIDPalette.Add(gID);
                dataChunk.globalIDHashs.Add(gID);
            } else {
                dataChunk.globalIDPalette.Add(-1);
            }
        }
        return true;
    }

    bool DeserializePaletteNetwork (PooledBitReader ms, DataChunk dataChunk) {
        int palCount = ms.ReadByte();
        if(palCount == -1) {
            return false;
        }

        for(int i = 0; i < palCount; i++) {
            int gid = ms.ReadInt32();
            dataChunk.globalIDPalette.Add(gid);
            dataChunk.globalIDHashs.Add(gid);
        }
        return true;
    }

    // Cleans errors (tilestring not found -> -1) from the palette
    void CleanPalette (DataChunk dataChunk) {
        for(int limit = 0; limit < 255; limit++) {
            if(dataChunk.globalIDPalette.Contains(-1)) {
                dataChunk.globalIDPalette.Remove(-1);
            } else {
                break;
            }
        }
    }
    #endregion

    #region TileData
    void SerializeTileData (BitWriter ms, DataChunk dataChunk) {
        int layerCount = 0;
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                layerCount++;
            }
        }
        ms.WriteByte((byte)layerCount);

        int chunkSize = dataChunk.chunkSize;
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                continue;
            }
            ms.WritePadBits();
            ms.WriteString(layerNames[l], true);

            #region Tile RLE Encoding
            int lenght = 0;
            int lastIndex = -1;
            int currentIndex = -1;
            for(int y = 0; y < dataChunk.chunkSize; y++) {
                bool inv = (y % 2 == 0);
                for(int x = inv ? 0 : (dataChunk.chunkSize - 1); inv ? (x < dataChunk.chunkSize) : (x >= 0); x = x + (inv ? 1 : -1)) {  // Zig Zags increase efficiency
                    currentIndex = (dataChunk.globalIDPalette.IndexOf(dataChunk.GetGlobalID(x, y, (TerrainLayers)l)) + 1);

                    // RLE -- If the tile is continued, extend the lenght.
                    if(lastIndex == currentIndex) {
                        lenght++;
                    } else {
                        // RLE -- If the lenght is above 1, a long chain has been broken. Write the type bit, the index and lenght.
                        // RLE -- If the lenght is 1, a one tile chain has been broken. Write the type bit, the index.
                        // RLE -- If the lenght is 0, the encoding has just started. Continue.
                        if(lenght > 1) {
                            ms.WriteBit(true); // True : Long (2 Bytes)
                            //ms.WriteByte(1); // True : Long (2 Bytes)
                            ms.WriteByte((byte)lenght);
                            ms.WriteByte((byte)lastIndex);
                        } else if(lenght == 1) {
                            ms.WriteBit(false); // False : Single (1 Bytes)
                            //ms.WriteByte(0); // False : Single (1 Bytes)
                            ms.WriteByte((byte)lastIndex);
                        }
                        lenght = 1;
                    }
                    lastIndex = currentIndex;
                }
            }

            // RLE -- On going chains must be stopped on ending
            if(lenght > 1) {
                ms.WriteBit(true);
                //ms.WriteByte(1);
                ms.WriteByte((byte)lenght);
                ms.WriteByte((byte)lastIndex);
            } else if(lenght == 1) {
                ms.WriteBit(false);
                //ms.WriteByte(0);
                ms.WriteByte((byte)lastIndex);
            }
            #endregion

            #region Bitmask RLE Encoding
            lenght = 0;
            lastIndex = -1;
            currentIndex = -1;
            for(int y = 2; y < dataChunk.chunkSize - 2; y++) {
                for(int x = 2; x < dataChunk.chunkSize - 2; x++) { // Zig Zags are not usefull with bitmask
                    // Air tile can be skipped without issues.
                    if(dataChunk.GetGlobalID(x, y, (TerrainLayers)l) == 0) {
                        continue;
                    }
                    currentIndex = dataChunk.GetBitmask(x, y, (TerrainLayers)l);

                    // RLE -- If the tile is continued, extend the lenght.
                    if(lastIndex == currentIndex) {
                        lenght++;
                    } else {
                        // RLE -- If the lenght is above 1, a long chain has been broken. Write the type bit, the index and lenght.
                        // RLE -- If the lenght is 1, a one tile chain has been broken. Write the type bit, the index.
                        // RLE -- If the lenght is 0, the encoding has just started. Continue.
                        if(lenght > 1) {
                            ms.WriteBit(true); // True : Long (2 Bytes)
                            //ms.WriteByte(1); // True : Long (2 Bytes)
                            ms.WriteByte((byte)lenght);
                            ms.WriteUInt16((ushort)lastIndex);
                        } else if(lenght == 1) {
                            ms.WriteBit(false); // False : Single (1 Bytes)
                            //ms.WriteByte(0); // False : Single (1 Bytes)
                            ms.WriteUInt16((ushort)lastIndex);
                        }
                        lenght = 1;
                    }
                    lastIndex = currentIndex;
                }
            }


            // RLE -- On going chains must be stopped on ending
            if(lenght > 1) {
                ms.WriteBit(true);
                //ms.WriteByte(1);
                ms.WriteByte((byte)lenght);
                ms.WriteUInt16((ushort)lastIndex);
            } else if(lenght == 1) {
                ms.WriteBit(false);
                //ms.WriteByte(0);
                ms.WriteUInt16((ushort)lastIndex);
            }
            #endregion
        }
    }

    void SerializeTileDataNetwork (PooledBitWriter ms, DataChunk dataChunk) {
        int layerCount = 0;
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                layerCount++;
            }
        }

        ms.WriteByte((byte)layerCount);

        int chunkSize = dataChunk.chunkSize;
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                continue;
            }
            ms.WriteByte((byte)l); // Client/Server should have matched layers. No need to share actual layer names

            #region Tile RLE Encoding
            int lenght = 0;
            int lastIndex = -1;
            int currentIndex = -1;
            for(int y = 0; y < dataChunk.chunkSize; y++) {
                bool inv = (y % 2 == 0);
                for(int x = inv ? 0 : (dataChunk.chunkSize - 1); inv ? (x < dataChunk.chunkSize) : (x >= 0); x = x + (inv ? 1 : -1)) {  // Zig Zags increase efficiency
                    currentIndex = (byte)(dataChunk.globalIDPalette.IndexOf(dataChunk.GetGlobalID(x, y, (TerrainLayers)l)) + 1);

                    // RLE -- If the tile is continued, extend the lenght.
                    if(lastIndex == currentIndex) {
                        lenght++;
                    } else {
                        // RLE -- If the lenght is above 1, a long chain has been broken. Write the type bit, the index and lenght.
                        // RLE -- If the lenght is 1, a one tile chain has been broken. Write the type bit, the index.
                        // RLE -- If the lenght is 0, the encoding has just started. Continue.
                        if(lenght > 1) {
                            ms.WriteBit(true); // True : Long (2 Bytes)
                            ms.WriteByte((byte)lenght);
                            ms.WriteByte((byte)lastIndex);
                        } else if(lenght == 1) {
                            ms.WriteBit(false); // False : Single (1 Bytes)
                            ms.WriteByte((byte)lastIndex);
                        }
                        lenght = 1;
                    }
                    lastIndex = currentIndex;
                }
            }

            // RLE -- On going chains must be stopped on ending
            if(lenght > 1) {
                ms.WriteBit(true);
                ms.WriteByte((byte)lenght);
                ms.WriteByte((byte)currentIndex);
            } else if(lenght == 1) {
                ms.WriteBit(false);
                ms.WriteByte((byte)currentIndex);
            }
            #endregion
        }
    }

    bool DeserializeTileData (BitReader ms, DataChunk dataChunk) {
        int layerCount = ms.ReadByte();
        StringBuilder sb = new StringBuilder();

        dataChunk.ClearLayerEdits();

        for(int ll = 0; ll < layerCount; ll++) {
            ms.SkipPadBits();
            string layerName = ms.ReadString(sb, true).ToString();
            sb.Clear();
            int l = layerNames.IndexOf(layerName);
            if(l < 0) {
                return false;
            }

            #region Tile RLE Decoding
            bool isLong = false;
            int tileIndex = 0;
            int lenght = 0;
            for(int y = 0; y < dataChunk.chunkSize; y++) {
                bool inv = (y % 2 == 0);
                for(int x = inv ? 0 : (dataChunk.chunkSize - 1); inv ? (x < dataChunk.chunkSize) : (x >= 0); x = x + (inv ? 1 : -1)) {

                    // RLE -- The run has ended. The next batch must be decoded
                    if(lenght == 0) {
                        isLong = ms.ReadBit();
                        //isLong = ms.ReadByte() == 1;

                        if(isLong) {
                            lenght = ms.ReadByte();
                            tileIndex = ms.ReadByte();
                        } else {
                            tileIndex = ms.ReadByte();
                            lenght = 1;
                        }
                    }
                    lenght--;

                    if(tileIndex <= 0) {
                        dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                    } else {
                        if((tileIndex - 1) >= dataChunk.globalIDPalette.Count) {
                            Debug.LogError("A tile has a palette index greater or equal to the palette length.");
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        } else if(dataChunk.globalIDPalette[tileIndex - 1] != -1) {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, dataChunk.globalIDPalette[tileIndex - 1]);
                        } else {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        }
                    }
                }
            }
            #endregion

            #region Bitmask RLE Decoding
            lenght = 0;
            tileIndex = 0;
            for(int y = 2; y < dataChunk.chunkSize - 2; y++) {
                for(int x = 2; x < dataChunk.chunkSize - 2; x++) {
                    if(dataChunk.GetGlobalID(x, y, (TerrainLayers)l) == 0) {
                        continue;
                    }

                    if(lenght == 0) {
                        isLong = ms.ReadBit();

                        if(isLong) {
                            lenght = ms.ReadByte();
                            tileIndex = ms.ReadUInt16();
                        } else {
                            tileIndex = ms.ReadUInt16();
                            lenght = 1;
                        }
                    }
                    lenght--;

                    dataChunk.SetBitmask(x, y, (TerrainLayers)l, (ushort)tileIndex);
                }
            }
            #endregion
        }

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                dataChunk.ClearTilesLayer(l);
            }
        }

        return true;
    }

    bool DeserializeTileDataNetwork (PooledBitReader ms, DataChunk dataChunk) {
        int layerCount = ms.ReadByte();

        for(int ll = 0; ll < layerCount; ll++) {
            int l = ms.ReadByte();
            if(l < 0) {
                return false;
            }

            bool isLong = false;
            int tileIndex = 0;
            int lenght = 0;
            for(int y = 0; y < dataChunk.chunkSize; y++) {
                bool inv = (y % 2 == 0);
                for(int x = inv ? 0 : (dataChunk.chunkSize - 1); inv ? (x < dataChunk.chunkSize) : (x >= 0); x = x + (inv ? 1 : -1)) {
                    // RLE -- The run has ended. The next batch must be decoded
                    if(lenght == 0) {
                        isLong = ms.ReadBit();
                        //isLong = ms.ReadByte() == 1;

                        if(isLong) {
                            lenght = ms.ReadByte();
                            tileIndex = ms.ReadByte();
                        } else {
                            tileIndex = ms.ReadByte();
                            lenght = 1;
                        }
                    }
                    lenght--;

                    if(tileIndex <= 0) {
                        dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                    } else {
                        if((tileIndex - 1) >= dataChunk.globalIDPalette.Count) {
                            Debug.LogError("A tile has a palette index greater or equal to the palette length.");
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        } else if(dataChunk.globalIDPalette[tileIndex - 1] != -1) {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, dataChunk.globalIDPalette[tileIndex - 1]);
                        } else {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        }
                    }
                }
            }
        }

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                dataChunk.ClearTilesLayer(l);
            }
        }

        return true;
    }
    #endregion

    #region MobileChunkInfos
    void SerializeMobileInfos (BitWriter ms, MobileDataChunk mobileDataChunk) {
        //Position (x, y, z)
        //Collider (offset.x, offset.y, size.x, size.y)
        //Restricted size (x, y)

        ms.WriteSingle(mobileDataChunk.mobileChunk.transform.position.x);
        ms.WriteSingle(mobileDataChunk.mobileChunk.transform.position.y);
        ms.WriteSingle(mobileDataChunk.mobileChunk.transform.position.z);

        ms.WriteSingle(mobileDataChunk.mobileChunk.boxCollider.offset.x);
        ms.WriteSingle(mobileDataChunk.mobileChunk.boxCollider.offset.y);
        ms.WriteSingle(mobileDataChunk.mobileChunk.boxCollider.size.x);
        ms.WriteSingle(mobileDataChunk.mobileChunk.boxCollider.size.y);

        ms.WriteSingle(mobileDataChunk.restrictedSize.x);
        ms.WriteSingle(mobileDataChunk.restrictedSize.y);

        ms.WriteSingle(mobileDataChunk.mobileChunk.rigidbody.velocity.x);
        ms.WriteSingle(mobileDataChunk.mobileChunk.rigidbody.velocity.y);
    }

    void DeserializeMobileInfos (BitReader ms, MobileDataChunk mobileDataChunk) {
        //Position (x, y, z)
        //Collider (offset.x, offset.y, size.x, size.y)
        //Restricted size (x, y)

        mobileDataChunk.mobileChunk.transform.position = new Vector3(ms.ReadSingle(), ms.ReadSingle(), ms.ReadSingle());
        mobileDataChunk.mobileChunk.position = mobileDataChunk.mobileChunk.transform.position;
        mobileDataChunk.mobileChunk.previousPosition = mobileDataChunk.mobileChunk.position;

        mobileDataChunk.mobileChunk.boxCollider.offset = new Vector2(ms.ReadSingle(), ms.ReadSingle());
        mobileDataChunk.mobileChunk.boxCollider.size = new Vector2(ms.ReadSingle(), ms.ReadSingle());
        mobileDataChunk.restrictedSize = new Vector2Int(ms.ReadInt32(), ms.ReadInt32());
        mobileDataChunk.mobileChunk.rigidbody.velocity = new Vector2(ms.ReadSingle(), ms.ReadSingle());
    }
    #endregion

    #region Byte Buffer Pool
    public byte[] GetByteBuffer () {
        if(bufferPool.TryDequeue(out byte[] result)) {
            return result;
        } else {
            return new byte[bufferSize];
        }
    }

    public void ReturnByteBuffer (byte[] buffer) {
        bufferPool.Enqueue(buffer);
    }
    #endregion
}
