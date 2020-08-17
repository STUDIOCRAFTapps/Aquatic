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
    public BaseWeapon wearableWeapon;
    WeaponPlayerData wearableWeaponData;

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

    #region Changing Weapon
    public void ChangeMainWeapon (BaseWeapon weapon) {
        mainWeapon = weapon;
        OnChangeWeapon();
    }

    public void ChangeSecondWeapon (BaseWeapon weapon) {
        secondWeapon = weapon;
        OnChangeWeapon();
    }

    public void ChangeWearableWeapon (BaseWeapon weapon) {
        wearableWeapon = weapon;
        OnChangeWeapon();
    }

    private void OnChangeWeapon () {
        PlayerHUD.inst.OnChangeWeapon(this);

        if(mainWeapon != null) {
            mainWeaponData = mainWeapon.CreateWeaponPlayerData();
            mainWeaponData.owner = ctrl;
            mainWeaponData.attackSlot = AttackSlot.Main;
        }
        if(secondWeapon != null) {
            secondWeaponData = secondWeapon.CreateWeaponPlayerData();
            secondWeaponData.owner = ctrl;
            secondWeaponData.attackSlot = AttackSlot.Second;
        }
        if(wearableWeapon != null) {
            wearableWeaponData = wearableWeapon.CreateWeaponPlayerData();
            wearableWeaponData.owner = ctrl;
            wearableWeaponData.attackSlot = AttackSlot.Wearable;
        }
    }
    #endregion


    void Update () {
        if(GameManager.inst.engineMode != EngineModes.Play) {
            return;
        }

        if(ctrl.isControlledLocally) {
            if(Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.RightShift)) {
                PlayerHUD.inst.MainWeaponSelectionSlide(this, mainWeapon, -1);
            }
            if(Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightShift)) {
                PlayerHUD.inst.MainWeaponSelectionSlide(this, mainWeapon, 1);
            }
            if(Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.RightShift)) {
                PlayerHUD.inst.WearableSelectionSlide(this, wearableWeapon, -1);
            }
            if(Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightShift)) {
                PlayerHUD.inst.WearableSelectionSlide(this, wearableWeapon, 1);
            }
            if(Input.GetKey(KeyCode.UpArrow)) {
                PlayerHUD.inst.SecondWeaponSelectionSlide(this, secondWeapon, -1);
            }
            if(Input.GetKey(KeyCode.DownArrow)) {
                PlayerHUD.inst.SecondWeaponSelectionSlide(this, secondWeapon, 1);
            }
        }

        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (pos - ctrl.GetHeadPosition()).normalized;
        if(ctrl.isControlledLocally) {
            ctrl.status.lookDir = dir;
        } else {
            dir = ctrl.status.lookDir; // Todo; interpolation
        }

        // Set the indicator at the correct position and direction
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        float dist = EntityManager.inst.RaycastEntities(new Ray2D(ctrl.GetHeadPosition(), dir));
        attackIndicator.position = (ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator2.position = (ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator.eulerAngles = Vector3.forward * angle;
        Vector2 truePos = (Vector3)(ctrl.GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));

        if(ctrl.isControlledLocally) {
            // Updates the components of the main weapon. It will take care of managing itself
            mainWeapon.OnUpdateIndicators(ref mainWeaponData);
            mainWeapon.OnWeaponEquippedUpdate(ref mainWeaponData);
            if(Input.GetMouseButtonDown(0)) {
                mainWeapon.OnStartAttack(ref mainWeaponData, dir, truePos, angle);
            }
            if(Input.GetMouseButton(0)) {
                mainWeapon.OnHoldAttack(ref mainWeaponData, dir, truePos, angle);
            }
            if(Input.GetMouseButtonUp(0)) {
                mainWeapon.OnReleaseAttack(ref mainWeaponData, dir, truePos, angle);
            }

            // Do the same for the second weapon
            secondWeapon.OnUpdateIndicators(ref secondWeaponData);
            secondWeapon.OnWeaponEquippedUpdate(ref secondWeaponData);
            if(Input.GetMouseButtonDown(1)) {
                secondWeapon.OnStartAttack(ref secondWeaponData, dir, truePos, angle);
            }
            if(Input.GetMouseButton(1)) {
                secondWeapon.OnHoldAttack(ref secondWeaponData, dir, truePos, angle);
            }
            if(Input.GetMouseButtonUp(1)) {
                secondWeapon.OnReleaseAttack(ref secondWeaponData, dir, truePos, angle);
            }

            // ...And the wearable one
            wearableWeapon.OnUpdateIndicators(ref wearableWeaponData);
            wearableWeapon.OnWeaponEquippedUpdate(ref wearableWeaponData);
            if(Input.GetKeyDown(KeyCode.Q)) {
                wearableWeapon.OnStartAttack(ref wearableWeaponData, dir, truePos, angle);
            }
            if(Input.GetKey(KeyCode.Q)) {
                wearableWeapon.OnHoldAttack(ref wearableWeaponData, dir, truePos, angle);
            }
            if(Input.GetKeyUp(KeyCode.Q)) {
                wearableWeapon.OnReleaseAttack(ref wearableWeaponData, dir, truePos, angle);
            }
        }
    }
}
