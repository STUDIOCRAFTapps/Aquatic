using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
using System.Linq;

public class ChunkCleaner : MonoBehaviour {
    public string sourceFolder;
    public string destinationReplaceFrom;
    public string destinationReplaceTo;

    // Const
    public const string savesFolder = "saves";
    public const string chunkDataFolder = "chunk_data";
    public static readonly string[] dimensions = { "overworld" };
    const string chunkFileEnd = ".cdat";
    const string chunkFileSeparator = "_";
    const string tileStringSeparator = ":";
    const int bufferSize = 8192;
    const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_:";

    public const string mobileChunkDataFolder = "mobile_chunk_data";
    public const string mobileChunkDataEnd = ".mdat";

    static byte[] buffer = new byte[bufferSize];
    
    StringBuilder sb;
    Dictionary<char, byte> charToByte;
    List<string> layerNames;

    private void Start () {
        sb = new StringBuilder();
        charToByte = new Dictionary<char, byte>();
        char[] authorizedChars = authorizedCharsString.ToCharArray();
        for(byte i = 0; i < authorizedChars.Length; i++) {
            charToByte.Add(authorizedChars[i], i);
        }

        layerNames = Enum.GetNames(typeof(TerrainLayers)).ToList();

        DataChunk dataChunk = new DataChunk();
        string[] allFiles = Directory.GetFiles(sourceFolder);
        
        foreach(string file in allFiles) {
            dataChunk.Init(Vector2Int.zero);
            LoadChunk(dataChunk, file);
            SaveChunk(dataChunk, file.Replace(destinationReplaceFrom, destinationReplaceTo));
        }
    }

    #region Surface Functions
    public bool LoadChunk (DataChunk destination, string chunkPath) {
        // The destination chunk should already have been cleaned and thus, 
        // its position should be correct and it can be passed to the GetChunkDirectory function.
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
        return hasSucceded;
    }

    public void SaveChunk (DataChunk source, string chunkPath) {
        //Serialize and save.
        using(FileStream fs = new FileStream(chunkPath, FileMode.Create)) {
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
    #endregion

    #region De/Serialize to Stream
    void SerializeToStream (BinaryWriter ms, DataChunk dataChunk) {
        SerializePalette(ms, dataChunk);

        SerializeTileData(ms, dataChunk);
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
    #endregion

    #region Palette
    void SerializePalette (BinaryWriter ms, DataChunk dataChunk) {
        ms.Write((byte)dataChunk.globalIDPalette.Count);
        for(int i = 0; i < dataChunk.globalIDPalette.Count; i++) {
            //This shouldn't create much allocations, it just reference already written strings
            TileString tileString = GeneralAsset.inst.GetTileStringFromGlobalID(dataChunk.globalIDPalette[i]);

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
            if(GeneralAsset.inst.GetGlobalIDFromTileString(tileString, out int gID)) {
                dataChunk.globalIDPalette.Add(gID);
            } else {
                Debug.Log(tileString.id);
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
        for(int l = 0; l < 4; l++) {
            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    byte tileInt = ms.ReadByte();

                    if(tileInt == 0) {
                        dataChunk.SetGlobalID(x, y, (TerrainLayers)l, 0);
                    } else {
                        if((tileInt - 1) >= dataChunk.globalIDPalette.Count) {
                            Debug.LogError("A tile has a palette index greater or equal to the palette. " + (tileInt - 1) + ", " + dataChunk.globalIDPalette.Count);
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
        for(int l = 0; l < 4; l++) {
            for(int x = 0; x < dataChunk.chunkSize; x++) {
                for(int y = 0; y < dataChunk.chunkSize; y++) {
                    dataChunk.SetBitmask(x, y, (TerrainLayers)l, ms.ReadUInt16());
                }
            }
        }

        return true;
    }
    #endregion
}
