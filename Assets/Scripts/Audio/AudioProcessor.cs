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
        public enum BandType
        {
            Simple,
            // Octave,
            Linear,
            Logarithmic
        }

        [Serializable]
        public class SampleEvent : UnityEvent<float[]>
        {}

        [Serializable]
        public class BeatEvent : UnityEvent<int[], float[]>
        {}

        const int samples = 1024;
        readonly FFT fft = new FFT(samples);

        [SerializeField] AudioSource audioSource;

        [SerializeField] BandType bandType;
        [SerializeField] int subBands = 64;
        [SerializeField] int logStartBandWidth = 1;
        
        public SampleEvent OnSample = new SampleEvent();
        public BeatEvent OnBeat = new BeatEvent();
        public UnityEvent OnComplete = new UnityEvent();

        [SerializeField]bool showDebug = true;
        const float debugLineWidth = 0.2f;
        const float debugHeight = 5f;

        public float SampleRatio { get; private set; }

        float[] samplesLeft;
        float[] samplesRight;
        
        float[,] energyHistory;

        float[] bandFrequencies;
        float[] bandSamples;

        public int FrequencyToBand(int frequency)
        {
            if (frequency < 0 || frequency > audioSource.clip.frequency / 2)
                throw new ArgumentOutOfRangeException(nameof(frequency));

            // TODO: bring the formulas here rather than a loop to save memory and computation time.
            for (var i = 0; i < bandFrequencies.Length; i++)
            {
                if (bandFrequencies[i] > frequency)
                    return i;
            }

            throw new InvalidOperationException();
        }

        void Start()
        {
            SampleRatio = samples/(float)audioSource.clip.frequency;
            subBands = bandType == BandType.Simple ? 1 : subBands;
            energyHistory = new float[subBands, audioSource.clip.frequency/samples];
            
            bandSamples = new float[subBands];
            bandFrequencies = new float[subBands];

            samplesLeft = new float[samples];
            samplesRight = new float[samples];

            StartCoroutine(SampleAudio()); 
        }

        IEnumerator SampleAudio()
        {
            while (audioSource.isPlaying) // TODO: Few more advanced controls to handle this.
            {
                audioSource.GetOutputData(samplesLeft, 0);
                audioSource.GetOutputData(samplesRight, 1);

                if (bandType == BandType.Simple)
                    SimpleBeatDetection();
                else
                    FrequencyBeatDetection();
                
                yield return new WaitForSeconds(SampleRatio);
            }

            OnComplete.Invoke();
        }

        private void SimpleBeatDetection()
        {
            var instantEnergy = 0.0f;

            for (var i = 0; i < samples; i++)
            {
                instantEnergy += samplesLeft[i]*samplesLeft[i] + samplesRight[i]*samplesRight[i];
            }

            var averageEnergy = 0.0f;
            for (var i = 0; i < energyHistory.Length; i++)
            {
                averageEnergy += energyHistory[0,i]*energyHistory[0,i];
            }

            averageEnergy /= energyHistory.Length;

            var variance = 0.0f;
            for (var i = 0; i < energyHistory.Length; i++)
            {
                var localVariance = (energyHistory[0,i] - averageEnergy);
                variance += localVariance * localVariance;
            }

            variance /= energyHistory.Length;

            var sensitivity = (-0.0025714f*variance) + 1.5142857f; // high energy variance = lower sensitivity
            
            if (instantEnergy > averageEnergy * sensitivity)
                OnBeat.Invoke(new int[] { 0 }, new float[] { instantEnergy });

            for (var i = energyHistory.Length - 2; i >= 0; i--)
            {
                energyHistory[0,i+1] = energyHistory[0,i]; // TODO: Could make this faster without the array.
            }

            energyHistory[0,0] = instantEnergy;
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

            if (bandType == BandType.Linear)
            {
                var bandWidth = spectrum.Length / bandSamples.Length;
                for (var band = 0; band < bandSamples.Length; band++)
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
                    bandSamples[band] = bandAvg;
                    
                    // TODO: Band freqs for linear
                }
            }

            if (bandType == BandType.Logarithmic)
            {
                // TODO: Write these equations here
                var a = (2*spectrum.Length - 2*bandSamples.Length*logStartBandWidth) / (float)(bandSamples.Length * (bandSamples.Length - 1));
                var b = logStartBandWidth - a;

                var offset = 0;
                for (var band = 0; band < bandSamples.Length; band++)
                {
                    bandFrequencies[band] = offset / SampleRatio;

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
                    bandSamples[band] = bandAvg;
                    offset += bandWidth;
                }
            }

            OnSample.Invoke(bandSamples);

            for (var i = 0; i < bandSamples.Length; i++)
            {
                var averageEnergy = 0.0f;
                for (var j = 0; j < energyHistory.GetLength(1); j++)
                    averageEnergy += energyHistory[i,j];
                
                averageEnergy /= energyHistory.GetLength(1);

                if (bandSamples[i] > averageEnergy * 1.4)
                    beats.Add(i);

                for (var j = energyHistory.GetLength(1) - 2; j >= 0; j--)
                    energyHistory[i, j + 1] = energyHistory[i, j]; // TODO: Could make this faster without the array.

                energyHistory[i, 0] = bandSamples[i]; 

                if (showDebug)
                {
                    var halfWidth = debugLineWidth * 0.5f;
                    var lineHeight = debugHeight * 1.5f;
                    var x = i * debugLineWidth - bandSamples.Length * halfWidth;

                    Debug.DrawLine(new Vector3(x - halfWidth, bandSamples[i] + debugHeight, 0), 
                                   new Vector3(x + halfWidth, bandSamples[i] + debugHeight, 0), 
                                   beats.Contains(i) ? Color.white : Color.red, SampleRatio);

                    Debug.DrawLine(new Vector3(x - halfWidth, averageEnergy + debugHeight, 0), 
                                   new Vector3(x + halfWidth, averageEnergy + debugHeight, 0), 
                                   Color.green, SampleRatio);
                    
                    if (i > 0)
                    {
                        Debug.DrawLine(new Vector3(x - debugLineWidth, bandSamples[i - 1] + lineHeight, 0), 
                                       new Vector3(x, bandSamples[i] + lineHeight, 0), 
                                       Color.yellow, SampleRatio);

                        Debug.DrawLine(new Vector3(x - debugLineWidth, Mathf.Log(bandSamples[i - 1] + 1) + lineHeight, 0), 
                                       new Vector3(x, Mathf.Log(bandSamples[i] + 1) + lineHeight, 0), 
                                       Color.cyan, SampleRatio);
                    }
                }
            }

            
            if (beats.Count > 0)
                OnBeat.Invoke(beats.ToArray(), bandSamples);
            
            // Debug.Log(beats.Count > 0 ? "Beat" : "No Beat");
        }
    }
}