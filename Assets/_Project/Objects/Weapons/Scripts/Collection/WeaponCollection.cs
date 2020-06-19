using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Collection", menuName = "Combat/Weapon/Collection")]
public class WeaponCollection : ScriptableObject {
    new public string name;

    public BaseWeapon[] items = null;

    public void BuildDictionary () {

    }
}
