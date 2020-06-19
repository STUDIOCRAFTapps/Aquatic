using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangAttackStrike : AttackStrike {
    float speed = 0f;

    public override void Init (PlayerController owner, BaseAttackStrikeData data, Vector2 additionnalAttVel, Vector2 direction) {
        base.Init(owner, data, additionnalAttVel, direction);
        speed = ((BoomerangAttackStrikeData)data).speed;
    }

    void Update () {
        if(!gameObject.activeSelf) {
            return;
        }
        transform.position += (Vector3)dir * speed * Time.deltaTime;
        speed += ((BoomerangAttackStrikeData)data).acceleration * Time.deltaTime;
    }
}
