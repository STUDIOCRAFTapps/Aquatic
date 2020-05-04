using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFrictionModule", menuName = "Player/Modules/Friction", order = -1)]
public class FrictionModule : BasePlayerModule {

    [Header("Parameters")]
    public float groundFriction = 16f;
    public float floatingFriction = 1f;

    public override void UpdateStatus (PlayerInfo info) {

    }

    public override void UpdateAction (PlayerInfo info) {
        if(info.status.isGrounded && !(info.status.isInAirBecauseOfJump && info.status.onGoingJump)) {
            info.rbody.velocity *= (1f - Time.fixedDeltaTime * groundFriction);
        } else {
            info.rbody.velocity *= (1f - Time.fixedDeltaTime * floatingFriction);
        }
    }
}
