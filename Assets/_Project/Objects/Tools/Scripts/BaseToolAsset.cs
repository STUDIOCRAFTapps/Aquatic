using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;

[CreateAssetMenu(fileName = "BaseTile", menuName = "Terrain/Tool/BaseTool")]
public class BaseToolAsset : ScriptableObject {

    public int gid = -1;

    [Header("Presentation")]
    public Sprite uiSprite;

    public virtual void OnPressed (ToolUseInfo info) {
    }

    public virtual void OnHold (ToolUseInfo info) {
    }

    public virtual void OnReleased (ToolUseInfo info) {
    }

    public virtual void OnSelection (ToolUseInfo info) {
    }


    public virtual void DecodeAction (BitReader reader) {
    }
}
