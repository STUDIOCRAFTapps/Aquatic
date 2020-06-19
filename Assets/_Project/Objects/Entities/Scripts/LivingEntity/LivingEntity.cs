using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LivingEntityData : EntityData {
    public float health;
    public Vector2 velocity;
    public bool noAI = false;
    public int state = 0;

    public List<GenericEffect> effects;

    public bool GetEffect (string codeName, out GenericEffect effect) {
        if(effects == null) {
            effect = null;
            return false;
        }
        foreach(GenericEffect e in effects) {
            if(e.codeName == codeName) {
                effect = e;
                return true;
            }
        }
        effect = null;
        return false;
    }
}

public class LivingEntity : Entity, IPixelAnimationCallbackReciever, IInteractableEntity {

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

        // Update effects duration
        List<GenericEffect> effects = ((LivingEntityData)entityData).effects; // Reference of the effect list for easier reading
        if(effects != null) {
            for(int i = effects.Count - 1; i >= 0; i--) {
                effects[i].duration -= Time.fixedDeltaTime;
                if(effects[i].duration < 0) {
                    effects.RemoveAt(i);
                }
            }
        }


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

    public virtual bool HitEntity (float damage) {
        ((LivingEntityData)entityData).health -= damage;
        ParticleManager.inst.PlayFlytext(transform.position, Mathf.Ceil(damage).ToString(), 0.5f, 2f);

        if(((LivingEntityData)entityData).health <= 0) {
            EntityManager.inst.Kill(this);
            ParticleManager.inst.PlayFixedParticle(transform.position, 4);
            return true;
        }
        return false;
    }

    public override Type GetDataType () {
        return typeof(LivingEntityData);
    }

    public virtual void OnRecieveCallback (uint code) {

    }

    public virtual bool OnCheckInteractWithCollider (Collider2D collider) {
        Bounds targeter = collider.bounds;
        targeter.SetMinMax(new Vector3(targeter.min.x, targeter.min.y), new Vector3(targeter.max.x, targeter.max.y));
        Bounds target = rigidbody.box.bounds;
        target.SetMinMax(new Vector3(target.min.x, target.min.y), new Vector3(target.max.x, target.max.y));

        if(!targeter.Intersects(target)) {
            return false;
        }
        //Debug.Log(collider.IsTouching(rigidbody.box));
        return true;
    }

    public virtual float OnCheckInteractWithRay (Ray2D ray) {
        Bounds target = rigidbody.box.bounds;
        target.SetMinMax(new Vector3(target.min.x, target.min.y), new Vector3(target.max.x, target.max.y));
        
        if(target.IntersectRay(new Ray(ray.origin, ray.direction), out float distance)) {
            return distance;
        }
        return Mathf.Infinity;
    }

    public void ApplyEffect (string codeName, float duration, byte level) {
        if(((LivingEntityData)entityData).GetEffect(codeName, out GenericEffect effect)) {
            effect.duration = Mathf.Max(effect.duration, duration);
            effect.level = level > effect.level ? level : effect.level;
        } else {
            if(((LivingEntityData)entityData).effects == null) {
                ((LivingEntityData)entityData).effects = new List<GenericEffect>();
            }
            ((LivingEntityData)entityData).effects.Add(new GenericEffect() {codeName = codeName, duration = duration, level = level});
        }
    }
}