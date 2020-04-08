using System;
using System.Linq;
using HandyJellyfish.Audio;
using UnityEngine;

namespace RetroSunset
{
    public class Scaler : MonoBehaviour
    {
        [SerializeField]AudioProcessor processor;
        [SerializeField]int band;
        
        [Range(0, 10)]
        [SerializeField]float minIntensity = 0;
        
        [Range(1, 10)]
        [SerializeField]float beatImpact = 1.1f;

        [SerializeField]bool x = true;
        [SerializeField]bool y = true;
        [SerializeField]bool z = true;
        
        Vector3 baseScale;
        Vector3 beatScale;
        
        void Start()
        {
            processor.OnBeat.AddListener(Scale);
            baseScale = transform.localScale;
        }

        void Update()
        {
            transform.localScale = baseScale + beatScale;
        }
        void Scale(int[] beats, float[] sample)
        {
            if (beats.Contains(band) && sample[band] > minIntensity)
                beatScale = new Vector3(sample[band]*(x ? 1 : 0), sample[band]*(y ? 1 : 0), sample[band]*(z ? 1 : 0)) * beatImpact;
            else
                beatScale = Vector3.zero;
        }
    }
}