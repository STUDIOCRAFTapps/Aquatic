using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAquaticNavigationModule", menuName = "Player/Modules/AquaticNavigation", order = -1)]
public class AquaticNavigationModule : BasePlayerModule {

    [Header("Parameters")]
    public float directionSmoothingFactor;
    public float directionSmoothingSpeed;
    public float directionSmoothVelInfluence;
    public float propulsionAcc;
    public float propulsionFriction;
    public float propulsionMax;
    public float propulsionMaxOutOfWater;
    public float propulsionUpBoost;
    public BasicNavigationModule groundNavigation;

    // Direction
    public override void UpdateStatus (PlayerInfo info) {
        int accDirX = 0;
        int accDirY = 0;
        if(Input.GetKey(KeyCode.A))
            accDirX--;
        if(Input.GetKey(KeyCode.D))
            accDirX++;
        if(Input.GetKey(KeyCode.W) || info.status.onGoingJump)
            accDirY++;
        if(Input.GetKey(KeyCode.S) && !info.status.onGoingJump)
            accDirY--;
        
        if(groundNavigation != null && info.status.isGrounded && !(accDirY > 0)) {
            groundNavigation.UpdateStatus(info);
            return;
        }

        #region Direction
        Vector2 targetDir = new Vector2(info.status.isGrounded ? 0 : accDirX, accDirY);
        float blend = 1f - Mathf.Pow(1f - directionSmoothingFactor, Time.deltaTime * directionSmoothingSpeed);

        bool isCurrentDirectionNull = accDirX == 0 && accDirY == 0;

        if(info.status.wasLastDirNull && !isCurrentDirectionNull) {
            info.status.combinedDirection = Vector2.Lerp(targetDir, info.rbody.velocity.normalized, info.rbody.velocity.magnitude * directionSmoothVelInfluence);
        } else if(Vector2.Dot(info.status.combinedDirection, targetDir) < -0.25f) {
            info.status.combinedDirection = Vector2.Lerp(targetDir, info.rbody.velocity.normalized, info.rbody.velocity.magnitude * directionSmoothVelInfluence);
        }
        if(!isCurrentDirectionNull) {
            info.status.combinedDirection = Quaternion.Slerp(
                Quaternion.LookRotation(Vector3.forward, info.status.combinedDirection),
                Quaternion.LookRotation(Vector3.forward, targetDir),
                blend
            ) * Vector3.up;
        } else {
            info.status.combinedDirection = Vector2.zero;
        }
        
        if(targetDir.y < 0 && (info.rbody.isCollidingLeft || info.rbody.isCollidingRight)) {
            info.status.combinedDirection = Vector2.down;
        }
        if(targetDir.y > 0 && (info.rbody.isCollidingLeft || info.rbody.isCollidingRight)) {
            info.status.combinedDirection = Vector2.up;
        }
        if(targetDir.x < 0 && (info.rbody.isCollidingUp || info.rbody.isCollidingDown)) {
            info.status.combinedDirection = Vector2.left;
        }
        if(targetDir.x > 0 && (info.rbody.isCollidingUp || info.rbody.isCollidingDown)) {
            info.status.combinedDirection = Vector2.right;
        }

        info.status.wasLastDirNull = accDirX == 0 && accDirY == 0;
        #endregion

        #region Propulsion
        Vector2 wallNormal = new Vector2(
            (info.rbody.isCollidingLeft ? -1f : 0f) + (info.rbody.isCollidingRight ? 1f : 0f),
            (info.rbody.isCollidingDown ? -1f : 0f) + (info.rbody.isCollidingUp ? 1f : 0f)
        );

        if(accDirX != 0f || accDirY != 0f) {
            if(wallNormal == Vector2.zero) {
                info.status.propulsionValue += propulsionAcc * Time.deltaTime;
            } else {
                info.status.propulsionValue += propulsionAcc * Time.deltaTime * Mathf.Min(1f, (-Vector2.Dot(wallNormal, info.status.combinedDirection) + 1f));
            }
        }
        info.status.propulsionValue = Mathf.Min(info.status.propulsionValue, Mathf.Lerp(propulsionMaxOutOfWater, propulsionMax, info.rbody.submergedPercentage));
        info.status.propulsionValue *= (1f - Time.fixedDeltaTime * propulsionFriction);
        #endregion
    }
    public override void UpdateAction (PlayerInfo info) {
        if(groundNavigation != null && info.status.isGrounded) {
            groundNavigation.UpdateAction(info);
            return;
        }

        Vector2 impulse = info.status.combinedDirection * info.status.propulsionValue;
        if(impulse.y > 0f) {
            impulse.y *= propulsionUpBoost;
        }
        info.rbody.velocity += impulse;
    }

    

    void AccelerateBody (PlayerInfo info, Vector2 direction, float maxSpeed, float acceleration) {
        Vector2 target = direction * maxSpeed;
        Vector2 impulse = direction * acceleration * Time.deltaTime * 50f;
        Vector2 v = info.rbody.velocity;

        if(target.x > 0f && v.x < target.x) {
            v.x = Mathf.Min(target.x, v.x + impulse.x);
        } else if(target.x < 0f && v.x > target.x) {
            v.x = Mathf.Max(target.x, v.x + impulse.x);
        }
        if(target.y > 0f && v.y < target.y) {
            v.y = Mathf.Min(target.y, v.y + impulse.y);
        } else if(target.y < 0f && v.y > target.y) {
            v.y = Mathf.Max(target.y, v.y + impulse.y);
        }

        info.rbody.velocity = v;
    }
}