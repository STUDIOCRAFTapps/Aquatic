using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPlayerData {
    public PlayerController owner;
    public float pressTime;
    public float attackTime;
    public AttackSlot attackSlot; 
}

public enum AttackSlot {
    Main,
    Second,
    Wearable
}

public abstract class BaseWeapon : ScriptableObject {

    new public string name;
    public string id;
    public Sprite icon;
    public WeaponTypes type;

    [HideInInspector] public int gid;

    /// <summary>
    /// Called once the weapon has been equiped. This can be used, for instance, to instantiate the anchor before it's used.
    /// </summary>
    public virtual void OnEquipWeapon () {
    }

    /// <summary>
    /// Called once the weapon has been unequiped. This can be used, for instance, to remove the anchor after it's been used.
    /// </summary>
    public virtual void OnUnequipWeapon () {
    }

    /// <summary>
    /// Called the frame the player presses the attack button
    /// </summary>
    public virtual void OnStartAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    /// <summary>
    /// Called for every normal (not fixed) frame the player holds the attack button
    /// </summary>
    public virtual void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    /// <summary>
    /// Called the frame the player releases the attack button
    /// </summary>
    public virtual void OnReleaseAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    public virtual void OnWeaponEquippedUpdate (ref WeaponPlayerData data) {

    }

    public virtual void OnUpdateIndicators (ref WeaponPlayerData data) {

    }

    public virtual WeaponPlayerData CreateWeaponPlayerData () {
        return new WeaponPlayerData();
    }
}

public enum WeaponTypes {
    ClickOnce, //Instant attack, must release to attack again
    ClickHold, //Instant attack, hold to attack again at fixed interval
    ChargeOnce, //Attack on release only, after holding for a certain time
    ChargeHold  //Attack after a fixed time, then repeatly attack after a different fixed time
}
