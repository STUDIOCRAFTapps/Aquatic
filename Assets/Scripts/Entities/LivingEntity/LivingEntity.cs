using System;
using UnityEngine;

[Serializable]
public class LivingEntityData : EntityData {
    public float health;
    public Vector2 velocity;
    public bool noAI = false;
}

public class LivingEntity : Entity {

    new public RigidbodyPixel rigidbody;
    
    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = !base.LoadData(entityData);
        rigidbody.Init();

        if(hasBaseFailed) return hasBaseFailed;

        ((LivingEntityData)entityData).health = ((LivingEntityAsset)asset).maxHealth;
        rigidbody.velocity = ((LivingEntityData)entityData).velocity;
        rigidbody.box.offset = ((LivingEntityAsset)asset).offset;
        rigidbody.box.size = ((LivingEntityAsset)asset).size;
        rigidbody.mass = ((LivingEntityAsset)asset).mass;

        return hasBaseFailed;
    }
    
    public override void OnSpawn () {
        base.OnSpawn();
    }
    
    public override void OnUpdate () {
        base.OnUpdate();
    }
    
    public override void OnFixedUpdate () {
        ((LivingEntityData)entityData).velocity = rigidbody.velocity;
        base.OnFixedUpdate();
    }

    public override void OnLongUpdate () {
        base.OnLongUpdate();
    }

    public override Type GetDataType () {
        return typeof(LivingEntityData);
    }
}