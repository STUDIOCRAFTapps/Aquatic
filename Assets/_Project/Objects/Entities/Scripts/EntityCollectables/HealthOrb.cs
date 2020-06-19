using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class HealthOrbData : EntityData {
    public int direction;
}

public class HealthOrb : Entity {
    new public RigidbodyPixel rigidbody;

    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = base.LoadData(entityData);
        return hasBaseFailed;
    }

    public override void OnSpawn () {
        ((HealthOrbData)entityData).direction = 1;
        if(UnityEngine.Random.Range(0, 2) == 0) {
            ((HealthOrbData)entityData).direction *= -1;
        }
        base.OnSpawn();
    }

    public override void OnFixedUpdate () {
        if(rigidbody.isCollidingDown) {
            rigidbody.velocity += Vector2.up * 16f;
        }
        if(rigidbody.isCollidingLeft) {
            ((HealthOrbData)entityData).direction *= -1;
            rigidbody.velocity.x *= -1;
        }
        if(rigidbody.isCollidingRight) {
            ((HealthOrbData)entityData).direction *= -1;
            rigidbody.velocity.x *= -1;
        }
        if(((HealthOrbData)entityData).direction > 0) {
            rigidbody.velocity.x = Mathf.Min(rigidbody.velocity.x + Time.fixedTime * 0.5f, 2);
        } else {
            rigidbody.velocity.x = Mathf.Max(rigidbody.velocity.x - Time.fixedTime * 0.5f, -2);
        }
    }

    public override void OnLongUpdate () {
        PlayerController pc = GameManager.inst.GetNearestPlayer(transform.position);

        if(pc != null) {
            if(new Bounds2D((Vector2)transform.position - Vector2.one * 0.5f, (Vector2)transform.position + Vector2.one * 0.5f).Overlaps(pc.rbody.aabb)) {
                pc.HealPlayer(((HealthOrbAsset)asset).healthPoints);
                ParticleManager.inst.PlayFixedParticle((Vector2)transform.position, 5);
                EntityManager.inst.Kill(this);
            }
        }
    }

    public override Type GetDataType () {
        return typeof(HealthOrbData);
    }
}
