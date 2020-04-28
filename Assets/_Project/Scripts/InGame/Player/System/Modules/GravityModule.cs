using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGravityModule", menuName = "Player/Modules/Gravity", order = -1)]
public class GravityModule : BasePlayerModule {

    [Header("Parameters")]
    public float upwardGravityForce = 45f;
    public float downwardGravityForce = 100f;
    public bool twoForceGravity = false;
    public bool submergionReduceGravity = false;

    public override void UpdateStatus (PlayerInfo info) {
        
    }

    public override void UpdateAction (PlayerInfo info) {
        if(twoForceGravity) {
            if((info.status.onGoingJump && info.rbody.velocity.y > 0) || (submergionReduceGravity && info.rbody.submergedPercentage > 0.3f)) {
                info.status.canceledJump = false;
                info.rbody.velocity += upwardGravityForce * Vector2.down * Time.fixedDeltaTime;
            } else {
                info.rbody.velocity += downwardGravityForce * Vector2.down * Time.fixedDeltaTime;
            }
        } else {
            info.rbody.velocity += downwardGravityForce * Vector2.down * Time.fixedDeltaTime;
        }
    }
}