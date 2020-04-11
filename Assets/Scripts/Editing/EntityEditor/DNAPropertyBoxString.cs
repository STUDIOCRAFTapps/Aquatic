using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DNAPropertyBoxString : DNAPropertyBox {

    public TMP_InputField stringField;

    public override void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        base.Setup(name, indentSize, ref fieldPathReference);
    }

    public override void LoadData () {
        stringField.text = ((string)GetDataUsingPath(DNAEditorManager.inst.dataSource, DNAEditorManager.inst.dataSourceType)).ToString();
    }

    public override void ApplyData () {
        SetDataUsingPath(DNAEditorManager.inst.dataSource, stringField.text, DNAEditorManager.inst.dataSourceType);
    }
}
