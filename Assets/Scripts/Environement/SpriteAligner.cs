using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAligner : MonoBehaviour {
    public Transform localObjectTransform;
    public Transform localSpriteTransform;
    public RigidbodyPixel rb;
    public Vector3 defaultPos;
    public bool alignToWorld;

    bool isFollowingParent = false;

    private void Start () {
        defaultPos = localSpriteTransform.localPosition;
    }

    private void Update () {
        if(alignToWorld) {
            Vector3 spritePosition = localObjectTransform.position;
            float wtp = TerrainManager.inst.worldToPixel;
            float subX = spritePosition.x * wtp - Mathf.Round(spritePosition.x * wtp);
            float subY = spritePosition.y * wtp - Mathf.Round(spritePosition.y * wtp);

            localSpriteTransform.localPosition = new Vector3(-subX * TerrainManager.inst.pixelToWorld, -subY * TerrainManager.inst.pixelToWorld, defaultPos.z);
            return;
        }
        if(!isFollowingParent && rb.IsParented()) {
            isFollowingParent = true;
        } else if(isFollowingParent && !rb.IsParented()) {
            isFollowingParent = false;
        }

        if(isFollowingParent) {
            float wtp = TerrainManager.inst.worldToPixel;
            Vector3 parentPosition = rb.GetParentPosition();
            Vector3 spritePosition = localObjectTransform.position;
            float psubX = parentPosition.x * wtp - Mathf.Round(parentPosition.x * wtp);
            float psubY = (parentPosition.y * wtp) - Mathf.Round(parentPosition.y * wtp) - 1f;
            float subX = Mathf.Repeat(spritePosition.x * wtp - psubX, 1f) + psubX;
            float subY = Mathf.Repeat(spritePosition.y * wtp - psubY, 1f) + psubY;

            localSpriteTransform.localPosition = new Vector3(-subX * TerrainManager.inst.pixelToWorld, -subY * TerrainManager.inst.pixelToWorld, parentPosition.z + defaultPos.z);
        } else if(rb.isInsideComplexObject != null && !rb.reorderChild) {
            Vector3 parentPosition = rb.isInsideComplexObject.GetPosition();
            localSpriteTransform.localPosition = new Vector3(defaultPos.x, defaultPos.y, parentPosition.z + defaultPos.z);
        } else {
            localSpriteTransform.localPosition = defaultPos;
        }
    }
}
