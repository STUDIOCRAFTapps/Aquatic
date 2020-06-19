using System.Collections.Generic;
using UnityEngine;

// Works similarly to the TileCollectionGroup class

    /*
[CreateAssetMenu(fileName = "EntityCollectionGroup", menuName = "Entities/Collections/EntityCollectionGroup")]
public class EntityCollectionGroup : ScriptableObject {
    [Header("Collections")]
    public List<EntityCollection> collections = new List<EntityCollection>();
    [HideInInspector] public Dictionary<string, EntityCollection> collByString;
    [HideInInspector] public List<EntityAsset> entitiesByGlobalID;

    #region Dictionary Building
    public void BuildDictionaries () {
        collByString = new Dictionary<string, EntityCollection>();
        entitiesByGlobalID = new List<EntityAsset>();
        for(int i = 0; i < collections.Count; i++) {
            collections[i].BuildDictionary();
            collByString.Add(collections[i].id, collections[i]);
        }

        int gid = 0;
        foreach(EntityCollection ec in collections) {
            foreach(EntityAsset ea in ec.items) {
                entitiesByGlobalID.Add(ea);
                ea.globalID = gid;
                ea.collection = ec;
                gid++;
            }
        }
    }
    #endregion
    
    public EntityAsset GetTileAssetFromGlobalID (int globalID) {
        return entitiesByGlobalID[globalID];
    }

    public bool GetGlobalIDFromEntityString (EntityString entityString, out int globalID) {
        globalID = -1;
        if(!collByString.ContainsKey(entityString.nspace)) {
            return false;
        }
        if(!collByString[entityString.nspace].entitiesByString.ContainsKey(entityString.id)) {
            return false;
        }
        globalID = collByString[entityString.nspace].entitiesByString[entityString.id].globalID;
        return true;
    }

    public EntityString GetEntityStringFromGlobalID (int globalID) {
        return new EntityString(entitiesByGlobalID[globalID].collection.id, entitiesByGlobalID[globalID].id);
    }
}
*/
