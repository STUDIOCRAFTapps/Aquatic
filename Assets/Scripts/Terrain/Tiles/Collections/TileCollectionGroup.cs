using System.Collections.Generic;
using UnityEngine;

//Global IDs are an runtime alternative to TileStrings.
//By default;
//0 is Empty.

//Once a chunk is loaded, the tilestring of any tile will be retrieve from the palette, using the
//raw tile id as index. Using the tilestring, the global id of the tile can be retraced and stored
//in data chunks (stored in RAM, not on the disk)

//To save global IDs, a tilestring must be found. To find a tilestring, simply find the corresponding
//BaseTile asset (using the global id as an index, by searching in tileAssetDictionary) and get the
//collection and tile's id.

//HELP: SHOULD THIS BE RUNTIME?

[CreateAssetMenu(fileName = "TileCollectionGroup", menuName = "Terrain/Collections/TileCollectionGroup")]
public class TileCollectionGroup : ScriptableObject {
    [Header("Collections")]
    public List<TileCollection> collections = new List<TileCollection>();
    [HideInInspector] public Dictionary<string, TileCollection> collByString;
    [HideInInspector] public List<BaseTileAsset> tileByGlobalID;

    [Header("Texture Dictionary")]
    public int maxTextureWidth = 32;
    public int maxTextureHeight = 32;
    public TextureFormat textureFormat;
    [HideInInspector] public Texture2DArray textures;

    #region Dictionary Building
    public void BuildDictionaries () {
        int textureArraySize = 0;
        collByString = new Dictionary<string, TileCollection>();
        collByString = new Dictionary<string, TileCollection>();
        tileByGlobalID = new List<BaseTileAsset>();
        for(int i = 0; i < collections.Count; i++) {
            collections[i].BuildDictionary();
            collByString.Add(collections[i].id, collections[i]);
            foreach(BaseTileAsset t in collections[i].items) {
                if(t.hasTextures) {
                    textureArraySize += t.textures.Length;
                }
            }
        }

        textures = new Texture2DArray(maxTextureWidth, maxTextureHeight, textureArraySize, textureFormat, false);
        int c = 0;
        int gid = 1;
        Color[] colorBuffer = new Color[maxTextureWidth * maxTextureHeight];
        foreach(TileCollection tc in collections) {
            foreach(BaseTileAsset bta in tc.items) {
                tileByGlobalID.Add(bta);
                bta.globalID = gid;
                bta.collection = tc;
                gid++;

                if(bta.hasTextures) {
                    bta.textureBaseIndex = c;
                    for(int i = 0; i < bta.textures.Length; i++) {
                        if(Mathf.CeilToInt(bta.textures[i].textureRect.width) > maxTextureWidth || Mathf.CeilToInt(bta.textures[i].textureRect.height) > maxTextureHeight) {
                            Debug.LogError("Sprite texture bigger than max texture array size.");
                            continue;
                        }
                        WriteSpriteToColorBuffer(
                            colorBuffer, maxTextureWidth,
                            SpriteToTexture(bta.textures[i]).GetPixels(),
                            Mathf.CeilToInt(bta.textures[i].textureRect.width),
                            Mathf.CeilToInt(bta.textures[i].textureRect.height)
                        );
                        textures.SetPixels(colorBuffer, c);
                        c++;
                    }
                }
            }
        }
        textures.Apply();
        textures.filterMode = FilterMode.Point;
    }

    public static Texture2D SpriteToTexture (Sprite sprite) {
        Texture2D texture = new Texture2D(Mathf.CeilToInt(sprite.textureRect.width), Mathf.CeilToInt(sprite.textureRect.height), TextureFormat.RGBA32, false);
        if(!texture.isReadable) {
            Debug.LogError($"The texture \"{texture.name}\" is not readable.");
        }
        Color[] newColors = sprite.texture.GetPixels(
            Mathf.FloorToInt(sprite.textureRect.x),
            Mathf.FloorToInt(sprite.textureRect.y),
            Mathf.CeilToInt(sprite.textureRect.width),
            Mathf.CeilToInt(sprite.textureRect.height)
        );
        texture.SetPixels(newColors);
        texture.Apply();
        return texture;
    }

    public static void WriteSpriteToColorBuffer (Color[] colorBuffer, int bufferWidth, Color[] sprite, int spriteWidth, int spriteHeight) {
        for(int x = 0; x < spriteWidth; x++) {
            for(int y = 0; y < spriteHeight; y++) {
                colorBuffer[x + y * bufferWidth] = sprite[x + y * spriteWidth];
            }
        }
    }
    #endregion
    
    public BaseTileAsset GetTileAssetFromGlobalID (int globalID) {
        return tileByGlobalID[globalID - 1];
    }

    public bool GetGlobalIDFromTileString (TileString tileString, out int globalID) {
        globalID = -1;
        if(!collByString.ContainsKey(tileString.nspace)) {
            return false;
        }
        if(!collByString[tileString.nspace].tilesByString.ContainsKey(tileString.id)) {
            return false;
        }
        globalID = collByString[tileString.nspace].tilesByString[tileString.id].globalID;
        return true;
    }

    public TileString GetTileStringFromGlobalID (int globalID) {
        if(globalID == 0) {
            Debug.LogError("Global ID must not be 0.");
        }
        return new TileString(tileByGlobalID[globalID - 1].collection.id, tileByGlobalID[globalID - 1].id);
    }
}
