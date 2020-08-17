using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GeneralAsset", menuName = "General/GeneralAsset")]
public class GeneralAsset : ScriptableObject {
    
    public static GeneralAsset inst;

    #region Parameters
    [Header("All Assets")]
    public NamespaceAssetGroup[] assetGroups;

    [Header("Texture Dictionary")]
    public int maxTextureWidth = 32;
    public int maxTextureHeight = 32;
    public TextureFormat textureFormat;
    [HideInInspector] public Texture2DArray textures;
    #endregion

    #region Dictionairies and Building
    [HideInInspector] public Dictionary<string, NamespaceAssetGroup> namespaceByString;

    List<BaseTileAsset> tileByGlobalID;
    List<EntityAsset> entitiesByGlobalID;
    List<BaseWeapon> weaponsByGlobalID;
    List<BaseToolAsset> toolsByGlobalID;
    List<BaseBrushAsset> brushesByGlobalID;
    
    public void Build () {
        namespaceByString = new Dictionary<string, NamespaceAssetGroup>();
        foreach(NamespaceAssetGroup nag in assetGroups) {
            namespaceByString.Add(nag.id, nag);
            nag.BuildAllCollections();
        }

        tileByGlobalID = new List<BaseTileAsset>();
        entitiesByGlobalID = new List<EntityAsset>();
        weaponsByGlobalID = new List<BaseWeapon>();
        toolsByGlobalID = new List<BaseToolAsset>();
        brushesByGlobalID = new List<BaseBrushAsset>();

        BuildTiles();
        BuildEntities();
        BuildWeapons();
        BuildTools();
        BuildBrushes();
    }

