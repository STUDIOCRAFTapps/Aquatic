using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    public RigidbodyPixel rbody;
    public SpriteRenderer spriteRenderer;
    public AnimationState[] animationStates;

    AnimationState currentState;
    float cS_StartFrame = 0f;
    Dictionary<string, AnimationState> idToState;

    private void Start () {
        idToState = new Dictionary<string, AnimationState>();
        currentState = animationStates[0];

        for(int i = 0; i < animationStates.Length; i++) {
            idToState.Add(animationStates[i].stateID, animationStates[i]);
        }
    }

    private void Update () {
        float t = Time.time - cS_StartFrame;
        int frameInd = 0;
        if(currentState.loop) {
            frameInd = Mathf.FloorToInt(Mathf.Repeat(t * currentState.GetTimeScaler() + currentState.startingFrame, currentState.frames.Length));
        } else {
            frameInd = Mathf.FloorToInt(Mathf.Clamp(t * currentState.GetTimeScaler(), 0f, currentState.frames.Length - 0.5f));
        }

        spriteRenderer.sprite = currentState.frames[frameInd];
    }

    public void ChangeState (string stateID) {
        if(currentState == idToState[stateID]) {
            return;
        }
        currentState = idToState[stateID];
        cS_StartFrame = Time.time;
    }
}

[System.Serializable]
public class AnimationState {
    public string stateID;
    public Sprite[] frames;
    public float millisec = 100f;
    public bool loop = true;
    public float startingFrame = 0f;

    private float timeScale = -1f;

    public float GetTimeScaler () {
        if(timeScale < 0) {
            timeScale = (millisec == 0f) ? 0f : 1000f / millisec;
        }
        return timeScale;
    }
}
