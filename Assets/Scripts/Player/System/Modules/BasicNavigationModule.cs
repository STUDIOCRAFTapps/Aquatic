using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBasicNavigationModule", menuName = "Player/Modules/BasicNavigation", order = -1)]
public class BasicNavigationModule : BasePlayerModule {

    [Header("Parameters")]
    public float groundMaxSpeed;
    public float groundAcceleration;
    public float floatingMaxSpeed;
    public float floatingAcceleration;

    public override void UpdateStatus (PlayerInfo info) {
        int accDirX = 0;
        if(Input.GetKey(KeyCode.A))
            accDirX--;
        if(Input.GetKey(KeyCode.D))
            accDirX++;
        info.status.combinedDirection = new Vector2(accDirX, 0);
    }

    public override void UpdateAction (PlayerInfo info) {
        if(info.status.isGrounded) {
            AccelerateBody(
                info,
                info.status.combinedDirection,
                groundMaxSpeed, groundAcceleration
            );
        } else {
            AccelerateBody(
                info,
                info.status.combinedDirection,
                floatingMaxSpeed, floatingAcceleration
            );
        }
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