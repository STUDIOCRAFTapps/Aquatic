using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
using System.Linq;
using Newtonsoft.Json;

public class WorldSaving : MonoBehaviour {

    #region Header and Initiation
    [Header("Variables")]
    public string saveName = "New Save";
    public string saveFolderName = "new_save";
    public int dimension { get; private set; }
    public static WorldSaving inst;

    // Const
    public const int bufferSize = 8192;
    public const string savesFolder = "saves";
    public const string editFolder = "edit";
    public const string playFolder = "play";
    public static readonly string[] dimentions = {"overworld"};

    public const string chunkDataFolder = "chunk_data";
    public const string regionFolder = "entity_regions";
    public const string mobileChunkDataFolder = "mobile_chunk_data";
    public const string entityDataFolder = "entity_data";

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
    string mobileChunkDatapath;
    string entityDatapath;
    string regionsDatapath;

    StringBuilder sb;
    Dictionary<char, byte> charToByte;
    List<string> layerNames;
    JsonSerializerSettings jss;

    // Shared Ressources
    static byte[] buffer = new byte[bufferSize];

    private void Awake () {
        if(inst == null) {
            inst = this;
        }

        s = Path.DirectorySeparatorChar;
        datapath = Application.persistentDataPath;
        sb = new StringBuilder();
        jss = new JsonSerializerSettings() {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        ComposeDataPaths();
        
        charToByte = new Dictionary<char, byte>();
        char[] authorizedChars = authorizedCharsString.ToCharArray();
        for(byte i = 0; i < authorizedChars.Length; i++) {
            charToByte.Add(authorizedChars[i], i);
        }

        layerNames = Enum.GetNames(typeof(TerrainLayers)).ToList();

        GameManager.inst.OnChangeEngineMode += OnChangeEngineMode;
    }

    private void OnDestroy () {
        GameManager.inst.OnChangeEngineMode -= OnChangeEngineMode;
    }

    public void OnChangeEngineMode () {
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
        string engmf = (GameManager.inst.engineMode == EngineModes.Edit) ? editFolder : playFolder;

        mobileChunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + mobileChunkDataFolder + s;
        if(!Directory.Exists(mobileChunkDatapath)) {
            Debug.Log(mobileChunkDatapath);
            Directory.CreateDirectory(mobileChunkDatapath);
        }

        chunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + chunkDataFolder + s;
        if(!Directory.Exists(chunkDatapath)) {
            Directory.CreateDirectory(chunkDatapath);
        }

        entityDatapath = datapath + s + savesFolder + s + saveFolderName + s + engmf + s + dimentions[dimension] + s + entityDataFolder + s;
        if(!Directory.Exists(entityDatapath)) {
            Directory.CreateDirectory(entityDatapath);
        }

        regionsDatapath = datapath + s + savesFolder + s + inst.saveFolderName + s + engmf + s + dimentions[dimension] + s + regionFolder + s;
        if(!Directory.Exists(regionsDatapath)) {
            Directory.CreateDirectory(regionsDatapath);
        }
    }

    public string GetChunkDirectory (DataChunk dataChunk) {
        sb.Clear();
        sb.Append(chunkDatapath);
        sb.Append(dataChunk.chunkPosition.x);
        sb.Append(chunkFileSeparator);
        sb.Append(dataChunk.chunkPosition.y);
        sb.Append(chunkFileEnd);

        return sb.ToString();
    }

    public string GetMobileChunkDirectory (MobileDataChunk mobileDataChunk) {
        sb.Clear();
        sb.Append(mobileChunkDatapath);
        sb.Append(mobileDataChunk.mobileChunk.uid);
        sb.Append(mobileChunkDataEnd);

        return sb.ToString();
    }

    public string GetEntityDirectory (int uid) {
        sb.Clear();
        sb.Append(entityDatapath);
        sb.Append(uid);
        sb.Append(entityDataEnd);

        return sb.ToString();
    }

    public string GetRegionDirectory (EntityRegion entityRegion) {
        sb.Clear();
        sb.Append(regionsDatapath);
        sb.Append(entityRegion.regionPosition.x);
        sb.Append(regionFileSeparator);
        sb.Append(entityRegion.regionPosition.y);
        sb.Append(regionFileEnd);

        return sb.ToString();
    }
    #endregion

    #region Front-End Functions
    #region Save
    public void SaveChunk (DataChunk source) {
        //Serialize and save.
        using(FileStream fs = new FileStream(GetChunkDirectory(source), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(MemoryStream ms = new MemoryStream(buffer))
        using(BinaryWriter bw = new BinaryWriter(ms)) {
            SerializeToStream(bw, source);
            defs.Write(buffer, 0, (int)ms.Position);
        }
    }

    public void SaveChunk (MobileDataChunk source) {
        //Serialize and save.
        using(FileStream fs = new FileStream(GetMobileChunkDirectory(source), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(MemoryStream ms = new MemoryStream(buffer))
        using(BinaryWriter bw = new BinaryWriter(ms)) {
            SerializeToStream(bw, source);
            defs.Write(buffer, 0, (int)ms.Position);
        }
    }

    public void SaveEntity (Entity entity) {
        //Serialize and save.
        using(FileStream fs = new FileStream(GetEntityDirectory(entity.entityData.uid), FileMode.Create))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress))
        using(StreamWriter sw = new StreamWriter(defs))
        using(JsonWriter jw = new JsonTextWriter(sw)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);

            serializer.Serialize(jw, new EntityDataWrapper(entity.entityData));
        }
    }

    public void SaveRegionFile (EntityRegion entityRegion) {
        using(FileStream fs = new FileStream(GetRegionDirectory(entityRegion), FileMode.Create)) {
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress)) {
                using(MemoryStream ms = new MemoryStream(buffer)) {
                    using(BinaryWriter bw = new BinaryWriter(ms)) {
                        SerializeRegionToStream(bw, entityRegion);
                        defs.Write(buffer, 0, (int)ms.Position);
                    }
                }
            }
        }
    }
    #endregion

