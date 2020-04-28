using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DNAEditorManager : MonoBehaviour {

    public float indentSize;
    public DNAPropertyBox templateGroup;
    public DNAPropertyBox templateSingle;
    public DNAPropertyBox templateInt;
    public DNAPropertyBox templateString;
    public DNAPropertyBox templateBool;
    public RectTransform content;

    public Sprite groupOpen;
    public Sprite groupClose;

    List<DNAPropertyBox> allPropertyBoxes;
    DNAPropertyBoxGroup group;
    List<string> currentFieldPath;
    int indent = 0;

    public static DNAEditorManager inst;

    Entity target;
    public EntityData dataSource;
    public Type dataSourceType = null;

    Type previousDataType = null;

    public void Init () {
        inst = this;
        allPropertyBoxes = new List<DNAPropertyBox>();
        currentFieldPath = new List<string>();
    }

    public void Setup (Entity target) {
        this.target = target;
        Type dataType = target.entityData.GetType();

        // Copy data
        dataSourceType = dataType;
        dataSource = target.entityData;

        //Should check if target data is using the same type as the previous. Only load data if that's the case
        if(dataType != previousDataType) {

            // Clear UI
            foreach(DNAPropertyBox dpb in allPropertyBoxes) {
                Destroy(dpb.gameObject);
            }
            allPropertyBoxes.Clear();

            // Search all fields
            group = null;
            currentFieldPath.Clear();
            GetAllFieldsOfType(dataType);
        }
        previousDataType = dataType;
        
        // Prepare UI and Load Data
        foreach(DNAPropertyBox dpb in allPropertyBoxes) {
            if((dpb as DNAPropertyBoxGroup) != null) {
                ((DNAPropertyBoxGroup)dpb).Close();
            }
            dpb.LoadData();
        }
    }

    public void ApplyDNAData () {
        // Apply Data
        foreach(DNAPropertyBox dpb in allPropertyBoxes) {
            dpb.ApplyData();
        }
        target.LoadData(dataSource);
    }

    public void ReloadDNAData () {
        // Reload Data
        foreach(DNAPropertyBox dpb in allPropertyBoxes) {
            dpb.LoadData();
        }
    }

    public void GetAllFieldsOfType (Type type) {
        foreach(FieldInfo fi in type.GetFields(BindingFlags.Public | BindingFlags.Instance).ToList()) {
            currentFieldPath.Add(fi.Name);

            if(fi.FieldType.Namespace != "System") {
                DNAPropertyBox dpb = CreateNewPropertyBox(fi.Name, indent, PropertyBoxType.Group);
                allPropertyBoxes.Add(dpb);
                indent++;
                DNAPropertyBoxGroup tempGroup = group;
                group = (DNAPropertyBoxGroup)dpb;
                GetAllFieldsOfType(fi.FieldType);
                group = tempGroup;
                indent--;
            } else {
                Type fieldType = fi.FieldType;
                DNAPropertyBox dpb = null;
                if(fieldType == typeof(float)) {
                    dpb = CreateNewPropertyBox(fi.Name, indent, PropertyBoxType.Single);
                } else if(fieldType == typeof(int)) {
                    dpb = CreateNewPropertyBox(fi.Name, indent, PropertyBoxType.Int);
                } else if(fieldType == typeof(string)) {
                    dpb = CreateNewPropertyBox(fi.Name, indent, PropertyBoxType.String);
                } else if(fieldType == typeof(bool)) {
                    dpb = CreateNewPropertyBox(fi.Name, indent, PropertyBoxType.Bool);
                }

                if(dpb != null) {
                    group?.childs.Add(dpb);
                }
                allPropertyBoxes.Add(dpb);
            }

            currentFieldPath.RemoveAt(currentFieldPath.Count - 1);
        }
    }

    DNAPropertyBox CreateNewPropertyBox (string name, int indentCount, PropertyBoxType type) {
        DNAPropertyBox newPB = null;
        switch(type) {
            case PropertyBoxType.Group:
                newPB = Instantiate(templateGroup, content);
            break;
            case PropertyBoxType.Single:
                newPB = Instantiate(templateSingle, content);
            break;
            case PropertyBoxType.Int:
                newPB = Instantiate(templateInt, content);
            break;
            case PropertyBoxType.String:
                newPB = Instantiate(templateString, content);
            break;
            case PropertyBoxType.Bool:
                newPB = Instantiate(templateBool, content);
            break;
        }
        newPB.gameObject.SetActive(true);
        newPB.Setup(name, indentSize * indentCount, ref currentFieldPath);

        return newPB;
    }

    public enum PropertyBoxType {
        Group,
        Single,
        Int,
        String,
        Bool
    }
}
