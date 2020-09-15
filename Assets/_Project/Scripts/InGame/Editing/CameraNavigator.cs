using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class CameraNavigator : MonoBehaviour {

    public static CameraNavigator inst;

    public BackgroundManager backgroundManager;
    public PixelPerfectCamera pixelPerfect;

    public Vector2Int largeScreenTarget = Vector2Int.one;
    public Transform playerCenter;
    new public Camera camera;
    public float pixelPerUnit = 16;
    public float pixelScale = 2;
    public float moveSmooth = 1f;
    public float velocitySmooth = 1f;
    public float velocityFriction = 1f;

    public bool playerFollowMode;

    Vector3 camOrigin;
    Vector2 screenOrigin;
    Vector2 maintainedVelocity;
    Vector2Int initTargetRes;
    bool zoomIn = true;

    #region Events
    public delegate void OnApplyLerp ();
    public event OnApplyLerp OnApplyLerpEvent;

    public delegate void OnPostApplyLerp ();
    public event OnPostApplyLerp OnPostApplyLerpEvent;

    public delegate void OnRevertLerpHandler ();
    public event OnRevertLerpHandler OnRevertLerpEvent;

    public delegate void OnPostRevertLerpHandler ();
    public event OnPostRevertLerpHandler OnPostRevertLerpEvent;

    private void OnPostRender () {
        OnRevertLerpEvent?.Invoke();
        OnPostRevertLerpEvent?.Invoke();
        }
        #endregion

    private void Awake () {
        inst = this;

        initTargetRes = new Vector2Int(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY); 
    }

    private void Update () {
        if(!playerCenter) {
            return;
        }
        float blend = 1f - Mathf.Pow(1f - moveSmooth, Time.deltaTime * 30f);

        if(Input.GetKeyDown(KeyCode.M)) {
            backgroundManager.CreateLayers();
            playerFollowMode = !playerFollowMode;
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

    void PostLerpCameraUpdate () {
        if(!playerCenter) {
            return;
        }

        if(playerFollowMode) {
            maintainedVelocity = Vector2.zero;
            transform.position = new Vector3(playerCenter.position.x, playerCenter.position.y, transform.position.z);

            zoomIn = true;
        } else {
            zoomIn = false;
        }
    }

    private void LateUpdate () {
        OnApplyLerpEvent?.Invoke();
        PostLerpCameraUpdate();
        OnPostApplyLerpEvent?.Invoke();
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
