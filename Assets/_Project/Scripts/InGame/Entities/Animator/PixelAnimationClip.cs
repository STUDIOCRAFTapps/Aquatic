using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PixelAnimationClipType {
    Loop,
    OneTime
}

[System.Serializable]
public struct PixelAnimationClipCallbacks {
    public int frame;
    public ushort callbackCode;
}

[CreateAssetMenu(fileName = "New PixelAnimationGroup", menuName = "Entities/Animation/AnimationGroup")]
public class PixelAnimationClip : ScriptableObject {
    public string clipName;
    public PixelAnimationClipType clipType;
    public string returnToOnEnd;

    public Sprite[] frames;
    public float[] secondsPerFrames;
    public float totalClipTime {
        get; private set;
    }
    public float[] timeAtFrame {
        get; private set;
    }
    public Dictionary<int, ushort> callbacks {
        get; private set;
    }
    public List<PixelAnimationClipCallbacks> callbackList;

    public PixelAnimationClip (PixelAnimationClipType clipType, Sprite[] frames, float[] secondsPerFrames) {
        this.clipType = clipType;
        this.frames = frames;
        this.secondsPerFrames = secondsPerFrames;
    }

    public void Initialize () {
        totalClipTime = 0f;
        timeAtFrame = new float[secondsPerFrames.Length];

        for(int i = 0; i < secondsPerFrames.Length; i++) {
            timeAtFrame[i] = totalClipTime;
            totalClipTime += secondsPerFrames[i];
        }

        callbacks = new Dictionary<int, ushort>();
        for(int i = 0; i < callbackList.Count; i++) {
            callbacks.Add(callbackList[i].frame, callbackList[i].callbackCode);
        }
    }
}
