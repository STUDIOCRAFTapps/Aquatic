using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class CameraNavigator : MonoBehaviour {

    public BackgroundManager backgroundManager;
    public PixelPerfectCamera pixelPerfect;

    new public Camera camera;
    public float pixelPerUnit = 16;
    public float pixelScale = 2;
    public float moveSmooth = 1f;
    public float velocitySmooth = 1f;
    public float velocityFriction = 1f;

    public bool playerFollowMode;

    public Transform playerCenter;

    Vector3 camOrigin;
    Vector2 screenOrigin;
    Vector2 maintainedVelocity;

    Vector2Int initTargetRes;

    private void Start () {
        initTargetRes = new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY); 
    }

    void Update () {
        float blend = 1f - Mathf.Pow(1f - moveSmooth, Time.deltaTime * 30f);

        if(Input.GetKeyDown(KeyCode.F)) {
            backgroundManager.CreateLayers();
            playerFollowMode = !playerFollowMode;
            backgroundManager.scaleFactor = playerFollowMode ? 0.0f : 0.5f;
        }

        if(playerFollowMode) {
            maintainedVelocity = Vector2.zero;
            transform.position = new Vector3(playerCenter.position.x, playerCenter.position.y, transform.position.z);//Vector3.Lerp(transform.position, new Vector3(playerCenter.position.x, playerCenter.position.y, transform.position.z), blend);
            pixelPerfect.refResolutionX = initTargetRes.x / 2;
            pixelPerfect.refResolutionY = initTargetRes.y / 2;
        } else {
            pixelPerfect.refResolutionX = initTargetRes.x;
            pixelPerfect.refResolutionY = initTargetRes.y;
        }

        if(Input.GetMouseButtonDown(2) && !playerFollowMode) {
            camOrigin = transform.position;
            screenOrigin = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }

        if(Input.GetMouseButton(2) && !playerFollowMode) {
            Vector2 currentScreenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 worldDelta = ScreenDeltaToWorldDelta(screenOrigin - currentScreenPos);
            Vector2 preMovePos = transform.position;
            transform.position = Vector3.Lerp(transform.position, camOrigin + new Vector3(worldDelta.x, worldDelta.y), blend);
            maintainedVelocity = (new Vector2(transform.position.x, transform.position.y) - preMovePos) / Time.deltaTime;
        } else {
            transform.position += new Vector3(maintainedVelocity.x, maintainedVelocity.y) * Time.deltaTime;
            maintainedVelocity *= (1f - Time.deltaTime) * velocityFriction;
        }
    }

    public Vector2 ScreenDeltaToWorldDelta (Vector2 screenDelta) {
        return screenDelta / (pixelPerfect.pixelRatio) / pixelPerUnit;
    }
}
