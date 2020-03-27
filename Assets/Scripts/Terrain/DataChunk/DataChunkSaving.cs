using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;

public class DataChunkSaving : MonoBehaviour {

    #region Header and Initiation
    [Header("Variables")]
    public string saveName = "New Save";
    public string saveFolderName = "new_save";
    public int dimension { get; private set; }
    public static DataChunkSaving inst;

    // Const
    public const string savesFolder = "saves";
    public const string chunkDataFolder = "chunk_data";
    public static readonly string[] dimensions = {"overworld"};
    const string chunkFileEnd = ".cdat";
    const string chunkFileSeparator = "_";
    const string tileStringSeparator = ":";
    const int bufferSize = 8192;
    const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_:";

    public const string mobileChunkDataFolder = "mobile_chunk_data";
    public const string mobileChunkDataEnd = ".mdat";

    // Privates
    char s; // Separator char
    string datapath;
    string chunkDatapath;
    string mobileChunkDatapath;
    StringBuilder sb;
    Dictionary<char, byte> charToByte;

    // Shared Ressources
    static byte[] buffer = new byte[bufferSize];

    private void Awake () {
        if(inst == null) {
            inst = this;
        }

        s = Path.DirectorySeparatorChar;
        datapath = Application.persistentDataPath;
        sb = new StringBuilder();

        ComposeDataPaths();
        
        charToByte = new Dictionary<char, byte>();
        char[] authorizedChars = authorizedCharsString.ToCharArray();
        for(byte i = 0; i < authorizedChars.Length; i++) {
            charToByte.Add(authorizedChars[i], i);
        }
    }
    #endregion

    #region Datapaths
    public void SetDimension (int dimension) {
        this.dimension = dimension;
        ComposeDataPaths();
    }

    void ComposeDataPaths () {
        mobileChunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + mobileChunkDataFolder + s;

        if(!Directory.Exists(mobileChunkDatapath)) {
            Directory.CreateDirectory(mobileChunkDatapath);
        }

        chunkDatapath = datapath + s + savesFolder + s + saveFolderName + s + chunkDataFolder + s + dimensions[dimension] + s;

        if(!Directory.Exists(chunkDatapath)) {
            Directory.CreateDirectory(chunkDatapath);
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
    #endregion

    #region Surface Save Functions
    public void SaveChunk (DataChunk source) {
        //Serialize and save.
        using(FileStream fs = new FileStream(GetChunkDirectory(source), FileMode.Create)) {
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress)) {
                using(MemoryStream ms = new MemoryStream(buffer)) {
                    using(BinaryWriter bw = new BinaryWriter(ms)) {
                        SerializeToStream(bw, source);
                        defs.Write(buffer, 0, (int)ms.Position);
                    }
                }
            }
        }
    }

    public void SaveChunk (MobileDataChunk source) {
        //Serialize and save.
        using(FileStream fs = new FileStream(GetMobileChunkDirectory(source), FileMode.Create)) {
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Compress)) {
                using(MemoryStream ms = new MemoryStream(buffer)) {
                    using(BinaryWriter bw = new BinaryWriter(ms)) {
                        SerializeToStream(bw, source);
                        defs.Write(buffer, 0, (int)ms.Position);
                    }
                }
            }
        }
    }

    public bool LoadChunk (DataChunk destination) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = GetChunkDirectory(destination);
        if(!File.Exists(chunkPath)) {
            return false;
        }
        bool hasSucceded;
        using(FileStream fs = new FileStream(chunkPath, FileMode.Open)) {
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress)) {
                using(MemoryStream ms = new MemoryStream(buffer)) {
                    using(BinaryReader br = new BinaryReader(ms)) {
                        int rlength = defs.Read(buffer, 0, buffer.Length);
                        hasSucceded = DeserializeFromStream(br, destination);
                    }
                }
            }
        }
        if(!hasSucceded) {
            Debug.LogError($"A chunk at {destination.chunkPosition} failed to be deserialized. Its file was deleted.");
            File.Delete(chunkPath);
        }
        return hasSucceded;
    }

    public bool LoadChunk (MobileDataChunk destination) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
        string chunkPath = GetMobileChunkDirectory(destination);
        if(!File.Exists(chunkPath)) {
            return false;
        }
        bool hasSucceded;
        using(FileStream fs = new FileStream(chunkPath, FileMode.Open)) {
            using(DeflateStream defs = new DeflateStream(fs, CompressionMode.Decompress)) {
                using(MemoryStream ms = new MemoryStream(buffer)) {
                    using(BinaryReader br = new BinaryReader(ms)) {
                        int rlength = defs.Read(buffer, 0, buffer.Length);
                        hasSucceded = DeserializeFromStream(br, destination);
                    }
                }
            }
        }
        if(!hasSucceded) {
            Debug.LogError($"A mobile chunk with uid {destination.mobileChunk.uid} failed to be deserialized. Its file was deleted.");
            File.Delete(chunkPath);
        }
        return hasSucceded;
    }

    public void DeleteMobileChunk (MobileDataChunk target) {
        string chunkPath = GetMobileChunkDirectory(target);
        File.Delete(chunkPath);
    }
    #endregion


    #region Stream Serialization
    void SerializeToStream (BinaryWriter ms, DataChunk dataChunk) {
        SerializePalette(ms, dataChunk);

        SerializeTileData(ms, dataChunk);
    }

    void SerializeToStream (BinaryWriter ms, MobileDataChunk mobileDataChunk) {
        SerializeMobileInfos(ms, mobileDataChunk);

        SerializePalette(ms, mobileDataChunk);

        SerializeTileData(ms, mobileDataChunk);
    }

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
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    // If the index is not found (is air), it'll be -1. To get the corrected palette index, add 1.
                    // That will simulate air being in the palette at the index 0.
                    ms.Write((byte)(dataChunk.globalIDPalette.IndexOf(dataChunk.GetGlobalID(x, y, (TerrainLayers)l)) + 1));
                }
            }
        }

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    ms.Write(dataChunk.GetBitmask(x, y, (TerrainLayers)l));
                }
            }
        }
    }

    bool DeserializeTileData (BinaryReader ms, DataChunk dataChunk) {
        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
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
        }

        for(int l = 0; l < TerrainManager.inst.layerParameters.Length; l++) {
            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    dataChunk.SetBitmask(x, y, (TerrainLayers)l, ms.ReadUInt16());
                }
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
