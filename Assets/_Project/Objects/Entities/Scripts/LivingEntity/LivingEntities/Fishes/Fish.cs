using System;
using System.Collections.Generic;
using UnityEngine;

// States:
// 0 - Chill (Idle, Attemps to keep same height, Travels slowly from side to side)
// 1 - Chase (Active follow, Will path find if player is not in direct line of sight)
// 2 - Chomp (Attack, Will imidialty retreat after)
// 3 - Retreat (Pause after attack, Will return to chase after it)

// Animation callbacks:
// 0 - Chomp Attack Damage Point
// 1 - On Chomp End
// 2 - On Retreat End

[Serializable]
public class FishData : LivingEntityData {
    
}

public class Fish : LivingEntity {

    public SpriteRenderer spriteRenderer;
    Vector2 targetDirection;
    float lastAttackTime;
    float targetIdleHeight;
    bool isIdlingLeft = false;

    public override bool LoadData (EntityData entityData) {
        bool hasBaseFailed = base.LoadData(entityData);

        if(hasBaseFailed) return hasBaseFailed;

        targetIdleHeight = entityData.position.y;

        // Recover Animator State
        int state = ((LivingEntityData)entityData).state;
        if(state == 0)
            animator.PlayClip("Chill");
        if(state == 1)
            animator.PlayClip("Chase");
        if(state == 2) {
            animator.PlayClip("Chase");
            ((LivingEntityData)entityData).state = 1;
        }
        if(state == 3)
            animator.PlayClip("Chill");

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
            if(rigidbody.submergedPercentage < 0.05f) {
                if(rigidbody.isCollidingDown) {
                    rigidbody.velocity += new Vector2(UnityEngine.Random.Range(-9f, 9f), 18f);
                }
                if(rigidbody.isCollidingLeft) {
                    rigidbody.velocity += Vector2.right * 9f;
                }
                if(rigidbody.isCollidingRight) {
                    rigidbody.velocity += Vector2.left * 9f;
                }
            } else {
                rigidbody.velocity += targetDirection * ((FishAsset)asset).speed * Time.deltaTime;
            }
        }

        base.OnFixedUpdate();
    }

    public override void OnManageAI () {
        PlayerController pc = GameManager.inst.GetNearestPlayer(transform.position);
        if(pc == null) {
            return;
        }

        float maxTargetDist = ((FishAsset)asset).maxTargetingDistance;
        float dist = Vector2.Distance(transform.position, pc.GetHeadPosition());
        if(dist > maxTargetDist) {
            if(((LivingEntityData)entityData).state == 1 || ((LivingEntityData)entityData).state == 2) {
                animator.PlayClipWithoutRestart("Chill");
                ((LivingEntityData)entityData).state = 0;
                targetIdleHeight = entityData.position.y;
            }
        } else {
            //ERROR! Should know if player is accesible before doing anything. Send pathfind request maybe?
            /*if(((LivingEntityData)entityData).state == 0) {
                animator.PlayClipWithoutRestart("Chase");
                ((LivingEntityData)entityData).state = 1;
            }*/
            Vector2Int rsize = Vector2Int.CeilToInt(rigidbody.box.size);
            Vector2 size = rigidbody.box.size;

            PathRequestManager.inst.RequestNextPoint(
                new PathRequest(
                    (Vector2)transform.position - (size * 0.5f) + rigidbody.box.offset + (Vector2.one * 0.5f),
                    pc.GetCenterPosition(),
                    (Action<Vector2, bool>)OnNextPointReceived
                ),
                rsize
            );
        }

        int state = ((LivingEntityData)entityData).state;
        if(state == 0) {
            targetDirection = (new Vector2(transform.position.x + (isIdlingLeft ? -1f : 1f), targetIdleHeight) - (Vector2)transform.position).normalized * 0.2f;
            if(rigidbody.isCollidingLeft) {
                isIdlingLeft = false;
            } else if(rigidbody.isCollidingRight) {
                isIdlingLeft = true;
            }
        } else if(state == 1 || state == 2) {
            // Raycast to check if the player is visible
            // If it is, go toward the player
            Vector2 dir = (pc.GetHeadPosition() - (Vector2)transform.position).normalized;
            if(PhysicsPixel.inst.RaycastTerrain(new Ray2D(transform.position, dir), dist, out Vector2 hitPoint)) {
                Vector2Int rsize = Vector2Int.CeilToInt(rigidbody.box.size);
                Vector2 size = rigidbody.box.size;

                PathRequestManager.inst.RequestNextPoint(
                    new PathRequest(
                        (Vector2)transform.position - (size * 0.5f) + rigidbody.box.offset + (Vector2.one * 0.5f),
                        pc.GetCenterPosition(),
                        (Action<Vector2, bool>)OnNextPointReceived
                    ),
                    rsize
                );
            } else {
                // If the player is clDose enough to attack, do it
                if(state == 1 && dist < 2f && (Time.time - lastAttackTime) > ((FishAsset)asset).minAttakInterval) {
                    lastAttackTime = Time.time;
                    animator.PlayClip("Chomp");
                    ((LivingEntityData)entityData).state = 2;
                }
                targetDirection = dir;
            }
        } else if(state == 3) {
            targetDirection = Vector2.zero;
            if((Time.time - lastAttackTime) > ((FishAsset)asset).minAttakInterval) {
                animator.PlayClipWithoutRestart("Chase");
                ((LivingEntityData)entityData).state = 1;
            }
        }
    }
    
    public void OnNextPointReceived (Vector2 nextPoint, bool pathSuccessful) {
        if(!pathSuccessful) {
            if(((LivingEntityData)entityData).state == 1 || ((LivingEntityData)entityData).state == 2) {
                animator.PlayClipWithoutRestart("Chill");
                ((LivingEntityData)entityData).state = 0;
                targetIdleHeight = entityData.position.y;
            }
            return;
        } else {
            if(((LivingEntityData)entityData).state == 0) {
                animator.PlayClipWithoutRestart("Chase");
                ((LivingEntityData)entityData).state = 1;
            }
        }

        targetDirection = (nextPoint - (Vector2)transform.position).normalized;
        if(rigidbody.isCollidingDown) {
            if(targetDirection.x > 0f && targetDirection.x < 0.9f) {
                targetDirection = Vector2.right;
            } else if(targetDirection.x < 0f && targetDirection.x > -0.9f) {
                targetDirection = Vector2.left;
            }
        }
    }

    public override void OnRecieveCallback (uint code) {
        if(code == 0) {
            
        } else if(code == 1) {
            animator.PlayClipWithoutRestart("Chill");
            ((LivingEntityData)entityData).state = 3;
        }
    }

    public override Type GetDataType () {
        return typeof(FishData);
    }
}