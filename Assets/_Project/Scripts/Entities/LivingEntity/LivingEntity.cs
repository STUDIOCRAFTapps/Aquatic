using System;
using UnityEngine;

[Serializable]
public class LivingEntityData : EntityData {
    public float health;
    public Vector2 velocity;
    public bool noAI = false;
    public int state = 0;
}

public class LivingEntity : Entity {

    new public RigidbodyPixel rigidbody;
    public PixelAnimator animator;
    
    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = base.LoadData(entityData);
        rigidbody.Init();
        
        if(hasBaseFailed) return hasBaseFailed;

        ((LivingEntityData)entityData).health = ((LivingEntityAsset)asset).maxHealth;
        rigidbody.velocity = ((LivingEntityData)entityData).velocity;
        rigidbody.box.offset = ((LivingEntityAsset)asset).offset;
        rigidbody.box.size = ((LivingEntityAsset)asset).size;
        rigidbody.mass = ((LivingEntityAsset)asset).mass;
        animator.animationGroup = ((LivingEntityAsset)asset).animationGroup;

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
        if(!((LivingEntityData)entityData).noAI) {
            OnManageAI();
        }
        base.OnLongUpdate();
    }

    public virtual void OnManageAI () {

    }

    public override Type GetDataType () {
        return typeof(LivingEntityData);
    }

    public virtual void OnAnimationCallback (uint code) {

    }
}