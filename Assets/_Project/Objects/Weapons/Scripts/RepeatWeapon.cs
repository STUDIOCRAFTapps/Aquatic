using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Repeat Weapon", menuName = "Combat/Weapon/Repeat")]
public class RepeatWeapon : BaseWeapon {

    public BaseAttackStrikeAsset baseAttackStrikeAsset;
    public bool doStrikeUseRotation = true;
    public float cooldown = 0.1f;

    public override void OnStartAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        data.pressTime = 0f;
    }

    public override void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        data.pressTime += Time.deltaTime;
        if(data.attackTime > cooldown) {
            data.attackTime = 0f;

            CombatManager.inst.SpawnStrike(
                data.owner, baseAttackStrikeAsset.GetPrefabID(), baseAttackStrikeAsset.GetData(),
                pos, angle, dir,
                Vector2.zero
            );
        }
    }

    public override void OnWeaponEquippedUpdate (ref WeaponPlayerData data) {
        data.attackTime += Time.deltaTime;
    }

    public override void OnUpdateIndicators (ref WeaponPlayerData data) {
        float timeDiff = Time.time - data.attackTime;
        data.owner.hud?.SetWeaponValue(data.attackSlot == AttackSlot.Main, cooldown == 0f ? 0f : timeDiff / cooldown);
    }
}
