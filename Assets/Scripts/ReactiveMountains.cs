using System;
using System.Collections;
using System.Collections.Generic;
using HandyJellyfish.Audio;
using UnityEngine;

namespace RetroSunset
{
    public class ReactiveMountains : MonoBehaviour
    {
        [SerializeField] AudioProcessor audio;
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

            audio.OnSample.AddListener(GenerateRow);
	}

        void GenerateRow(float[] sampleData)
        {
            //while (true)
            //{
                //var sampleData = audio.bands; //new float[samples];
                //audio.GetSpectrumData(sampleData, 0, FFTWindow.BlackmanHarris);

                if (sampleData.Length == 0)
                {
                    return;
                    //continue;
                }
                
                float minValue = float.MaxValue;

                for (int i = 0; i < sampleData.Length; i++)
                {
                    if (sampleData[i] < minValue)
                        minValue = sampleData[i];
                    
                    if (i == 0 || i == sampleData.Length - 1)
                        continue;

                    Debug.DrawLine(new Vector3(i - 1, sampleData[i], 0), new Vector3(i, sampleData[i + 1], 0), Color.red);
                    Debug.DrawLine(new Vector3(i - 1, Mathf.Log(sampleData[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(sampleData[i]) + 10, 2), Color.cyan);
                    Debug.DrawLine(new Vector3(Mathf.Log(i - 1), sampleData[i - 1] - 10, 1), new Vector3(Mathf.Log(i), sampleData[i] - 10, 1), Color.green);
                    Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(sampleData[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(sampleData[i]), 3), Color.blue);
                }
            

                heightMap.RemoveAt(sampleData.Length*sampleData.Length - sampleData.Length);
                for (var i = 0; i < sampleData.Length; i++)
                {
                    var ix = invert ? sampleData.Length - (i + 1) : i;
                    var height = Mathf.FloatToHalf(sampleData[ix]) - Mathf.FloatToHalf(minValue);
                    heightMap.Insert(0, (ushort)height);
                }

                dynamicGrid.SetHeightMap(sampleData.Length, sampleData.Length, heightMap.ToArray(), 1250);
                //yield return null; // new WaitForSeconds(60/bpm);
            //}
        }
    }
}