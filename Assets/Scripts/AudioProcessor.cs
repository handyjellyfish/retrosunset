using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HandyJellyfish.Audio
{
    public class AudioProcessor : MonoBehaviour
    {
        private struct Complex
        {
            public float Real;
            public float Imaginary;

            public override string ToString()
            {
                return Real.ToString() + " + " + Imaginary.ToString() + "i";
            }
        }

        [SerializeField] AudioSource audio;
        [SerializeField] bool simpleBeat = true;

        const int samples = 1024;
        float sampleTime;

        float[] energySamples;
        float[,] bandEnergySamples;

        float[] samplesLeft;
        float[] samplesRight;

        void Start()
        {
            Debug.Log("Audio Channels: " + audio.clip.channels);
            Debug.Log("Audio Frequency: " + audio.clip.frequency);

            sampleTime = samples/(float)audio.clip.frequency;
            energySamples = new float[audio.clip.frequency/samples];
            bandEnergySamples = new float[32, audio.clip.frequency/samples];

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
            var values = new Complex[samples];
            var bandEnergy = new float[32]; // TODO - input;
            var energyDivision = 32 / (float)samples;
            var beats = new List<int>();

            for(var i = 0; i < samples; i++)
            {
                values[i].Real = samplesLeft[i];
                values[i].Imaginary = samplesRight[i];
            }
            
            FFT(values);
            
            for (var i = 0; i < samples; i++)
            {
                var amplitude = Mathf.Sqrt(values[i].Real * values[i].Real + values[i].Imaginary * values[i].Imaginary);
                bandEnergy[i / 32] += energyDivision * amplitude;
            }

            for (var i = 0; i < bandEnergy.Length; i++)
            {
                var averageEnergy = 0.0f;
                for (var j = 0; j < bandEnergySamples.GetLength(1); j++)
                {
                    averageEnergy += bandEnergySamples[i,j];
                }

                averageEnergy /= bandEnergySamples.GetLength(1);

                Debug.DrawLine(new Vector3((i == 0 ? i : i - 1) * 0.2f - 16*0.2f, bandEnergy[i == 0 ? i : i - 1] * 1000 + 5, 0), 
                               new Vector3(i * 0.2f  - 16*0.2f, bandEnergy[i]*1000 + 5, 0), 
                               Color.red);

                Debug.DrawLine(new Vector3((i == 0 ? i : i - 1) * 0.2f - 16*0.2f, averageEnergy * 2500 + 5, 0), 
                               new Vector3(i * 0.2f  - 16*0.2f, averageEnergy * 2500 + 5, 0), 
                               Color.green);
                               
                if (bandEnergy[i] > 2.5 * averageEnergy)
                    beats.Add(i);

                for (var j = bandEnergySamples.GetLength(1) - 2; j >= 0; j--)
                {
                    bandEnergySamples[i, j + 1] = bandEnergySamples[i, j]; // TODO: Could make this faster without the array.
                }

                bandEnergySamples[i, 0] = bandEnergy[i]; 
            }

            var beatString = string.Join(",", beats);
            
            if (!string.IsNullOrWhiteSpace(beatString))
                Debug.Log("BEATS: " + beatString);
        }

        private void FFT(Complex[] values)
        {
            // TODO: Confirm numbers are valid e.g. power of 2 etc.
            SubFFT(values, values.Length, 0);
            
            var N2 = (float)values.Length * 2;

            for (var i = 0; i < values.Length; i++)
            {
                values[i].Real /= N2;
                values[i].Imaginary /= N2;
            }
        }

        private void SubFFT(Complex[] values, int n, int lo)
        {
            if (n > 1)
            {
                var m = n / 2;
                    
                if (n > 2)
                {
                    // shuffle into odd and even
                    var temp = new Complex[m];

                    for (var i = 0; i < m; i++)
                        temp[i] = values[i * 2 + lo + 1];
                    for (var i = 0; i < m; i++)
                        values[i + lo] = values[i * 2 + lo];
                    for (var i = 0; i < m; i++)
                        values[i + lo + m] = temp[i];
                }

                SubFFT(values, m, lo);
                SubFFT(values, m, lo + m);
                
                Complex w, h;

                for (var i = lo; i < lo + m; i++)
                {
                    var v1 = values[i];
                    var v2 = values[i + m];

                    w.Real = Mathf.Cos(2.0f * Mathf.PI * (i - lo) / (float)n);
                    w.Imaginary = Mathf.Sin(2.0f * Mathf.PI * (i - lo) / (float)n);

                    h.Real = v2.Real * w.Real - v2.Imaginary * w.Imaginary;
                    h.Imaginary = v2.Real * w.Imaginary + v2.Imaginary * w.Real;

                    values[i].Real += h.Real;
                    values[i].Imaginary += h.Imaginary;

                    values[i + m].Real = v1.Real - h.Real;
                    values[i + m].Imaginary = v1.Imaginary - h.Imaginary;
                }
            }
        }
    }
}