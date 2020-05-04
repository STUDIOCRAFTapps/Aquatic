using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New PixelAnimationGroup", menuName = "Entities/Animation/AnimationGroup")]
public class PixelAnimationGroup : ScriptableObject {
    public PixelAnimationClip[] clips;
    private Dictionary<string, PixelAnimationClip> clipByName;

    public void Initialize () {
        clipByName = new Dictionary<string, PixelAnimationClip>();
        foreach(PixelAnimationClip clip in clips) {
            clipByName.Add(clip.clipName, clip);
            clip.Initialize();
        }
    }

    public PixelAnimationClip GetClipByName (string name) {
        if(clips.Length == 0) {
            Debug.LogError("There's no clip in this group.");
            return null;
        }

        if(clipByName == null) {
            Debug.LogError("This pixel animation group hasn't been initialized. Please add it to the list of used pixel animations.");
        }
        if(clipByName.TryGetValue(name, out PixelAnimationClip clip)) {
            return clip;
        } else {
            Debug.LogError($"Clip \"{name}\" not found. Returned clip[0] instead");
            return clips[0];
        }
    }
}