using System;
using UnityEngine;

[Serializable]
public class FishData : LivingEntityData {
    
}

public class Fish : LivingEntity {

    public SpriteRenderer spriteRenderer;
    Vector2 targetDirection;

    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = !base.LoadData(entityData);

        if(hasBaseFailed) return hasBaseFailed;

        return hasBaseFailed;
    }

    public override void OnSpawn () {
        base.OnSpawn();
    }

    public override void OnUpdate () {
        if(targetDirection.x != 0f) {
            spriteRenderer.flipX = targetDirection.x > 0f;
        }

        base.OnUpdate();
    }

    public override void OnFixedUpdate () {
        if(!((LivingEntityData)entityData).noAI) {
            rigidbody.velocity += targetDirection * 40f * Time.deltaTime;
        }

        base.OnFixedUpdate();
    }

    public override void OnLongUpdate () {
        if(((LivingEntityData)entityData).noAI) {
            return;
        }
        targetDirection = Vector2.zero;
        PlayerController pc = GameManager.inst.GetNearestPlayer(transform.position);
        if(pc == null) {
            return;
        }

        float maxTargetDist = ((FishAsset)asset).maxTargetingDistance;
        float dist = Vector2.Distance(transform.position, pc.GetHeadPosition());
        if(dist > maxTargetDist) {
            return;
        }

        Vector2 dir = (pc.GetHeadPosition() - (Vector2)transform.position).normalized;
        if(PhysicsPixel.inst.RaycastTerrain(new Ray2D(transform.position, dir), dist, out Vector2 hitPoint)) {
            return;
        }

        targetDirection = (pc.GetHeadPosition() - (Vector2)transform.position).normalized;

        // Check the distance to see if it's close enough
        // Raycast to check if the player is visible
        // If it is, go toward the player
        // If not pathfind toward it

        base.OnLongUpdate();
    }

    public override Type GetDataType () {
        return typeof(FishData);
    }
}