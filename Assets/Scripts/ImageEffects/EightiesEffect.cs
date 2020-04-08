using System;
using UnityEngine;

namespace RetroSunset.ImageEffects
{
    [ExecuteInEditMode]
    public class EightiesEffect : MonoBehaviour
    {
        [SerializeField] Material material;

        [Range(-0.05f, 0.05f)]
        [SerializeField] float aberrationX = 0;

        [Range(-0.05f, 0.05f)]
        [SerializeField] float aberrationY = 0;

        [Range(0, 1)]
        [SerializeField] float shakeFrequency = 0;

        Vector2 aberration;

        void Start()
        {
            aberration = new Vector2(aberrationX, aberrationY);
        }

        void Update()
        {
            if (shakeFrequency != 0 && UnityEngine.Random.value < shakeFrequency)
            {
                var shakeX = (UnityEngine.Random.value - 0.5f) * 2 * aberrationX;
                var shakeY = (UnityEngine.Random.value - 0.5f) * 2 * aberrationY;

                var x = aberration.x + shakeX;
                var y = aberration.y + shakeY;
                
                if (aberrationX > 0 && x > aberrationX || aberrationX < 0 && x < aberrationX) x = aberrationX;
                if (aberrationY > 0 && y > aberrationY || aberrationY < 0 && y < aberrationY) y = aberrationY;
                
                if (aberrationX > 0 && x < -aberrationX || aberrationX < 0 && x > -aberrationX) x = -aberrationX;
                if (aberrationY > 0 && y < -aberrationY || aberrationY < 0 && y > -aberrationY) y = -aberrationY;
                
                aberration = new Vector2(x, y);
            }

            // var aberrationX = UnityEngine.Random.Range(minAberrationX, maxAberrationX);
            // var aberrationY = UnityEngine.Random.Range(minAberrationY, maxAberrationY);

            if (material != null)
            {
                material.SetFloat("_AberrationX", aberration.x);
                material.SetFloat("_AberrationY", aberration.y);
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (material != null)
                Graphics.Blit(src, dest, material);
        }
    }
}