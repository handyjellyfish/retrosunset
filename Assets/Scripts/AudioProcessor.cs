using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Complex = System.Numerics.Complex;

namespace HandyJellyfish.Audio
{
    public class AudioProcessor : MonoBehaviour
    {
        [Serializable]
        public class SampleEvent : UnityEvent<float[]>
        {}

        [Serializable]
        public class BeatEvent : UnityEvent<int[], float[]>
        {}

        [SerializeField] AudioSource audio;
        [SerializeField] bool simpleBeat = true;

        const int samples = 1024;
        FFT fft = new FFT(samples);

        float sampleTime;

        float[] energySamples;
        float[,] bandEnergySamples;

        float[] samplesLeft;
        float[] samplesRight;
        
        public float[] bands;

        public SampleEvent OnSample = new SampleEvent();
        public BeatEvent OnBeat = new BeatEvent();

        void Start()
        {
            Debug.Log("Audio Channels: " + audio.clip.channels);
            Debug.Log("Audio Frequency: " + audio.clip.frequency);

            sampleTime = samples/(float)audio.clip.frequency;
            energySamples = new float[audio.clip.frequency/samples];
            bandEnergySamples = new float[64, audio.clip.frequency/samples];
            bands = new float[0];

            samplesLeft = new float[samples];
            samplesRight = new float[samples];

            StartCoroutine(SampleAudio());
        }

        IEnumerator SampleAudio()
        {
            while (audio.isPlaying) // TODO: Few more advanced controls to handle this.
            {
                audio.GetOutputData(samplesLeft, 0);
                audio.GetOutputData(samplesRight, 1);

                if (simpleBeat)
                    SimpleBeatDetection();
                else
                    FrequencyBeatDetection();
                
                yield return new WaitForSeconds(sampleTime);
            }
        }

        private void SimpleBeatDetection()
        {
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
            
            if (instantEnergy > averageEnergy * sensitivity)
                Debug.Log("BBBBBBEAAAT");

            for (var i = energySamples.Length - 2; i >= 0; i--)
            {
                energySamples[i+1] = energySamples[i]; // TODO: Could make this faster without the array.
            }

            energySamples[0] = instantEnergy;
        }

        private void FrequencyBeatDetection()
        {
            var fftBuffer = new Complex[samples];
            
            var spectrum = new float[samples / 2 + 1];
            
            var bands = new float[64]; // TODO - input;
            //var bandWidth = spectrum.Length / bands.Length;

            var beats = new List<int>();

            for(var i = 0; i < samples; i++)
                fftBuffer[i] = new Complex(samplesLeft[i], samplesRight[i]);
            
            fft.Transform(fftBuffer);
            
            for (var i = 0; i < spectrum.Length; i++)
            {
                spectrum[i] = (float)Math.Sqrt(Math.Pow(fftBuffer[i].Real, 2) + Math.Pow(fftBuffer[i].Imaginary, 2));
            }

            // for (var band = 0; band < bands.Length; band++)
            // {
            //     var bandAvg = 0f;
            //     int freq;

            //     for (freq = 0; freq < bandWidth; freq++)
            //     {
            //         var offset = freq + band * bandWidth;
                    
            //         if (offset > spectrum.Length)
            //             break;

            //         bandAvg += spectrum[offset];
            //     }

            //     bandAvg /= freq + 1;
            //     bands[band] = bandAvg;
            // }
            
            var w1 = 1;
            var a = (2*spectrum.Length - 2*bands.Length*w1) / (float)(bands.Length * bands.Length - bands.Length);
            var b = w1 - a;

            var offset = 0;
            for (var band = 0; band < bands.Length; band++)
            {
                var bandAvg = 0f;
                var bandWidth = (int)Mathf.Floor(a*(band+1) + b);
                int spectrumBand;

                for (spectrumBand = 0; spectrumBand < bandWidth; spectrumBand++)
                {
                    if (offset + spectrumBand > spectrum.Length)
                        break;

                    bandAvg += spectrum[offset + spectrumBand];
                }

                bandAvg *= (spectrumBand + 1) / (float)spectrum.Length;
                bands[band] = bandAvg;
                offset += bandWidth;
            }
            
            OnSample.Invoke(bands);

            for (var i = 0; i < bands.Length; i++)
            {
                var averageEnergy = 0.0f;
                for (var j = 0; j < bandEnergySamples.GetLength(1); j++)
                {
                    averageEnergy += bandEnergySamples[i,j];
                }

                averageEnergy /= bandEnergySamples.GetLength(1);

                Debug.DrawLine(new Vector3(i * 0.2f - 32*0.2f, 0, 0), 
                               new Vector3(i * 0.2f - 32*0.2f, bands[i], 0), 
                               Color.red, sampleTime);

                Debug.DrawLine(new Vector3((i == 0 ? i : i - 1) * 0.2f - 32*0.2f, averageEnergy, 0), 
                               new Vector3(i * 0.2f  - 32*0.2f, averageEnergy, 0), 
                               Color.green, sampleTime);
                               
                if (bands[i] > averageEnergy * 1.4)
                    beats.Add(i);

                for (var j = bandEnergySamples.GetLength(1) - 2; j >= 0; j--)
                {
                    bandEnergySamples[i, j + 1] = bandEnergySamples[i, j]; // TODO: Could make this faster without the array.
                }

                bandEnergySamples[i, 0] = bands[i]; 
            }

            if (beats.Count > 0)
                OnBeat.Invoke(beats.ToArray(), bands);

            var beatString = string.Join(",", beats);
            
            if (!string.IsNullOrWhiteSpace(beatString) && beats.Contains(3))
                Debug.Log("BEATS: " + beatString);
        }
    }
}