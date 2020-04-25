using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEditNavigationModule", menuName = "Player/Modules/EditNavigation", order = -1)]
public class EditNavigationModule : BasePlayerModule {

    [Header("Parameters")]
    public bool limitedToEdit = true;
    public float speed;

    public override void UpdateStatus (PlayerInfo info) {
        if(!limitedToEdit || GameManager.inst.engineMode == EngineModes.Edit) {
            int accDirX = 0;
            int accDirY = 0;
            if(Input.GetKey(KeyCode.A))
                accDirX--;
            if(Input.GetKey(KeyCode.D))
                accDirX++;
            if(Input.GetKey(KeyCode.S))
                accDirY--;
            if(Input.GetKey(KeyCode.W))
                accDirY++;
            info.status.combinedDirection = new Vector2(accDirX, accDirY);
        }
    }

    public override void UpdateAction (PlayerInfo info) {
        if(!limitedToEdit || GameManager.inst.engineMode == EngineModes.Edit) {
            info.rbody.transform.position += new Vector3(Time.deltaTime * info.status.combinedDirection.x * speed, Time.deltaTime * info.status.combinedDirection.y * speed, 0f);
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