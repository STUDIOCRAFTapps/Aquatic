using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class CameraNavigator : MonoBehaviour {

    public BackgroundManager backgroundManager;
    public PixelPerfectCamera pixelPerfect;

    public Vector2Int largeScreenTarget = Vector2Int.one;
    new public Camera camera;
    public float pixelPerUnit = 16;
    public float pixelScale = 2;
    public float moveSmooth = 1f;
    public float velocitySmooth = 1f;
    public float velocityFriction = 1f;

    public float ratioDebug = 0f;

    public bool playerFollowMode;

    public Transform playerCenter;

    Vector3 camOrigin;
    Vector2 screenOrigin;
    Vector2 maintainedVelocity;

    Vector2Int initTargetRes;

    bool zoomIn = true;

    private void Start () {
        initTargetRes = new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY); 
    }

    void Update () {
        float blend = 1f - Mathf.Pow(1f - moveSmooth, Time.deltaTime * 30f);

        if(Input.GetKeyDown(KeyCode.F)) {
            backgroundManager.CreateLayers();
            playerFollowMode = !playerFollowMode;
        }

        if(playerFollowMode) {
            maintainedVelocity = Vector2.zero;
            transform.position = new Vector3(playerCenter.position.x, playerCenter.position.y, transform.position.z);//Vector3.Lerp(transform.position, new Vector3(playerCenter.position.x, playerCenter.position.y, transform.position.z), blend);

            zoomIn = true;
        } else {
            zoomIn = false;
        }

        CalculateResolution();

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

    public void CalculateResolution () {
        Vector2Int screenRes = new Vector2Int(Screen.width, Screen.height);

        if(screenRes.x / 2 > initTargetRes.x || screenRes.y / 2 > initTargetRes.y) {
            //Debug.Log("Larger Screen");
            pixelPerfect.refResolutionX = largeScreenTarget.x;
            pixelPerfect.refResolutionY = largeScreenTarget.y;
            zoomIn = false;
        } else {
            //Debug.Log("Smaller Screen");
            pixelPerfect.refResolutionX = zoomIn ? initTargetRes.x / 2 : initTargetRes.x;
            pixelPerfect.refResolutionY = zoomIn ? initTargetRes.y / 2 : initTargetRes.y;
        }
        backgroundManager.scaleFactor = zoomIn ? 1f : 2f;
    }

    public Vector2 ScreenDeltaToWorldDelta (Vector2 screenDelta) {
        return screenDelta / (pixelPerfect.pixelRatio) / pixelPerUnit;
    }
}
