using System;
using System.Linq;
using HandyJellyfish.Audio;
using UnityEngine;

namespace RetroSunset
{
    public class Scaler : BeatReactor
    {
        [SerializeField]Vector3 beatScale = Vector3.one;
        
        Vector3 baseScale;
        Vector3 scale;
        
        protected override void Start()
        {
            baseScale = transform.localScale;
            base.Start();
        }

        void Update()
        {
            // TODO: Bring in an animation to scale back the beat on reaction - steal from mountains code
            transform.localScale = baseScale + scale;
        }

        protected override void ProcessBeat(float beatIntensity)
        {
            scale = beatScale * beatIntensity;
        }
    }
}