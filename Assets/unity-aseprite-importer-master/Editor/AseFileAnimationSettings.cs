using UnityEngine;
using System;
using System.Collections.Generic;

namespace AsepriteImporter
{
    [System.Serializable]
    public class AseFileAnimationSettings
    {

        public AseFileAnimationSettings()
        {

        }

        public AseFileAnimationSettings(string name)
        {
            animationName = name;
        }

        [SerializeField] public string animationName;
        [SerializeField] public string about;
        [SerializeField] public PixelAnimationClipType animationType;
        [SerializeField] public string returnToOnEnd;
        [SerializeField] public List<PixelAnimationClipCallbacks> callbacks = new List<PixelAnimationClipCallbacks>();

        public override string ToString()
        {
            return animationName;
        }
    }
}