using System;
using System.Linq;
using HandyJellyfish.Audio;
using UnityEngine;

namespace RetroSunset
{
    public class Colourer : MonoBehaviour
    {
        [SerializeField]MeshRenderer renderer;

        [SerializeField]AudioProcessor processor;
        [SerializeField]int band;
        
        [SerializeField]bool r = true;
        [SerializeField]bool g = true;
        [SerializeField]bool b = true;
        
        Color baseColor;
        Color beatColor;
        
        void Start()
        {
            processor.OnBeat.AddListener(Scale);
            baseColor = renderer.material.GetColor("_Albedo");
        }

        void Update()
        {
            renderer.material.SetColor("_Albedo", baseColor * beatColor);
        }
        void Scale(int[] beats, float[] sample)
        {
            if (beats.Contains(band))
                beatColor = new Color(r ? sample[band] : 1, g ? sample[band] : 1, b ? sample[band] : 1);
            else
                beatColor = Color.white;
        }
    }
}