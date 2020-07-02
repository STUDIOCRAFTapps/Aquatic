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
                    OnEffectUpdate();
                }
            }
        }


        base.OnFixedUpdate();
    }

    #region Behaviour
    public override void OnLongUpdate () {
        if(!((LivingEntityData)entityData).noAI) {
            OnManageAI();
        }
        base.OnLongUpdate();
    }

    public virtual void OnManageAI () {

    }

    public virtual void OnRecieveCallback (uint code) {

    }
    #endregion

    #region Effects
    // Called only when new effects get applied or run out
    public virtual void OnEffectUpdate () {

    }

    public bool GetEffect (string codeName, out GenericEffect effect) {
        if(((LivingEntityData)entityData).effects == null) {
            effect = null;
            return false;
        }
        foreach(GenericEffect e in ((LivingEntityData)entityData).effects) {
            if(e.codeName == codeName) {
                effect = e;
                return true;
            }
        }
        effect = null;
        return false;
    }

    public void ApplyEffect (string codeName, float duration, byte level) {
        if(GetEffect(codeName, out GenericEffect effect)) {
            effect.duration = Mathf.Max(effect.duration, duration);
            effect.level = level > effect.level ? level : effect.level;
        } else {
            if(((LivingEntityData)entityData).effects == null) {
                ((LivingEntityData)entityData).effects = new List<GenericEffect>();
            }
            ((LivingEntityData)entityData).effects.Add(new GenericEffect() {
                codeName = codeName, duration = duration, level = level
            });
            OnEffectUpdate();
        }
    }
    #endregion

    #region Interaction
    public virtual bool HitEntity (float damage) {
        animator.PlayHitFlash(0.2f);
        ((LivingEntityData)entityData).health -= damage;
        ParticleManager.inst.PlayFlytext(transform.position, Mathf.Ceil(damage).ToString(), 0.5f, 2f);

        if(((LivingEntityData)entityData).health <= 0) {
            EntityManager.inst.Kill(this);
            ParticleManager.inst.PlayFixedParticle(transform.position, 4);
            return true;
        }
        return false;
    }

    public virtual bool OnCheckInteractWithCollider (Bounds2D colliderBounds) {
        ref Bounds2D targeter = ref colliderBounds;
        Bounds2D target = new Bounds2D(rigidbody.box.bounds.min, rigidbody.box.bounds.max);

        if(!targeter.Overlaps(target)) {
            return false;
        }
        return true;
    }

    public virtual bool OnCheckInteractWithRay (Ray2D ray, out float distance) {
        Bounds2D target = new Bounds2D(rigidbody.box.bounds.min, rigidbody.box.bounds.max);

        Vector2 invDir = new Vector2(ray.direction.x == 0f ? 0f : 1f / ray.direction.x, ray.direction.y == 0f ? 0f : 1f / ray.direction.y);
        if(target.IntersectRay(ray.origin, invDir, out float dist)) {
            if(dist < 0f) {
                distance = Mathf.Infinity;
                return false;
            }
            distance = dist;
            return true;
        } else {
            distance = Mathf.Infinity;
            return false;
        }
    }
    #endregion

    public override Type GetDataType () {
        return typeof(LivingEntityData);
    }
}