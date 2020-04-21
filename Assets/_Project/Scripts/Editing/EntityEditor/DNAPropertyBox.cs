using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;

public class DNAPropertyBox : MonoBehaviour {
    public TextMeshProUGUI nameText;
    public RectTransform indent;

    public List<string> fieldPath;

    public virtual void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        if(fieldPath == null) {
            fieldPath = new List<string>();
        }
        fieldPath.Clear();

        nameText.text = name;
        indent.sizeDelta = new Vector2(indentSize, indent.sizeDelta.y);

        for(int i = 0; i < fieldPathReference.Count; i++) {
            fieldPath.Add(fieldPathReference[i]);
        }
    }

    public virtual void LoadData () {

    }

    public virtual void ApplyData () {

    }

    public object GetDataUsingPath (object source, Type sourceType) {
        FieldInfo fi;
        Type typeChain = sourceType;

        object objectChain = source;
        for(int i = 0; i < fieldPath.Count - 1; i++) {
            fi = typeChain.GetField(fieldPath[i], BindingFlags.Public | BindingFlags.Instance);
            typeChain = fi.FieldType;
            objectChain = fi.GetValue(objectChain);
        }
        fi = typeChain.GetField(fieldPath[fieldPath.Count - 1], BindingFlags.Public | BindingFlags.Instance);
        return fi.GetValue(objectChain);
    }

    public void SetDataUsingPath (object destination, object value, Type sourceType) {
        FieldInfo fi;
        Type typeChain = sourceType;
        object objectChain = destination;
        object parentObjectChain = null;
        for(int i = 0; i < fieldPath.Count - 1; i++) {
            fi = typeChain.GetField(fieldPath[i], BindingFlags.Public | BindingFlags.Instance);
            typeChain = fi.FieldType;
            parentObjectChain = objectChain;
            objectChain = fi.GetValue(objectChain);
        }

        // Check if struct
        if(typeChain.IsValueType && !typeChain.IsEnum && !typeChain.IsPrimitive) {
            fi = typeChain.GetField(fieldPath[fieldPath.Count - 1], BindingFlags.Public | BindingFlags.Instance);
            fi.SetValue(objectChain, value);
            
            fi = parentObjectChain.GetType().GetField(fieldPath[fieldPath.Count - 2], BindingFlags.Public | BindingFlags.Instance);
            fi.SetValue(parentObjectChain, objectChain);
        } else {
            fi = typeChain.GetField(fieldPath[fieldPath.Count - 1], BindingFlags.Public | BindingFlags.Instance);
            fi.SetValue(objectChain, value);
        }
    }
}
