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
        public enum SampleBandType
        {
            Simple,
            // Octave,
            Linear,
            Dynamic
        }

        [Serializable]
        public class SampleEvent : UnityEvent<float[]>
        {}

        [Serializable]
        public class BeatEvent : UnityEvent<int[], float[]>
        {}

        [SerializeField] AudioSource audio;

        [SerializeField] SampleBandType sampleBandType;
        [SerializeField] int dynamicBand1Width = 1;
        
        public int SampleBands = 32;
        public SampleEvent OnSample = new SampleEvent();
        public BeatEvent OnBeat = new BeatEvent();

        [SerializeField]bool debug = true;
        
        const int samples = 1024;
        FFT fft = new FFT(samples);

        public float SampleTime { get; private set; }

        float[] energySamples;
        float[,] bandEnergySamples;

        float[] samplesLeft;
        float[] samplesRight;
        
        float[] bands;

        void Start()
        {
            SampleTime = samples/(float)audio.clip.frequency;
            energySamples = new float[audio.clip.frequency/samples];
            bandEnergySamples = new float[SampleBands * 2, audio.clip.frequency/samples];
            bands = new float[SampleBands];

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

                if (sampleBandType == SampleBandType.Simple)
                    SimpleBeatDetection();
                else
                    FrequencyBeatDetection();
                
                yield return new WaitForSeconds(SampleTime);
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
                OnBeat.Invoke(new int[] { 0 }, new float[] { instantEnergy });

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
            
            var beats = new List<int>();

            for(var i = 0; i < samples; i++)
                fftBuffer[i] = new Complex(samplesLeft[i], samplesRight[i]);
            
            fft.Transform(fftBuffer);
            
            for (var i = 0; i < spectrum.Length; i++)
                spectrum[i] = (float)Math.Sqrt(Math.Pow(fftBuffer[i].Real, 2) + Math.Pow(fftBuffer[i].Imaginary, 2));

            if (sampleBandType == SampleBandType.Linear)
            {
                var bandWidth = spectrum.Length / bands.Length;
                for (var band = 0; band < bands.Length; band++)
                {
                    var bandAvg = 0f;
                    int freq;

                    for (freq = 0; freq < bandWidth; freq++)
                    {
                        var ix = freq + band * bandWidth;
                        
                        if (ix > spectrum.Length)
                            break;

                        bandAvg += spectrum[ix];
                    }

                    bandAvg /= freq + 1;
                    bands[band] = bandAvg;
                }
            }

            if (sampleBandType == SampleBandType.Dynamic)
            {
                // TODO: Write these equations here
                var a = (2*spectrum.Length - 2*bands.Length*dynamicBand1Width) / (float)(bands.Length * (bands.Length - 1));
                var b = dynamicBand1Width - a;

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
            }

            OnSample.Invoke(bands);

            var debugOffset = SampleBands*0.1f;
            
            for (var i = 0; i < bands.Length; i++)
            {
                var averageEnergy = 0.0f;
                for (var j = 0; j < bandEnergySamples.GetLength(1); j++)
                    averageEnergy += bandEnergySamples[i,j];
                
                averageEnergy /= bandEnergySamples.GetLength(1);

                if (debug)
                {
                    var lineOffset = i * 0.2f - debugOffset;
                    Debug.DrawLine(new Vector3(lineOffset, 5, 0), new Vector3(lineOffset, bands[i] + 5, 0), Color.red, SampleTime);
                    Debug.DrawLine(new Vector3(lineOffset - (i == 0 ? 0 : 0.2f), averageEnergy + 5, 0), new Vector3(lineOffset, averageEnergy + 5, 0), Color.green, SampleTime);
                }

                if (bands[i] > averageEnergy * 1.4)
                    beats.Add(i);

                for (var j = bandEnergySamples.GetLength(1) - 2; j >= 0; j--)
                    bandEnergySamples[i, j + 1] = bandEnergySamples[i, j]; // TODO: Could make this faster without the array.

                bandEnergySamples[i, 0] = bands[i]; 
            }

            if (beats.Count > 0)
                OnBeat.Invoke(beats.ToArray(), bands);
        }
    }
}