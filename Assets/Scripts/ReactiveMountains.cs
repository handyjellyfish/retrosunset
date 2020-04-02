using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace RetroSunset
{
    public class ReactiveMountains : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] DynamicGrid dynamicGrid;

        [SerializeField] int samples = 128;
        [SerializeField] int channel = 0;

        [SerializeField] bool invert = false;

        List<ushort> heightMap;

        void Start()
        {
            heightMap = new List<ushort>(samples * samples);
            for(var i = 0; i < samples * samples; i++)
                heightMap.Add(0);

            StartCoroutine(GenerateRow());
	}

        IEnumerator GenerateRow()
        {
            while (true)
            {
                var sampleData = new float[samples];
                audioSource.GetSpectrumData(sampleData, 0, FFTWindow.BlackmanHarris);

                // for (int i = 1; i < sampleData.Length - 1; i++)
                // {
                //     Debug.DrawLine(new Vector3(i - 1, sampleData[i] + 10, 0), new Vector3(i, sampleData[i + 1] + 10, 0), Color.red);
                //     Debug.DrawLine(new Vector3(i - 1, Mathf.Log(sampleData[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(sampleData[i]) + 10, 2), Color.cyan);
                //     Debug.DrawLine(new Vector3(Mathf.Log(i - 1), sampleData[i - 1] - 10, 1), new Vector3(Mathf.Log(i), sampleData[i] - 10, 1), Color.green);
                //     Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(sampleData[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(sampleData[i]), 3), Color.blue);
                // }
            
                heightMap.RemoveAt(samples*samples - samples);
                for (var i = 0; i < samples; i++)
                    heightMap.Insert(0, Mathf.FloatToHalf(sampleData[invert ? samples - (i + 1) : i]));
                
                dynamicGrid.SetHeightMap(samples, samples, heightMap.ToArray(), 1250);
                yield return null; // new WaitForSeconds(60/bpm);
            }
        }
    }
}