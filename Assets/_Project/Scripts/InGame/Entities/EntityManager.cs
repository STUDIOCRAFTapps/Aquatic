using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class EntityManager : MonoBehaviour {
    
    public float longUpdateFrequency = 0.1f;

    public List<Entity> allLoadedEntities;
    public Dictionary<int, Queue<Entity>> unusedEntities;
    public Dictionary<int, Queue<EntityData>> unusedEntityData;
    public Dictionary<int, Entity> entitiesByUID;
    public Queue<Entity> terminationRequests;

    public Dictionary<int, EntityJobManager> entityJobManagers;

    public static EntityManager inst;

    int basicUID = 0;

    private void Awake () {
        inst = this;

        allLoadedEntities = new List<Entity>();
        unusedEntities = new Dictionary<int, Queue<Entity>>();
        unusedEntityData = new Dictionary<int, Queue<EntityData>>();
        entitiesByUID = new Dictionary<int, Entity>();
        terminationRequests = new Queue<Entity>();

        entityJobManagers = new Dictionary<int, EntityJobManager>();

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

    #region Interaction
    public void ExecuteOverlapsEntity (Collider2D coll, Action<Entity> action) {
        for(int i = allLoadedEntities.Count - 1; i >= 0; i--) {
            Entity target = allLoadedEntities[i];
            IInteractableEntity interactEntity = target as IInteractableEntity;
            if(interactEntity != null) {
                if(interactEntity.OnCheckInteractWithCollider(new Bounds2D(coll.bounds.min, coll.bounds.max))) {
                    action(target);
                }
            }
        }
    }

    public void ExecuteOverlapsEntity (Bounds2D coll, Action<Entity> action) {
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

    public void ExecuteRaycastEntity (Ray2D ray, float maxDistance, Action<Entity> action) {
        for(int i = allLoadedEntities.Count - 1; i >= 0; i--) {
            Entity target = allLoadedEntities[i];
            IInteractableEntity interactEntity = target as IInteractableEntity;
            if(interactEntity == null) {
                continue;
            }
            if(interactEntity.OnCheckInteractWithRay(ray, out float distance)) {
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance, Color.red);
                if(distance <= maxDistance) {
                    action(target);
                }
            }
        }
    }

    public float RaycastEntities (Ray2D ray) {
        float minDistance = Mathf.Infinity;
        foreach(Entity target in allLoadedEntities) {
            IInteractableEntity interactEntity = target as IInteractableEntity;
            if(interactEntity == null) {
                continue;
            }
            if(interactEntity.OnCheckInteractWithRay(ray, out float dist)) {
                if(dist < minDistance) {
                    minDistance = dist;
                }
            }
        }
        return minDistance;
    }

    public Entity GetEntityAtPoint (Vector2 point) {
        Collider2D box = Physics2D.OverlapPoint(point, 1 << 8);
        if(box == null) {
            return null;
        } else {
            return box.GetComponent<Entity>();
        }
    }
    #endregion

    #region Spawning
    public Entity GetNewEntity (int gid) {
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

        return entity;
    }

    public EntityData GetNewEntityData (int gid) {
        EntityData entityData = null;

        if(unusedEntityData.ContainsKey(gid)) {
            if(unusedEntityData[gid].Count > 0) {
                entityData = unusedEntityData[gid].Dequeue();
            }
        }
        if(entityData == null) {
            Type dataType = GeneralAsset.inst.GetEntityAssetFromGlobalID(gid).prefab.GetDataType();
            entityData = (EntityData)Activator.CreateInstance(dataType);
        }

        return entityData;
    }

    public Entity Spawn (Vector2 position, EntityString entity) {
        return Spawn(position, GeneralAsset.inst.namespaceByString[entity.nspace].entitiesByString[entity.id].globalID);
    }

    public Entity Spawn (Vector2 position, int gid) {
        Entity entity = GetNewEntity(gid);
        EntityData entityData = (EntityData)Activator.CreateInstance(entity.GetDataType()); // Cannot borrow new entityData from pool, it can't be cleaned.
        entityData.position = position;
        entityData.uid = basicUID;
        entityData.assetReference = GeneralAsset.inst.GetEntityStringFromGlobalID(gid);
        entity.LoadData(entityData);
        
        entitiesByUID.Add(entityData.uid, entity);
        allLoadedEntities.Add(entity);
        EntityRegionManager.inst.AddEntity(entity);

        int uid = basicUID;
        basicUID++;
        PlayerPrefs.SetInt("entityUID", basicUID);
        Debug.Log("Change shitty uid system, you know what you know how");
        
        StartNewChunkJobAt(
            uid, 
            JobState.Loaded, 
            GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
            null, null, null
        );

        return entity;
    }

    // TODO: Spawn should create jobm entry,
    // Load should create a job
    // Job should create a job
    // Save should create a job
    // Job interaction should be correctly managed
    public void LoadFromUID (int uid, int gid) {
        EntityData entityData = GetNewEntityData(gid);

        StartNewChunkJobAt(
            uid,
            JobState.Loaded,
            GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
            () => { // JOB
                if(!WorldSaving.inst.LoadEntityFile(uid, gid, entityData, GameManager.inst.currentDataLoadMode)) {
                    return;
                }
            },
            () => { // CALLBACK
                Entity entity = GetNewEntity(gid);

                entity.LoadData(entityData);
                EntityRegionManager.inst.AddEntity(entity);
                entity.gameObject.SetActive(false);
                EntityRegionManager.inst.outOfBoundsEntities.Add(entity);

                entitiesByUID.Add(entityData.uid, entity);
                allLoadedEntities.Add(entity);
            },
            () => { //CALLBACK IF CANCELED
                unusedEntityData[gid].Enqueue(entityData);
            }
        );
    }
    #endregion

    #region Killing (Delete and Save)
    public void Kill (Entity entity) {
        terminationRequests.Enqueue(entity);
    }

    void Internal_Kill (Entity entity) {
        EntityRegionManager.inst.RemoveEntity(entity);

        entityJobManagers[entity.entityData.uid].CancelAllJobs();
        entitiesByUID.Remove(entity.entityData.uid);
        allLoadedEntities.Remove(entity);

        if(!unusedEntities.ContainsKey(entity.asset.globalID)) {
            unusedEntities.Add(entity.asset.globalID, new Queue<Entity>());
        }
        entity.gameObject.SetActive(false);
        unusedEntities[entity.asset.globalID].Enqueue(entity);
        unusedEntityData[entity.asset.globalID].Enqueue(entity.entityData);

        WorldSaving.inst.DeleteEntity(entity);
    }

    public void UnloadEntity (Entity entity, bool removeFromRegions = false, bool save = true) {
        if(removeFromRegions) {
            EntityRegionManager.inst.RemoveEntity(entity);
        }
        entitiesByUID.Remove(entity.entityData.uid);
        allLoadedEntities.Remove(entity);

        
        entity.gameObject.SetActive(false);
        StartNewChunkJobAt(
            entity.entityData.uid,
            JobState.Unloaded,
            GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
            () => { // JOB
                if(save) {
                    WorldSaving.inst.SaveEntity(entity);
                }
            },
            () => { // CALLBACK
                if(!unusedEntities.ContainsKey(entity.asset.globalID)) {
                    unusedEntities.Add(entity.asset.globalID, new Queue<Entity>());
                }
                if(!unusedEntityData.ContainsKey(entity.asset.globalID)) {
                    unusedEntityData.Add(entity.asset.globalID, new Queue<EntityData>());
                }
                unusedEntities[entity.asset.globalID].Enqueue(entity);
                unusedEntityData[entity.asset.globalID].Enqueue(entity.entityData);
            },
            () => { //CALLBACK IF CANCELED

            }
        );
    }

    public void SaveEntity (Entity entity) {
        StartNewChunkJobAt(
            entity.entityData.uid,
            JobState.Saving,
            GameManager.inst.currentDataLoadMode == DataLoadMode.TryReadonly,
            () => { // JOB
                WorldSaving.inst.SaveEntity(entity);
            },
            () => { // CALLBACK

            },
            () => { //CALLBACK IF CANCELED

            }
        );
        
    }
    #endregion

    #region Job Management
    public JobState GetChunkLoadState (int uid) {
        if(entityJobManagers.TryGetValue(uid, out EntityJobManager cjm)) {
            return cjm.targetLoadState;
        } else {
            return JobState.Unloaded;
        }
    }

    public void StartNewChunkJobAt (int uid, JobState newLoadState, bool isReadonly, Action job, Action callback, Action cancelCallback) {
        if(entityJobManagers.TryGetValue(uid, out EntityJobManager cjm)) {
            cjm.StartNewJob(newLoadState, isReadonly, job, callback, cancelCallback);
        } else {
            EntityJobManager newCjm = new EntityJobManager(uid);
            entityJobManagers.Add(uid, newCjm);
            if(job != null) {
                newCjm.StartNewJob(newLoadState, isReadonly, job, callback, cancelCallback);
            } else if(newLoadState != JobState.Saving) {
                newCjm.targetLoadState = newLoadState;
            }
        }
    }

    public void RemoveJobManager (int uid) {
        entityJobManagers.Remove(uid);
    }
    #endregion

    
}
