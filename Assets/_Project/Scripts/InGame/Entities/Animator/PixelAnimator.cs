using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelAnimator : MonoBehaviour {
    public SpriteRenderer targetGraphic;
    public PixelAnimationGroup animationGroup;
    public MonoBehaviour callbackReciever;

    PixelAnimationClip clip;
    float timeOfStartClip = 0f;
    int currentFrame = 0;
    float timeOfStartFrame = 0f;

    void Update () {
        if(clip == null) {
            return;
        }

        float frameStartTime = Time.time - timeOfStartFrame;
        float clipStartTime = Time.time - timeOfStartClip;

        if(clip.clipType == PixelAnimationClipType.OneTime && clipStartTime > clip.totalClipTime) {
            if(!string.IsNullOrEmpty(clip.returnToOnEnd)) {
                PlayClip(clip.returnToOnEnd);
            }
            if(clip.callbacks.TryGetValue(clip.frames.Length, out ushort code)) {
                ((IPixelAnimationCallbackReciever)callbackReciever)?.OnRecieveCallback(code);
            }
        } else if(frameStartTime > clip.secondsPerFrames[currentFrame]) {
            if(clip.callbacks.TryGetValue(currentFrame, out ushort code)) {
                ((IPixelAnimationCallbackReciever)callbackReciever)?.OnRecieveCallback(code);
            }
            currentFrame = Modulo(currentFrame + 1, clip.secondsPerFrames.Length);

            timeOfStartFrame = Time.time;
            targetGraphic.sprite = clip.frames[currentFrame];
        }
    }

    public void PlayClip (string clipName) {
        PixelAnimationClip newClip = animationGroup.GetClipByName(clipName);
        if(newClip == null) {
            return;
        }

        timeOfStartClip = Time.time;
        timeOfStartFrame = Time.time;
        currentFrame = 0;
        clip = newClip;
    }

    public void PlayClipWithoutRestart (string clipName) {
        if(clip != null) {
            if(clip.clipName == clipName) {
                return;
            }
        }

        PixelAnimationClip newClip = animationGroup.GetClipByName(clipName);
        if(newClip == null) {
            return;
        }

        timeOfStartClip = Time.time;
        timeOfStartFrame = Time.time;
        currentFrame = 0;
        clip = newClip;
    }

    public void PlayClipIfIsLoop (string clipName) {
        if(clip != null) {
            if(clip.clipType == PixelAnimationClipType.OneTime) {
                return;
            }
            if(clip.clipName == clipName) {
                return;
            }
        }

        PixelAnimationClip newClip = animationGroup.GetClipByName(clipName);
        if(newClip == null) {
            return;
        }

        timeOfStartClip = Time.time;
        timeOfStartFrame = Time.time;
        currentFrame = 0;
        clip = newClip;
    }

    public PixelAnimationClip GetCurrentClip () {
        return clip;
    }

    static int Modulo (int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}

public interface IPixelAnimationCallbackReciever {
    void OnRecieveCallback (uint code);
}
