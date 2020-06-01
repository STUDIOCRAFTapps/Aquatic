using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour {

    public Transform attackIndicator;

    public PlayerController ctrl;
    public BaseWeapon currentWeapon;
    WeaponPlayerData weaponData;

    private void Start () {
        OnChangeWeapon();
    }

    private void OnChangeWeapon () {
        weaponData = currentWeapon.CreateWeaponPlayerData();
    }

    void Update () {

        if(GameManager.inst.engineMode != EngineModes.Play) {
            attackIndicator.gameObject.SetActive(false);
            return;
        } else {
            attackIndicator.gameObject.SetActive(true);
        }


        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (pos - ctrl.GetHeadPosition()).normalized;
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        float dist = EntityManager.inst.RaycastEntities(new Ray2D(ctrl.GetHeadPosition(), dir));
        attackIndicator.position = (ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator.eulerAngles = Vector3.forward * angle;
        Vector2 truePos = (Vector3)(ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));

        if(Input.GetMouseButtonDown(0)) {
            currentWeapon.OnStartAttack(weaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButton(0)) {
            currentWeapon.OnHoldAttack(weaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButtonUp(0)) {
            currentWeapon.OnReleaseAttack(weaponData, dir, truePos, angle);
        }
    }
}
