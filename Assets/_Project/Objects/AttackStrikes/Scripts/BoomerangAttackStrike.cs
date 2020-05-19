using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangAttackStrike : AttackStrike {

    float speed = 0f;

    public override void Init (PlayerController owner, BaseAttackStrikeAsset asset, Vector2 additionnalAttVel, Vector2 direction) {
        base.Init(owner, asset, additionnalAttVel, direction);
        speed = ((BoomerangAttackStrikeAsset)asset).speed;
    }

    void Update () {
        if(!gameObject.activeSelf) {
            return;
        }
        transform.position += (Vector3)dir * speed * Time.deltaTime;
        speed += ((BoomerangAttackStrikeAsset)asset).acceleration * Time.deltaTime;
    }
}
