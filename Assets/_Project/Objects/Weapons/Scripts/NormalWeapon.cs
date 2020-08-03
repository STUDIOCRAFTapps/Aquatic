using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Normal Weapon", menuName = "Combat/Weapon/Normal")]
public class NormalWeapon : BaseWeapon {

    public BaseAttackStrikeAsset baseAttackStrikeAsset;
    public bool doStrikeUseRotation = true;
    public float cooldown = 0.1f;

    public override void OnStartAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        float timeDiff = Time.time - data.lastAttackTime;
        data.timeOfPress = Time.time;
        
        if(timeDiff > cooldown) {
            data.lastAttackTime = Time.time;

            CombatManager.inst.SpawnStrike(
                data.owner, baseAttackStrikeAsset.GetPrefabID(), baseAttackStrikeAsset.GetData(),
                pos, angle, dir,
                Vector2.zero
            );
        }
    }

    public override void OnUpdateIndicators (ref WeaponPlayerData data) {
        float timeDiff = Time.time - data.lastAttackTime;
        data.owner.hud?.SetWeaponValue(data.attackSlot == AttackSlot.Main, cooldown == 0f ? 0f : timeDiff / cooldown);
    }
}
