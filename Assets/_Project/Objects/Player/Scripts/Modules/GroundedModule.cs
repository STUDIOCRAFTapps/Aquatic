using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGroundedModule", menuName = "Player/Modules/Grounded", order = -1)]
public class GroundedModule : BasePlayerModule {

    //[Header("Parameters")]

    public override void UpdateStatus (PlayerInfo info) {
        // Checks if the player is on the ground and adjusts the player status accordingly
        bool wasGrounded = info.status.isGrounded;
        info.status.isGrounded = info.rbody.isCollidingDown;
        if(info.status.isGrounded && info.rbody.velocity.y <= 0) {
            info.status.isInAirBecauseOfJump = false;
            info.status.lastGroundedTime = Time.time;
        }

        // Update fluid time
        if(info.rbody.submergedPercentage > 0.1f) {
            info.status.fluidTime += Time.deltaTime;
        } else {
            info.status.fluidTime = 0f;
        }

        // Land particle
        if(!wasGrounded && info.status.isGrounded && info.status.prevVel.y < -20f) {
            ParticleManager.inst.PlayFixedParticle(info.rbody.transform.position, 1);
        }
        info.status.prevVel = info.rbody.velocity;
    }

    public override void UpdateAction (PlayerInfo info) {
    }
}