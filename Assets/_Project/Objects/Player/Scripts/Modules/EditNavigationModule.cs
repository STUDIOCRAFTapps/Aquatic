using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEditNavigationModule", menuName = "Player/Modules/EditNavigation", order = -1)]
public class EditNavigationModule : BasePlayerModule {

    [Header("Parameters")]
    public float speed;

    public override void UpdateStatus (PlayerInfo info) {
        if(!info.status.isFlying) {
            return;
        }

        info.status.combinedDirection = info.status.dir;
    }

    public override void UpdateAction (PlayerInfo info) {
        if(!info.status.isFlying) {
            return;
        }

        info.rbody.transform.position += new Vector3(
            Time.deltaTime * info.status.combinedDirection.x * speed, 
            Time.deltaTime * info.status.combinedDirection.y * speed, 0f) * (info.status.jump > 0 ? 1.5f : 1f);
        info.rbody.velocity = Vector2.zero;
    }
}