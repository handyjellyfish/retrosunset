using System.Linq;
using UnityEngine;

namespace HandyJellyfish.Audio
{
    public abstract class BeatReactor : MonoBehaviour
    {
        [SerializeField]AudioProcessor processor;
        
        [SerializeField]int minFrequency;
        [SerializeField]int maxFrequency;

        [Range(0, 10)]
        [SerializeField]float minIntensity = 0;
        
        [Range(1, 10)]
        [SerializeField]float beatMultiplier = 1.1f;

        protected virtual void Start()
        {
            processor.OnBeat.AddListener(OnBeat);
        }

        void OnBeat(int[] beats, float[] intensities)
        {
            var minBand = processor.FrequencyToBand(minFrequency);
            var maxBand = processor.FrequencyToBand(maxFrequency);
            
            var intensity = beats.Where(b => b <= minBand && b <= maxBand).Sum(b => intensities[b]);

            if (intensity > minIntensity)
                ProcessBeat(intensity * beatMultiplier);
            // if (beats.Contains(band) && sample[band] > minIntensity)
            //     beatScale = new Vector3(sample[band]*(x ? 1 : 0), sample[band]*(y ? 1 : 0), sample[band]*(z ? 1 : 0)) * beatImpact;
            // else
            //     beatScale = Vector3.zero;
        }

        protected abstract void ProcessBeat(float beatIntensity);
    }
}