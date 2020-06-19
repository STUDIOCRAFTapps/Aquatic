using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour {

    public Transform attackIndicator;
    public Transform attackIndicator2;
    public Image mainWeaponUI;
    public Image secondWeaponUI;

    public PlayerController ctrl;
    public BaseWeapon mainWeapon;
    WeaponPlayerData mainWeaponData;
    public BaseWeapon secondWeapon;
    WeaponPlayerData secondWeaponData;

    private void Start () {
        OnChangeWeapon();

        OnChangeEngineMode();
        GameManager.inst.OnChangeEngineMode += OnChangeEngineMode;
    }



    void OnChangeEngineMode () {
        ctrl.SetHealth(PlayerController.defaultHealth);

        if(GameManager.inst.engineMode != EngineModes.Play) {
            attackIndicator.gameObject.SetActive(false);
        } else {
            attackIndicator.gameObject.SetActive(true);
        }
    }

    public void ChangeMainWeapon (BaseWeapon weapon) {
        mainWeapon = weapon;
        OnChangeWeapon();
    }

    public void ChangeSecondWeapon (BaseWeapon weapon) {
        secondWeapon = weapon;
        OnChangeWeapon();
    }

    private void OnChangeWeapon () {
        PlayerHUD.inst.OnChangeWeapon(this);

        if(mainWeapon != null) {
            mainWeaponData = mainWeapon.CreateWeaponPlayerData();
        }
        if(secondWeapon != null) {
            secondWeaponData = secondWeapon.CreateWeaponPlayerData();
        }
    }



    void Update () {
        if(GameManager.inst.engineMode != EngineModes.Play) {
            return;
        }

        if(Input.GetKey(KeyCode.RightArrow)) {
            PlayerHUD.inst.MainWeaponSelectionSlide(this, mainWeapon, -1);
        }
        if(Input.GetKey(KeyCode.LeftArrow)) {
            PlayerHUD.inst.MainWeaponSelectionSlide(this, mainWeapon, 1);
        }
        if(Input.GetKey(KeyCode.UpArrow)) {
            PlayerHUD.inst.SecondWeaponSelectionSlide(this, secondWeapon, -1);
        }
        if(Input.GetKey(KeyCode.DownArrow)) {
            PlayerHUD.inst.SecondWeaponSelectionSlide(this, secondWeapon, 1);
        }

        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (pos - ctrl.GetHeadPosition()).normalized;
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        float dist = EntityManager.inst.RaycastEntities(new Ray2D(ctrl.GetHeadPosition(), dir));
        attackIndicator.position = (ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator2.position = (ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator.eulerAngles = Vector3.forward * angle;
        Vector2 truePos = (Vector3)(ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));

        if(Input.GetMouseButtonDown(0)) {
            mainWeapon.OnStartAttack(ref mainWeaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButton(0)) {
            mainWeapon.OnHoldAttack(ref mainWeaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButtonUp(0)) {
            mainWeapon.OnReleaseAttack(ref mainWeaponData, dir, truePos, angle);
        }

        if(Input.GetMouseButtonDown(1)) {
            secondWeapon.OnStartAttack(ref secondWeaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButton(1)) {
            secondWeapon.OnHoldAttack(ref secondWeaponData, dir, truePos, angle);
        }
        if(Input.GetMouseButtonUp(1)) {
            secondWeapon.OnReleaseAttack(ref secondWeaponData, dir, truePos, angle);
        }
    }
}
