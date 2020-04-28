using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DNAPropertyBoxInt : DNAPropertyBox {

    public TMP_InputField intField;

    public override void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        base.Setup(name, indentSize, ref fieldPathReference);
    }

    public override void LoadData () {
        intField.text = ((int)GetDataUsingPath(DNAEditorManager.inst.dataSource, DNAEditorManager.inst.dataSourceType)).ToString();
    }

    public override void ApplyData () {
        if(int.TryParse(intField.text, out int value)) {
            SetDataUsingPath(DNAEditorManager.inst.dataSource, value, DNAEditorManager.inst.dataSourceType);
        }
    }
}
