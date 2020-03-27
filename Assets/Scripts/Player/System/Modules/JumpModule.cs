﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewJumpModule", menuName = "Player/Modules/Jump", order = -1)]
public class JumpModule : BasePlayerModule {

    [Header("Parameters")]
    public float initialJumpForce;
    public float jumpCooldown;
    public float maxUngroundedJumpTime;
    public float minFluidJumpTime;
    public float outOfFluidJumpForce;

    public float jumpCancelForce;
    public bool jumpCancelEnabled;

    public override void UpdateStatus (PlayerInfo info) {
        // Checks for input and jump cancellation
        info.status.wasOnGoingJump = info.status.onGoingJump;
        info.status.onGoingJump = Input.GetKey(KeyCode.Space);
        if(info.status.wasOnGoingJump && !info.status.onGoingJump) {
            info.status.canceledJump = true;
        }
    }

    public override void UpdateAction (PlayerInfo info) {
        // Jump Timers
        float jumpCooldownTimer = Time.time - info.status.lastJumpTime;
        float ungroundedCooldownTimer = Time.time - info.status.lastGroundedTime;

        // Jump Checks
        bool jumpOptReq0 = info.status.isGrounded;
        bool jumpOptReq1 = (ungroundedCooldownTimer < maxUngroundedJumpTime && !info.status.isInAirBecauseOfJump);
        bool jumpOptReq2 = (info.rbody.submergedPercentage > 0.5f && info.rbody.submergedPercentage < 0.6f) && info.status.fluidTime > minFluidJumpTime && !info.status.isGrounded;
        bool jumpReq0 = info.status.onGoingJump;
        bool jumpReq1 = (jumpCooldownTimer > jumpCooldown);

        // Execute action
        bool canJump = jumpReq0 && jumpReq1 && (jumpOptReq0 || jumpOptReq1 || jumpOptReq2);
        if(canJump) {
            info.rbody.velocity.y = 0;
            info.rbody.velocity += Vector2.up * ((jumpOptReq2) ? outOfFluidJumpForce : initialJumpForce);
            info.status.lastJumpTime = Time.time;
            info.status.isInAirBecauseOfJump = true;
        }

        // Jump Cancel
        if(info.status.canceledJump && (info.rbody.velocity.y > 0) && jumpCancelEnabled) {
            info.rbody.velocity += jumpCancelForce * Vector2.down;
        }
    }
}
