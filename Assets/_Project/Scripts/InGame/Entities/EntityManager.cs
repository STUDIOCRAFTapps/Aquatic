using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class EntityManager : MonoBehaviour {
    
    public float longUpdateFrequency = 0.1f;

    public List<Entity> allLoadedEntities;
    public Dictionary<int, Queue<Entity>> unusedEntities;
    public Dictionary<int, Entity> entitiesByUID;
    public Queue<Entity> terminationRequests;

    public static EntityManager inst;

    int basicUID = 0;

    private void Awake () {
        inst = this;

        allLoadedEntities = new List<Entity>();
        unusedEntities = new Dictionary<int, Queue<Entity>>();
        entitiesByUID = new Dictionary<int, Entity>();
        terminationRequests = new Queue<Entity>();

        basicUID = PlayerPrefs.GetInt("entityUID", 0);
    }

    public void Update () {
        if(GameManager.inst.engineMode != EngineModes.Play) {
            return;
        }
        foreach(Entity entity in allLoadedEntities) {
            if(Time.unscaledTime - entity.timeOfLastLongUpdate >= longUpdateFrequency) {
                entity.timeOfLastLongUpdate = Time.unscaledTime;
                entity.OnLongUpdate();
            }
        }
    }

    public void LateUpdate () {
        while(terminationRequests.Count > 0) {
            Internal_Kill(terminationRequests.Dequeue());
        }
    }

    public void ExecuteOverlapsEntity (Collider2D coll, Action<Entity> action) {
        for(int i = allLoadedEntities.Count - 1; i >= 0; i--) {
            Entity target = allLoadedEntities[i];
            IInteractableEntity interactEntity = target as IInteractableEntity;
            if(interactEntity != null) {
                if(interactEntity.OnCheckInteractWithCollider(coll)) {
                    action(target);
                }
            }
        }
    }

    public float RaycastEntities (Ray2D ray) {
        float minDistance = Mathf.Infinity;
        foreach(Entity target in allLoadedEntities) {
            IInteractableEntity interactEntity = target as IInteractableEntity;
            if(interactEntity != null) {
                float dist = interactEntity.OnCheckInteractWithRay(ray);
                if(dist < minDistance) {
                    minDistance = dist;
                }
            }
        }
        return minDistance;
    }

    public Entity Spawn (Vector2 position, EntityString entity) {
        return Spawn(position, GeneralAsset.inst.namespaceByString[entity.nspace].entitiesByString[entity.id].globalID);
    }

    public Entity Spawn (Vector2 position, int gid) {
        Entity entity = null;
        
        if(unusedEntities.ContainsKey(gid)) {
            if(unusedEntities[gid].Count > 0) {
                entity = unusedEntities[gid].Dequeue();
                entity.gameObject.SetActive(true);
            } 
        }
        if(entity == null) {
            entity = Instantiate(GeneralAsset.inst.GetEntityAssetFromGlobalID(gid).prefab, transform);
        }

        Type dataType = entity.GetDataType();
        EntityData entityData = (EntityData)Activator.CreateInstance(dataType);
        entityData.position = position;
        entityData.uid = basicUID;
        entityData.assetReference = GeneralAsset.inst.GetEntityStringFromGlobalID(gid);
        entity.LoadData(entityData);

        entitiesByUID.Add(entityData.uid, entity);
        allLoadedEntities.Add(entity);

        EntityRegionManager.inst.AddEntity(entity);

        basicUID++;
        PlayerPrefs.SetInt("entityUID", basicUID);
        Debug.Log("Change shitty uid system, you know what you know how");

        return entity;
    }

    public void Kill (Entity entity) {
        terminationRequests.Enqueue(entity);
    }

    void Internal_Kill (Entity entity) {
        EntityRegionManager.inst.RemoveEntity(entity);

        entitiesByUID.Remove(entity.entityData.uid);
        allLoadedEntities.Remove(entity);

        if(!unusedEntities.ContainsKey(entity.asset.globalID)) {
            unusedEntities.Add(entity.asset.globalID, new Queue<Entity>());
        }
        entity.gameObject.SetActive(false);
        unusedEntities[entity.asset.globalID].Enqueue(entity);

        WorldSaving.inst.DeleteEntity(entity);
    }

    public void UnloadEntity (Entity entity, bool removeFromRegions = false, bool save = true) {
        if(save) {
            SaveEntity(entity);
        }

        if(removeFromRegions) {
            EntityRegionManager.inst.RemoveEntity(entity);
        }

        entitiesByUID.Remove(entity.entityData.uid);
        allLoadedEntities.Remove(entity);

        if(!unusedEntities.ContainsKey(entity.asset.globalID)) {
            unusedEntities.Add(entity.asset.globalID, new Queue<Entity>());
        }
        entity.gameObject.SetActive(false);
        unusedEntities[entity.asset.globalID].Enqueue(entity);
    }

    public void SaveEntity (Entity entity) {
        WorldSaving.inst.SaveEntity(entity);
    }

    public void LoadFromUID (int uid) {
        if(!WorldSaving.inst.LoadEntityFile(uid, out EntityData entityData, GameManager.inst.currentDataLoadMode)) {
            return;
        }

        if(!GeneralAsset.inst.GetGlobalIDFromEntityString(entityData.assetReference, out int gid)) {
            return;
        }

        Entity entity = null;
        if(unusedEntities.ContainsKey(gid)) {
            if(unusedEntities[gid].Count > 0) {
                entity = unusedEntities[gid].Dequeue();
                entity.gameObject.SetActive(true);
            }
        }
        if(entity == null) {
            entity = Instantiate(GeneralAsset.inst.GetEntityAssetFromGlobalID(gid).prefab, transform);
        }

        entity.LoadData(entityData);
        entitiesByUID.Add(entityData.uid, entity);
        allLoadedEntities.Add(entity);

        EntityRegionManager.inst.AddEntity(entity);
        entity.gameObject.SetActive(false);
        EntityRegionManager.inst.outOfBoundsEntities.Add(entity);
    }

    public Entity GetEntityAtPoint (Vector2 point) {
        Collider2D box = Physics2D.OverlapPoint(point, 1 << 8);
        if(box == null) {
            return null;
        } else {
            return box.GetComponent<Entity>();
        }
    }
}