    #region Load
    public bool LoadChunk (DataChunk dataChunk) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = GetChunkDirectory(dataChunk);
        if(!File.Exists(chunkPath)) {
            return false;
        }

        bool hasSucceded;
        try {
            using(FileStream fs = new FileStream(chunkPath, FileMode.Open))
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
            using(MemoryStream ms = new MemoryStream(buffer))
            using(BinaryReader br = new BinaryReader(ms)) {
                int rlength = defs.Read(buffer, 0, buffer.Length);
                hasSucceded = DeserializeFromStream(br, dataChunk);
            }
        } catch(Exception e) {
            Debug.Log("Failed to load chunk: " + e.ToString());
            hasSucceded = false;
        }

        if(!hasSucceded) {
            Debug.LogError($"A chunk at {dataChunk.chunkPosition} failed to be deserialized. Its file was deleted.");
            File.Delete(chunkPath);
        }
        return hasSucceded;
    }

    public bool LoadChunk (MobileDataChunk dataChunk) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = GetMobileChunkDirectory(dataChunk);
        if(!File.Exists(chunkPath)) {
            return false;
        }
        bool hasSucceded;
        using(FileStream fs = new FileStream(chunkPath, FileMode.Open))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
        using(MemoryStream ms = new MemoryStream(buffer))
        using(BinaryReader br = new BinaryReader(ms)) {
            int rlength = defs.Read(buffer, 0, buffer.Length);
            hasSucceded = DeserializeFromStream(br, dataChunk);
        }

        if(!hasSucceded) {
            Debug.LogError($"A mobile chunk with uid {dataChunk.mobileChunk.uid} failed to be deserialized. Its file was deleted.");
            File.Delete(chunkPath);
        }
        return hasSucceded;
    }

    public bool LoadEntity (int uid, out EntityData entityData) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string entityPath = GetEntityDirectory(uid);
        if(!File.Exists(entityPath)) {
            entityData = null;
            return false;
        }
        bool hasSucceded;
        using(FileStream fs = new FileStream(entityPath, FileMode.Open))
        using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
        using(StreamReader sr = new StreamReader(defs))
        using(JsonReader jr = new JsonTextReader(sr)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);

            entityData = serializer.Deserialize<EntityDataWrapper>(jr).entityData;
            hasSucceded = true;
        }

        if(!hasSucceded) {
            Debug.LogError($"An entity with uid {uid} failed to be deserialized. Its file was deleted.");
            File.Delete(entityPath);
        }
        return hasSucceded;
    }

    public bool LoadRegionFile (EntityRegion entityRegion) {
        string regionPath = GetRegionDirectory(entityRegion);
        if(!File.Exists(regionPath)) {
            return false;
        }

        bool hasSucceded;
        try {
            using(FileStream fs = new FileStream(regionPath, FileMode.Open))
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress))
            using(MemoryStream ms = new MemoryStream(buffer))
            using(BinaryReader br = new BinaryReader(ms)) {
                int rlength = defs.Read(buffer, 0, buffer.Length);
                hasSucceded = DeserializeStreamToRegion(br, entityRegion);
            }
        } catch(System.Exception e) {
            Debug.Log("Failed to load chunk: " + e.ToString());
            hasSucceded = false;
        }

        if(!hasSucceded) {
            Debug.LogError($"A region at {entityRegion.regionPosition} failed to be deserialized. Its file was deleted.");
            File.Delete(regionPath);
        }
        return hasSucceded;
    }
    #endregion

    #region Delete
    public void DeleteChunk (DataChunk target) {
        string chunkPath = GetChunkDirectory(target);
        if(File.Exists(chunkPath)) {
            File.Delete(chunkPath);
        }

    }

    public void DeleteMobileChunk (MobileDataChunk target) {
        string chunkPath = GetMobileChunkDirectory(target);
        if(File.Exists(chunkPath)) {
            File.Delete(chunkPath);
        }
        
    }

    public void DeleteEntity (Entity entity) {
        string entityPath = GetEntityDirectory(entity.entityData.uid);
        if(File.Exists(entityPath)) {
            File.Delete(entityPath);
        }

    }
    #endregion
    #endregion


    #region Stream Functions
    #region SerializeToStream
    void SerializeToStream (BinaryWriter ms, DataChunk dataChunk) {
        SerializePalette(ms, dataChunk);

        SerializeTileData(ms, dataChunk);
    }

    void SerializeToStream (BinaryWriter ms, MobileDataChunk mobileDataChunk) {
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
                for(int i = 0; i < entityRegion.subRegions[x][y].entitiesUIDs.Count; i++) {
                    ms.Write(entityRegion.subRegions[x][y].entitiesUIDs[i]);
                }
            }
        }
    }
    #endregion

    #region DeserializeFromStream
    bool DeserializeFromStream (BinaryReader ms, DataChunk dataChunk) {
        if(!DeserializePalette(ms, dataChunk)) {
            return false;
        }

        if(!DeserializeTileData(ms, dataChunk)) {
            return false;
        }

        CleanPalette(dataChunk);

        return true;
    }

    bool DeserializeFromStream (BinaryReader ms, MobileDataChunk mobileDataChunk) {
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
                    entityRegion.subRegions[x][y].entitiesUIDs.Add(ms.ReadInt32());
                }
            }
        }

        return true;
    }
    #endregion
    #endregion

    #region Palette
    void SerializePalette (BinaryWriter ms, DataChunk dataChunk) {
        ms.Write((byte)dataChunk.globalIDPalette.Count);
        for(int i = 0; i < dataChunk.globalIDPalette.Count; i++) {
            //This shouldn't create much allocations, it just reference already written strings
            TileString tileString = TerrainManager.inst.tiles.GetTileStringFromGlobalID(dataChunk.globalIDPalette[i]);

            //Mark the length
            ms.Write((byte)(tileString.nspace.Length + tileString.id.Length + 1));

            //Zero allocation method to write the string to the stream; using a reference array (char -> byte).
            for(int l = 0; l < tileString.nspace.Length; l++) {
                ms.Write(charToByte[tileString.nspace[l]]);
            }
            ms.Write(charToByte[':']);
            for(int l = 0; l < tileString.id.Length; l++) {
                ms.Write(charToByte[tileString.id[l]]);
            }
        }
    }

    bool DeserializePalette (BinaryReader ms, DataChunk dataChunk) {
        int palCount = ms.ReadByte();
        if(palCount == -1) {
            return false;
        }

        TileString tileString = new TileString();

        for(int i = 0; i < palCount; i++) {
            byte readLength = ms.ReadByte();
            int readIndex = 0;
            byte readByte;
            sb.Clear();

            //Read the namespace
            for(; readIndex < readLength; readIndex++) {
                readByte = ms.ReadByte();
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
            if(TerrainManager.inst.tiles.GetGlobalIDFromTileString(tileString, out int gID)) {
                dataChunk.globalIDPalette.Add(gID);
            } else {
                dataChunk.globalIDPalette.Add(-1);
            }
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
    void SerializeTileData (BinaryWriter ms, DataChunk dataChunk) {
        int layerCount = 0;
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                layerCount++;
            }
        }

        ms.Write((byte)layerCount);

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            if(!dataChunk.HasLayerBeenEdited((TerrainLayers)l)) {
                continue;
            }
            ms.Write(layerNames[l]);

            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    // If the index is not found (is air), it'll be -1. To get the corrected palette index, add 1.
                    // That will simulate air being in the palette at the index 0.
                    ms.Write((byte)(dataChunk.globalIDPalette.IndexOf(dataChunk.GetGlobalID(x, y, (TerrainLayers)l)) + 1));
                }
            }

            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    ms.Write(dataChunk.GetBitmask(x, y, (TerrainLayers)l));
                }
            }
        }
    }

    bool DeserializeTileData (BinaryReader ms, DataChunk dataChunk) {
        int layerCount = ms.ReadByte();

        for(int ll = 0; ll < layerCount; ll++) {
            string layerName = ms.ReadString();
            int l = layerNames.IndexOf(layerName);
            if(l < 0) {
                return false;
            }

            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    byte tileInt = ms.ReadByte();

                    if(tileInt == 0) {
                        dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                    } else {
                        if((tileInt - 1) >= dataChunk.globalIDPalette.Count) {
                            Debug.LogError("A tile has a palette index greater or equal to the palette.");
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        } else if(dataChunk.globalIDPalette[tileInt - 1] != -1) {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, dataChunk.globalIDPalette[tileInt - 1]);
                        } else {
                            dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                        }
                    }
                }
            }

            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    dataChunk.SetBitmask(x, y, (TerrainLayers)l, ms.ReadUInt16());
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
    void SerializeMobileInfos (BinaryWriter ms, MobileDataChunk mobileDataChunk) {
        //Position (x, y, z)
        //Collider (offset.x, offset.y, size.x, size.y)
        //Restricted size (x, y)

        ms.Write(mobileDataChunk.mobileChunk.transform.position.x);
        ms.Write(mobileDataChunk.mobileChunk.transform.position.y);
        ms.Write(mobileDataChunk.mobileChunk.transform.position.z);

        ms.Write(mobileDataChunk.mobileChunk.boxCollider.offset.x);
        ms.Write(mobileDataChunk.mobileChunk.boxCollider.offset.y);
        ms.Write(mobileDataChunk.mobileChunk.boxCollider.size.x);
        ms.Write(mobileDataChunk.mobileChunk.boxCollider.size.y);

        ms.Write(mobileDataChunk.restrictedSize.x);
        ms.Write(mobileDataChunk.restrictedSize.y);

        ms.Write(mobileDataChunk.mobileChunk.rigidbody.velocity.x);
        ms.Write(mobileDataChunk.mobileChunk.rigidbody.velocity.y);
    }

    void DeserializeMobileInfos (BinaryReader ms, MobileDataChunk mobileDataChunk) {
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
}
