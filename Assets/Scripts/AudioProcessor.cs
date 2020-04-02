using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HandyJellyfish.Audio
{
    public class AudioProcessor : MonoBehaviour
    {
        [SerializeField] AudioSource audio;
        

        const int samples = 1024;
        float sampleTime;

        float[] energySamples;

        void Start()
        {
            Debug.Log("Audio Channels: " + audio.clip.channels);
            Debug.Log("Audio Frequency: " + audio.clip.frequency);

            sampleTime = samples/(float)audio.clip.frequency;
            energySamples = new float[audio.clip.frequency/samples];

            StartCoroutine(SampleAudio());
        }

        IEnumerator SampleAudio()
        {
            while (audio.isPlaying) // TODO: Monitor audio source playing.
            {
                var samplesLeft = new float[samples];
                var samplesRight = new float[samples];

                audio.GetOutputData(samplesLeft, 0);
                audio.GetOutputData(samplesRight, 1);

                var instantEnergy = 0.0f;

                for (var i = 0; i < samples; i++)
                {    
                    instantEnergy += samplesLeft[i]*samplesLeft[i] + samplesRight[i]*samplesRight[i];
                }

                var averageEnergy = 0.0f;
                for (var i = 0; i < energySamples.Length; i++)
                {
                    averageEnergy += energySamples[i]*energySamples[i];
                }

                averageEnergy /= energySamples.Length;

                var variance = 0.0f;
                for (var i = 0; i < energySamples.Length; i++)
                {
                    var localVariance = (energySamples[i] - averageEnergy);
                    variance += localVariance * localVariance;
                }

                variance /= energySamples.Length;

                var sensitivity = (-0.0025714f*variance) + 1.5142857f; // high energy variance = lower sensitivity
                Debug.Log(sensitivity);

                if (instantEnergy > averageEnergy * sensitivity)
                    Debug.Log("BBBBBBEAAAT");

                for (var i = energySamples.Length - 2; i >= 0; i--)
                {
                    energySamples[i+1] = energySamples[i]; // TODO: Could make this faster without the array.
                }

                energySamples[0] = instantEnergy;

                yield return new WaitForSeconds(sampleTime);
            }
        }
    }
}