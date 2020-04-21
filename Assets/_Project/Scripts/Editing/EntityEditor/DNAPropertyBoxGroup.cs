using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DNAPropertyBoxGroup : DNAPropertyBox {
    public List<DNAPropertyBox> childs;
    public bool isOpen = false;

    public Image buttonImage;

    public override void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        base.Setup(name, indentSize, ref fieldPathReference);
    }

    public void Toggle () {
        if(isOpen) {
            Close();
        } else {
            Open();
        }

        isOpen = !isOpen;
    }

    public void Open () {
        buttonImage.sprite = DNAEditorManager.inst.groupClose;

        foreach(DNAPropertyBox dpb in childs) {
            dpb.gameObject.SetActive(true);
        }
    }

    public void Close () {
        buttonImage.sprite = DNAEditorManager.inst.groupOpen;

        foreach(DNAPropertyBox dpb in childs) {
            dpb.gameObject.SetActive(false);
        }
    }
}
