using System;
using UnityEngine;

// Allows typenamehandling auto to work at the "root".
[Serializable]
public class EntityDataWrapper {
    public EntityData entityData;

    public EntityDataWrapper (EntityData entityData) {
        this.entityData = entityData;
    }
}

[Serializable]
public class EntityData {
    public EntityString assetReference;
    public int uid;
    public Vector2 position;
    [NonSerialized] public Vector2 previousPosition;
    [NonSerialized] public float timeOfLastAutosave;
}

public class Entity : MonoBehaviour {

    public InterpolatedTransform interpolatedTransform {
        get {
            if(!testedForITransform) {
                _interpolatedTransform = GetComponent<InterpolatedTransform>();

                testedForITransform = true;
            }
            return _interpolatedTransform;
        }
        private set {
            _interpolatedTransform = value;
        }
    }
    InterpolatedTransform _interpolatedTransform;
    bool testedForITransform = false;

    public EntityData entityData { private set; get; }
    public EntityAsset asset { private set; get; }
    [HideInInspector] public float timeOfLastLongUpdate;

    // Executed when the managers needs the entity to restore a saved state from an entityData (Make sure to call base)
    public virtual bool LoadData (EntityData entityData) {
        this.entityData = entityData;
        if(GeneralAsset.inst.GetGlobalIDFromEntityString(entityData.assetReference, out int globalID)) {
            asset = GeneralAsset.inst.GetEntityAssetFromGlobalID(globalID);
        } else {
            return true;
        }
        SetPosition(entityData.position, true);

        OnSpawn();
        return false;
    }

    // Only execute once for the entiere lifetime of the entity (Make sure to call base)
    public virtual void OnSpawn () {

    }

    // Every frame, used mostly for rendering and smooth operations (Make sure to call base). This will still be called in editor mode.
    public virtual void OnUpdate () {
        
    }

    // Execute every fixed interval, used mostly for physics (Make sure to call base)
    public virtual void OnFixedUpdate () {
        entityData.previousPosition = entityData.position;
        entityData.position = transform.position;
        
        if(TerrainManager.inst.WorldToChunk(entityData.previousPosition) != TerrainManager.inst.WorldToChunk(entityData.position)) {
            if(!EntityRegionManager.inst.MoveEntity(this, entityData.previousPosition)) {
                // Failed moving, region not loaded. Must load and unload region imidiatly
                EntityRegionManager.inst.LoadRegionAtChunk(TerrainManager.inst.WorldToChunk(entityData.position));
                EntityRegionManager.inst.MoveEntity(this, entityData.previousPosition);
                EntityRegionManager.inst.UnloadRegionAtChunk(TerrainManager.inst.WorldToChunk(entityData.position));
                return;
            }
        }

        if(!TerrainManager.inst.IsEntityInLoadedChunks(this)) {
            gameObject.SetActive(false);
            EntityRegionManager.inst.outOfBoundsEntities.Add(this);

            //entityData.position = entityData.previousPosition;
            //SetPosition(entityData.position);
        }
    }

    // Execute every unfrequent fixed interval, used mostly for more heavy operation like player checks (Make sure to call base)
    public virtual void OnLongUpdate () {

    }

    public virtual Type GetDataType () {
        return typeof(EntityData);
    }

    public void SetPosition (Vector3 position, bool changeAllData = false) {
        if(interpolatedTransform != null) {
            interpolatedTransform.SetTransformPosition(position);
        } else {
            transform.position = position;
        }
        if(changeAllData) {
            entityData.position = position;
            entityData.previousPosition = position;
        }
    }

    #region Monobehaviour
    private void Update () {
        OnUpdate();
    }

    private void FixedUpdate () {
        if(GameManager.inst.engineMode != EngineModes.Play) {
            return;
        }
        OnFixedUpdate();
    }
    #endregion
}

public interface IInteractableEntity {
    bool OnCheckInteractWithCollider(Bounds2D colliderBounds);
    bool OnCheckInteractWithRay (Ray2D ray, out float distance);
}