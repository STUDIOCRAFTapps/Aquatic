using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStrike : MonoBehaviour, IPixelAnimationCallbackReciever {

    public Collider2D hitCollider;
    public PixelAnimator pixelAnimator;
    public int assetID;
    protected PlayerController owner;
    protected Vector2 additionnalAttVel;
    protected BaseAttackStrikeAsset asset;
    protected Vector2 dir;

    int hitCount = 0;

    public virtual void Init (PlayerController owner, BaseAttackStrikeAsset asset, Vector2 additionnalAttVel, Vector2 direction) {
        hitCount = 0;
        this.owner = owner;
        pixelAnimator.PlayClip("Strike");
        this.additionnalAttVel = additionnalAttVel;
        this.asset = asset;
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
                if(asset.knockbackUponLast && hitCount == asset.maxHitCount) {
                    le.rigidbody.velocity = (dir * 25f * asset.knockbackMultiplier + additionnalAttVel);
                } else {
                    le.rigidbody.velocity = Vector2.zero;
                }
                le.animator.PlayHitFlash(0.2f);
                le.HitEntity(asset.damage);
            }
        });
    }

    public virtual void Die () {
        if(hitCount < asset.maxHitCount) {
            return;
        }

        if(asset.playParticleOnDeath != -1) {
            ParticleManager.inst.PlayFixedParticle(transform.position, asset.playParticleOnDeath);
        }
        CombatManager.inst.SetStrikeAsUnused(assetID, this);
    }
}
