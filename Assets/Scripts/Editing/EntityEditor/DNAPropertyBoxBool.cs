using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DNAPropertyBoxBool : DNAPropertyBox {

    public Toggle boolField;

    public override void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        base.Setup(name, indentSize, ref fieldPathReference);
    }

    public override void LoadData () {
        boolField.isOn = (bool)GetDataUsingPath(DNAEditorManager.inst.dataSource, DNAEditorManager.inst.dataSourceType);
    }

    public override void ApplyData () {
        SetDataUsingPath(DNAEditorManager.inst.dataSource, boolField.isOn, DNAEditorManager.inst.dataSourceType);
    }
}
