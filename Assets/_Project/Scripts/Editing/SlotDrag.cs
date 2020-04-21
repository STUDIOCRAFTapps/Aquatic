using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotDrag : MonoBehaviour {

    public Image icon;
    public RectTransform terrainEditorRect;
    public TerrainEditorUI teUI;
    Vector3 mouseDelta;
    public Button button;

    public bool isDragging = false;

    // Shitty slap fix because I can't force to drag
    private void Update () {
        if(isDragging) {
            Drag();

            if(!Input.GetMouseButton(0)) {
                EndDrag();
            }
        }
    }

    public void BeginDrag () {
        transform.SetParent(terrainEditorRect);
        transform.SetSiblingIndex(terrainEditorRect.childCount);
        mouseDelta = transform.position - Input.mousePosition;
        teUI.isDraggingSlot = true;
        isDragging = true;
    }

    public void Drag () {
        transform.position = Input.mousePosition + mouseDelta;
    }

    public void EndDrag () {
        isDragging = false;
        teUI.isDraggingSlot = false;
        teUI.DropTileOnHotbar(this);
    }
}
