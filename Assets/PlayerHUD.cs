using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour {

    public static PlayerHUD inst;
    public GameObject hudRoot;

    [Header("Health")]
    public Transform healthContainer;
    public Transform healthIconTemplate;
    private List<Transform> healthContainerItems = new List<Transform>();
    private List<Image> healthIcons = new List<Image>();

    [Header("Weapons")]
    public float maxSelectorsVisibleTime = 4f;
    public float selectorsFadeLength = 0.5f;
    public float slideLength = 1f;
    public RectTransform weaponSelections;
    public Image mainWeaponUI;
    public Transform mainContentParent;
    public Image[] mainContentIcons;
    public Image secondWeaponUI;
    public Transform secondContentParent;
    public Image[] secondContentIcons;

    private void Awake () {
        inst = this;

        GameManager.inst.OnChangeEngineMode += OnChangeEngineMode;
        OnChangeEngineMode();

        BuildHealthContainer(5);

        startSelectionsPos = weaponSelections.anchoredPosition;
        weaponSelections.anchoredPosition = Vector2.zero;
    }
    
    private void OnChangeEngineMode () {
        hudRoot.SetActive(GameManager.inst.engineMode == EngineModes.Play);
    }

    #region Health
    public void BuildHealthContainer (int iconCount) {
        foreach(Transform items in healthContainerItems) {
            Destroy(items.gameObject);
        }
        healthContainerItems.Clear();
        healthIcons.Clear();

        for(int i = 0; i < iconCount; i++) {
            Transform newHealthIcon = Instantiate(healthIconTemplate, healthContainer);
            newHealthIcon.gameObject.SetActive(true);
            healthContainerItems.Add(newHealthIcon);
            healthIcons.Add(newHealthIcon.GetChild(0).GetComponent<Image>());
            healthIcons[i].fillAmount = 1f;
        }
    }

    public void UpdateHealth (float healthPoints) {
        float health = healthPoints / PlayerController.healthPerHearts;

        for(int i = 0; i < healthIcons.Count; i++) {
            if(i + 1 <= health) {
                healthIcons[i].fillAmount = 1f;
            } else if(i + 1 > health && i < health) {
                healthIcons[i].fillAmount = Mathf.FloorToInt(Mathf.Repeat(health, 1f) * 16f) * 0.0625f;
            } else {
                healthIcons[i].fillAmount = 0f;
            }
        }
    }
    #endregion

    #region Weapon Select
    int selectedMainWeaponIndex = 0;
    int selectedSecondWeaponIndex = 0;
    float slideMainTime;
    float slideSecondTime;
    int slideMainDir;
    int slideSecondDir;
    float selectorTimer = 0f;
    float selectorFadeTimer = 0f;
    int selectorFadeDir = 1;
    Vector2 startSelectionsPos;

    private void Update () {
        SlideAnimationMain();
        SlideAnimationSecond();

        SelectionTimer();
    }

    void SelectionTimer () {
        if(selectorFadeTimer > 0f) {
            if(selectorFadeDir > 0) {
                weaponSelections.anchoredPosition = Vector2.Lerp(startSelectionsPos, new Vector2(0f, startSelectionsPos.y), selectorFadeTimer / selectorsFadeLength);
            } else {
                weaponSelections.anchoredPosition = Vector2.Lerp(new Vector2(0f, startSelectionsPos.y), startSelectionsPos, selectorFadeTimer / selectorsFadeLength);
            }
            selectorFadeTimer -= Time.deltaTime;
            return;
        } else if(selectorFadeTimer < 0f) {
            selectorFadeTimer = 0f;

            if(selectorFadeDir > 0) {
                weaponSelections.anchoredPosition = startSelectionsPos;
                selectorTimer = maxSelectorsVisibleTime;
            } else {
                weaponSelections.anchoredPosition = new Vector2(0f, startSelectionsPos.y);
            }
            return;
        }
        
        if(selectorTimer < 0f) {

            selectorTimer = 0f;
            HideSelection();
        } else if(selectorTimer > 0f) {
            selectorTimer -= Time.deltaTime;
        }
    }

    void ShowSelection (PlayerCombatController cc) {
        if(selectorTimer > 0) {
            selectorTimer = maxSelectorsVisibleTime;
        } else if(selectorFadeTimer > 0 && selectorFadeDir == -1) {
            selectorFadeTimer = selectorsFadeLength - selectorFadeTimer;
            selectorFadeDir = 1;
        } else if(selectorTimer == 0f && selectorFadeTimer == 0f) {
            selectorFadeTimer = selectorsFadeLength;
            selectorFadeDir = 1;

            SlideMain(cc, 0);
            SlideSecond(cc, 0);
        }
    }

    void HideSelection () {
        selectorFadeTimer = selectorsFadeLength;
        selectorFadeDir = -1;
    }

    public void MainWeaponSelectionSlide (PlayerCombatController cc, BaseWeapon current, int dir) {
        ShowSelection(cc);
        if(selectorTimer == 0f) {
            return;
        }

        selectedMainWeaponIndex = current.gid;
        SlideMain(cc, dir);
    }

    public void SecondWeaponSelectionSlide (PlayerCombatController cc, BaseWeapon current, int dir) {
        ShowSelection(cc);
        if(selectorTimer == 0f) {
            return;
        }

        selectedSecondWeaponIndex = current.gid;
        SlideSecond(cc, dir);
    }

    void SlideMain (PlayerCombatController cc, int dir) {
        if(slideMainTime > 0f && dir != slideMainDir) {
            slideMainTime = slideLength - slideMainTime;
            slideMainDir = dir;

            selectedMainWeaponIndex = Mod(selectedMainWeaponIndex + dir, GeneralAsset.inst.GetWeaponCount());
            cc?.ChangeMainWeapon(GeneralAsset.inst.GetWeaponAssetFromGlobalID(selectedMainWeaponIndex));

        } else if(slideMainTime <= 0f) {
            int wc = GeneralAsset.inst.GetWeaponCount();

            selectedMainWeaponIndex = Mod(selectedMainWeaponIndex + dir, GeneralAsset.inst.GetWeaponCount());
            cc?.ChangeMainWeapon(GeneralAsset.inst.GetWeaponAssetFromGlobalID(selectedMainWeaponIndex));

            int shift = (dir < 0) ? 1 : 0;
            mainContentIcons[0].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex + 1 + shift, wc)).icon;
            mainContentIcons[1].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex + 0 + shift, wc)).icon;
            mainContentIcons[2].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex - 1 + shift, wc)).icon;
            mainContentIcons[3].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex - 2 + shift, wc)).icon;

            if(dir != 0) {
                slideMainTime = slideLength;
                slideMainDir = dir;
            }

            if(dir > 0) {
                mainContentParent.localPosition = new Vector3(-30f, 0f);
            } else {
                mainContentParent.localPosition = new Vector3(0f, 0f);
            }
        }
    }

    void SlideSecond (PlayerCombatController cc, int dir) {
        if(slideSecondTime > 0f && dir != slideSecondDir) {
            slideSecondTime = slideLength - slideSecondTime;
            slideSecondDir = dir;

            selectedSecondWeaponIndex = Mod(selectedSecondWeaponIndex + dir, GeneralAsset.inst.GetWeaponCount());
            cc.ChangeSecondWeapon(GeneralAsset.inst.GetWeaponAssetFromGlobalID(selectedSecondWeaponIndex));

        } else if(slideSecondTime <= 0f) {
            int wc = GeneralAsset.inst.GetWeaponCount();

            selectedSecondWeaponIndex = Mod(selectedSecondWeaponIndex + dir, GeneralAsset.inst.GetWeaponCount());
            cc.ChangeSecondWeapon(GeneralAsset.inst.GetWeaponAssetFromGlobalID(selectedSecondWeaponIndex));

            int shift = (dir < 0) ? 1 : 0;
            secondContentIcons[0].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex + 1 + shift, wc)).icon;
            secondContentIcons[1].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex + 0 + shift, wc)).icon;
            secondContentIcons[2].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex - 1 + shift, wc)).icon;
            secondContentIcons[3].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex - 2 + shift, wc)).icon;

            if(dir != 0) {
                slideSecondTime = slideLength;
                slideSecondDir = dir;
            }

            if(dir > 0) {
                secondContentParent.localPosition = new Vector3(-24f, 0f);
            } else {
                secondContentParent.localPosition = new Vector3(0f, 0f);
            }
        }
    }

    int Mod (int x, int m) {
        return (x % m + m) % m;
    }

    void SlideAnimationMain () {
        if(slideMainTime > 0) {
            float time = (slideLength - slideMainTime) / slideLength;
            if(slideMainDir > 0) {
                mainContentParent.localPosition = new Vector3(Mathf.Lerp(-30f, 0f, Mathf.SmoothStep(0f, 1f, time)), 0f);
            } else {
                mainContentParent.localPosition = new Vector3(Mathf.Lerp(0f, -30f, Mathf.SmoothStep(0f, 1f, time)), 0f);
            }

            slideMainTime -= Time.deltaTime;
        } else if(slideMainTime < 0) {
            slideMainTime = 0f;
            mainContentParent.localPosition = new Vector3(0f, 0f);

            int wc = GeneralAsset.inst.GetWeaponCount();
            mainContentIcons[0].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex + 1, wc)).icon;
            mainContentIcons[1].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex + 0, wc)).icon;
            mainContentIcons[2].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex - 1, wc)).icon;
            mainContentIcons[3].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedMainWeaponIndex - 2, wc)).icon;
        }
    }

    void SlideAnimationSecond () {
        if(slideSecondTime > 0) {
            float time = (slideLength - slideSecondTime) / slideLength;
            if(slideSecondDir > 0) {
                secondContentParent.localPosition = new Vector3(Mathf.Lerp(-24f, 0f, Mathf.SmoothStep(0f, 1f, time)), 0f);
            } else {
                secondContentParent.localPosition = new Vector3(Mathf.Lerp(0f, -24f, Mathf.SmoothStep(0f, 1f, time)), 0f);
            }

            slideSecondTime -= Time.deltaTime;
        } else if(slideSecondTime < 0) {
            slideSecondTime = 0f;
            secondContentParent.localPosition = new Vector3(0f, 0f);

            int wc = GeneralAsset.inst.GetWeaponCount();
            secondContentIcons[0].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex + 1, wc)).icon;
            secondContentIcons[1].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex + 0, wc)).icon;
            secondContentIcons[2].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex - 1, wc)).icon;
            secondContentIcons[3].sprite = GeneralAsset.inst.GetWeaponAssetFromGlobalID(Mod(selectedSecondWeaponIndex - 2, wc)).icon;
        }
    }

    public void OnChangeWeapon (PlayerCombatController pc) {
        if(pc.mainWeapon == null) {
            if(mainWeaponUI != null) {
                mainWeaponUI.sprite = null;
                mainWeaponUI.enabled = false;
            }
        } else {
            if(mainWeaponUI != null) {
                mainWeaponUI.sprite = pc.mainWeapon.icon;
                mainWeaponUI.enabled = true;
            }
        }

        if(pc.secondWeapon == null) {
            if(secondWeaponUI != null) {
                secondWeaponUI.sprite = null;
                secondWeaponUI.enabled = false;
            }
        } else {
            if(secondWeaponUI != null) {
                secondWeaponUI.sprite = pc.secondWeapon.icon;
                secondWeaponUI.enabled = true;
            }
        }
    }
    #endregion
}
