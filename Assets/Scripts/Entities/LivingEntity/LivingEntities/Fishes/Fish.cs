using System;
using System.Collections.Generic;
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
            Vector2Int rsize = Vector2Int.CeilToInt(rigidbody.box.size);
            Vector2 size = rigidbody.box.size;

            PathRequestManager.inst.RequestNextPoint(
                new PathRequest(
                    (Vector2)transform.position - (size * 0.5f) + rigidbody.box.offset + (Vector2.one * 0.5f),
                    pc.GetCenterPosition(),
                    (Action<Vector2, bool>)OnNexPointReceived
                ),
                rsize
            );
        } else {
            targetDirection = dir;
        }

        // Check the distance to see if it's close enough
        // Raycast to check if the player is visible
        // If it is, go toward the player
        // If not pathfind toward it

        base.OnLongUpdate();
    }
    
    public void OnNexPointReceived (Vector2 nextPoint, bool pathSuccessful) {
        if(!pathSuccessful) {
            return;
        }
        Debug.DrawLine(nextPoint, (Vector2)transform.position, Color.yellow);
        targetDirection = (nextPoint - (Vector2)transform.position).normalized;
        if(rigidbody.isCollidingDown) {
            if(targetDirection.x > 0f && targetDirection.x < 0.9f) {
                targetDirection = Vector2.right;
            } else if(targetDirection.x < 0f && targetDirection.x > -0.9f) {
                targetDirection = Vector2.left;
            }
        }
    }

    public override Type GetDataType () {
        return typeof(FishData);
    }
}