    public void BuildTiles () {
        // Checking how many textures there is. (Note: There should be both 16x16 and 32x32 materials implemented later)
        int textureArraySize = 0;
        foreach(NamespaceAssetGroup nag in assetGroups)
        foreach(TileCollection tc in nag.tileCollections)
        foreach(BaseTileAsset bta in tc.items) {
            if(bta.hasTextures) {
                textureArraySize += bta.textures.Length;
            }
        }

        // Building the texture array. (Note: There should be both 16x16 and 32x32 materials implemented later)
        textures = new Texture2DArray(maxTextureWidth, maxTextureHeight, textureArraySize, textureFormat, false);
        int c = 0;
        int gid = 1;
        Color[] colorBuffer = new Color[maxTextureWidth * maxTextureHeight];
        foreach(NamespaceAssetGroup nag in assetGroups)
        foreach(TileCollection tc in nag.tileCollections) {
            tc.parent = nag;
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

    public void BuildEntities () {
        int gid = 0;
        foreach(NamespaceAssetGroup nag in assetGroups)
        foreach(EntityCollection ec in nag.entityCollections) {
            ec.parent = nag;
            foreach(EntityAsset ea in ec.items) {
                ea.globalID = gid;
                ea.collection = ec;
                entitiesByGlobalID.Add(ea);
                gid++;
            }
        }
    }

    public void BuildWeapons () {
        int gid = 0;
        foreach(NamespaceAssetGroup nag in assetGroups)
        foreach(WeaponCollection wc in nag.weaponCollections)
        foreach(BaseWeapon bw in wc.items) {
            bw.gid = gid;
            weaponsByGlobalID.Add(bw);
            gid++;

        }
    }

    public void BuildTools () {
        int gid = 0;
        foreach(NamespaceAssetGroup nag in assetGroups)
        foreach(BaseToolAsset bta in nag.tools) {
            bta.gid = gid;
            toolsByGlobalID.Add(bta);
            gid++;

        }
    }

    public void BuildBrushes () {
        int gid = 0;
        foreach(NamespaceAssetGroup nag in assetGroups)
            foreach(BaseBrushAsset bba in nag.brushes) {
                bba.gid = gid;
                brushesByGlobalID.Add(bba);
                gid++;

            }
    }
    #endregion

    #region Utilities
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

    #region Access Assets
    #region Tiles
    public BaseTileAsset GetTileAssetFromGlobalID (int globalID) {
        return tileByGlobalID[globalID - 1];
    }

    public bool GetGlobalIDFromTileString (TileString tileString, out int globalID) {
        globalID = -1;
        if(!namespaceByString.ContainsKey(tileString.nspace)) {
            return false;
        }
        if(!namespaceByString[tileString.nspace].tilesByString.ContainsKey(tileString.id)) {
            return false;
        }
        globalID = namespaceByString[tileString.nspace].tilesByString[tileString.id].globalID;
        return true;
    }

    public TileString GetTileStringFromGlobalID (int globalID) {
        if(globalID == 0) {
            Debug.LogError("Global ID must not be 0.");
        }
        return new TileString(tileByGlobalID[globalID - 1].collection.parent.id, tileByGlobalID[globalID - 1].id);
    }

    public int GetTileCount () {
        return tileByGlobalID.Count;
    }
    #endregion

    #region Entities
    public EntityAsset GetEntityAssetFromGlobalID (int globalID) {
        return entitiesByGlobalID[globalID];
    }

    public bool GetGlobalIDFromEntityString (EntityString entityString, out int globalID) {
        globalID = -1;
        if(!namespaceByString.ContainsKey(entityString.nspace)) {
            return false;
        }
        if(!namespaceByString[entityString.nspace].entitiesByString.ContainsKey(entityString.id)) {
            return false;
        }
        globalID = namespaceByString[entityString.nspace].entitiesByString[entityString.id].globalID;
        return true;
    }

    public EntityString GetEntityStringFromGlobalID (int globalID) {
        return new EntityString(entitiesByGlobalID[globalID].collection.parent.id, entitiesByGlobalID[globalID].id);
    }

    public int GetEntityCount () {
        return entitiesByGlobalID.Count;
    }
    #endregion

    #region Weapons
    public BaseWeapon GetWeaponAssetFromGlobalID (int globalID) {
        return weaponsByGlobalID[globalID];
    }

    public int GetWeaponCount () {
        return weaponsByGlobalID.Count;
    }
    #endregion

    #region Tools & Brushes
    public BaseToolAsset GetToolAssetFromGlobalID (int globalID) {
        return toolsByGlobalID[globalID];
    }

    public BaseBrushAsset GetBrushFromGlobalID (int globalID) {
        return brushesByGlobalID[globalID];
    }
    #endregion
    #endregion

    // Build global texture ressources here
    // Build global id collection here
    // All tile asset and other searching stuff needs to be done here
}

[System.Serializable]
public class NamespaceAssetGroup {
    public string name;
    public string id;
    public string description;

    public TileCollection[] tileCollections;
    public EntityCollection[] entityCollections;
    public WeaponCollection[] weaponCollections;
    public BaseToolAsset[] tools;
    public BaseBrushAsset[] brushes;

   [HideInInspector] public Dictionary<string, BaseTileAsset> tilesByString;
   [HideInInspector] public Dictionary<string, EntityAsset> entitiesByString;
   [HideInInspector] public Dictionary<string, BaseWeapon> weaponsByString;

    public void BuildAllCollections () {
        tilesByString = new Dictionary<string, BaseTileAsset>();
        entitiesByString = new Dictionary<string, EntityAsset>();
        weaponsByString = new Dictionary<string, BaseWeapon>();

        foreach(TileCollection tc in tileCollections) {
            tc.parent = this;

            tc.BuildDictionary();
            foreach(BaseTileAsset bta in tc.items) {
                if(tilesByString.ContainsKey(bta.id)) {
                    Debug.LogError($"Namespace {id} already contains a tile with the id \"{bta.id}\"");
                } else {
                    bta.collection = tc;
                    tilesByString.Add(bta.id, bta);
                }
            }
        }
        foreach(EntityCollection ec in entityCollections) {
            ec.BuildDictionary();
            foreach(EntityAsset ea in ec.items) {
                if(entitiesByString.ContainsKey(ea.id)) {
                    Debug.LogError($"Namespace {id} already contains a tile with the id \"{ea.id}\"");
                } else {
                    ea.collection = ec;
                    entitiesByString.Add(ea.id, ea);
                }
            }
        }
        foreach(WeaponCollection wc in weaponCollections) {
            wc.BuildDictionary();
            foreach(BaseWeapon bw in wc.items) {
                if(entitiesByString.ContainsKey(bw.id)) {
                    Debug.LogError($"Namespace {id} already contains a tile with the id \"{bw.id}\"");
                } else {
                    //bw.collection = wc;
                    weaponsByString.Add(bw.id, bw);
                }
            }
        }
    }

    // TODO;
    // - Fixed Particle Assets
    // - Attack Strike Assets
}



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