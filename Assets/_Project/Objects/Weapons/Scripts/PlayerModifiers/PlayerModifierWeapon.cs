using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModifierData : WeaponPlayerData {
}

[CreateAssetMenu(fileName = "New Player Modifier Weapon", menuName = "Combat/Weapon/PlayerModifier")]
public class PlayerModifierWeapon : BaseWeapon {

    public BasePlayerModule module;

    public override void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        // Call player to apply module once.
        data.owner.RunWearableModule((WearableModule)module, (PlayerModifierData)data);
    }
    
    public override WeaponPlayerData CreateWeaponPlayerData () {
        return new PlayerModifierData();
    }
}