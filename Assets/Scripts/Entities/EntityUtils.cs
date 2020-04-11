using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class EntityString {
    public string nspace;
    public string id;

    public EntityString () { }

    public EntityString (string nspace, string id) {
        this.nspace = nspace;
        this.id = id;
    }
}