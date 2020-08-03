using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class WaterCurrentData : EntityData {
    public Vector2 direction = Vector2.up;
    public float timeLifeTime;
}

public class WaterCurrent : Entity {

    public Transform visualAnchor;
    public BoxCollider2D areaEffectCollider;

    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = base.LoadData(entityData);
        return hasBaseFailed;
    }

    public void SetDirection (Vector2 direction) {
        ((WaterCurrentData)entityData).direction = direction;
        visualAnchor.eulerAngles = Vector3.forward * Mathf.Atan2(-((WaterCurrentData)entityData).direction.x, ((WaterCurrentData)entityData).direction.y) * Mathf.Rad2Deg;
    }

    public override void OnSpawn () {
        visualAnchor.eulerAngles = Vector3.forward * Mathf.Atan2(-((WaterCurrentData)entityData).direction.x, ((WaterCurrentData)entityData).direction.y) * Mathf.Rad2Deg;
        base.OnSpawn();
    }

    public override void OnLongUpdate () {
        EntityManager.inst.ExecuteOverlapsEntity(areaEffectCollider, (entity) => {
            RigidbodyPixel rb = entity.GetComponent<RigidbodyPixel>();
            if(rb != null) {
                rb.velocity += ((WaterCurrentData)entityData).direction * rb.mass * ((WaterCurrentAsset)asset).force * EntityManager.inst.longUpdateFrequency;
            }
        });
        ((WaterCurrentData)entityData).timeLifeTime += EntityManager.inst.longUpdateFrequency;
        if(((WaterCurrentData)entityData).timeLifeTime > ((WaterCurrentAsset)asset).maxMaxLastTime) {
            EntityManager.inst.Kill(this);
        }
    }

    public override Type GetDataType () {
        return typeof(WaterCurrentData);
    }
}
