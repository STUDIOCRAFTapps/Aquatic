using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour {

    public GameObject hudRoot;
    public Transform healthContainer;
    public Transform healthIconTemplate;

    private List<Transform> healthContainerItems = new List<Transform>();
    private List<Image> healthIcons = new List<Image>();

    private void Awake () {
        GameManager.inst.OnChangeEngineMode += OnChangeEngineMode;

        BuildHealthContainer(5);
    }
    
    private void OnChangeEngineMode () {
        hudRoot.SetActive(GameManager.inst.engineMode == EngineModes.Play);
    }

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

    public void UpdateHealth (float health) {
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
}
