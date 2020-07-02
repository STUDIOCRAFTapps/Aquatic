using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStrike : MonoBehaviour, IPixelAnimationCallbackReciever {

    public Collider2D hitCollider;
    public PixelAnimator pixelAnimator;
    public int assetID;
    protected PlayerController owner;
    protected Vector2 additionnalAttVel;
    protected BaseAttackStrikeData data;
    protected Vector2 dir;

    int hitCount = 0;

    public virtual void Init (PlayerController owner, BaseAttackStrikeData data, Vector2 additionnalAttVel, Vector2 direction) {
        hitCount = 0;
        this.owner = owner;
        pixelAnimator.PlayClip("Strike");
        this.additionnalAttVel = additionnalAttVel;
        this.data = data;
        dir = direction;
    }

    public virtual void OnRecieveCallback (uint code) {
        if(code == 0) {
            Hit();
        } else {
            Die();
        }
    }

    public virtual void Hit () {
        hitCount++;
        EntityManager.inst.ExecuteOverlapsEntity(hitCollider, (e) => {
            LivingEntity le = e as LivingEntity;
            if(le != null) {
                if(data.knockbackUponLast && hitCount == data.maxHitCount) {
                    le.rigidbody.velocity = (dir * 25f * data.knockbackMultiplier + additionnalAttVel);
                } else {
                    le.rigidbody.velocity = Vector2.zero;
                }
                le.HitEntity(data.damage);
                //le.ApplyEffect("default:freeze", 1f, 0);
            }
        });
    }

    public virtual void Die () {
        if(hitCount < data.maxHitCount && data.maxHitCount > 1) {
            return;
        }

        if(data.playParticleOnDeath != -1) {
            ParticleManager.inst.PlayFixedParticle(transform.position, data.playParticleOnDeath);
        }
        CombatManager.inst.SetStrikeAsUnused(assetID, this);
    }
}
