using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MicTools;

namespace MicTools
{
    [RequireComponent(typeof(MicrophoneBuffer))]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("MicrophoneTools/FormantFinder")]
    public class FormantFinder : MonoBehaviour
    {
        private AudioSource audioSource;

        private float[] spectrum;
        public float[] Spectrum
        {
            get
            {
                return spectrum;
            }
        }
        private const int sampleWindow = 128;
        private static int spectrumSize = 8192;

        private const int activationThreshold = 20;

        private int windowsSoFar;
        public float noiseLevel = 1f;
        public float NoiseLevel
        {
            get
            {
                return noiseLevel;
            }
        }

        private FormantRecord[] formants;
        public FormantRecord[] Formants
        {
            get
            {
                return formants;
            }
        }

        public int f1;
        public int f2;
        public float f1freq;
        public float f2freq;

        private float timeStep = 0.02f;
        private double elapsedTime = 0;

        void Start()
        {
            audioSource = this.GetComponent<AudioSource>();
            spectrum = new float[spectrumSize];
        }

        void Update()
        {
            elapsedTime += AudioSettings.dspTime;
            if (elapsedTime >= timeStep)
            {
                windowsSoFar++;
                elapsedTime = 0;
                audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

                float mean = MicrophoneInput.SumIntensity(spectrum) / spectrum.Length;
                noiseLevel += (mean - noiseLevel) / windowsSoFar;
                formants = PeakPicking(spectrum, noiseLevel);

                if (formants.Length > 0)
                {
                    f1 = formants[0].Peak;
                    if (formants.Length > 1)
                        f2 = formants[1].Peak;
                    else
                        f2 = 0;
                }
                else
                {
                    f1 = 0;
                    f2 = 0;
                }
                f1freq = IndexToFrequency(f1);
                f2freq = IndexToFrequency(f2);

            }
        }

        private FormantRecord[] PeakPicking(float[] data, float noiseLevel)
        {
            List<FormantRecord> formants = new List<FormantRecord>();

            int start = 0;
            int end = 0;
            int peak = 0;
            float highest = 0;

            bool inPeak = false;

            for (int i = 10; i < data.Length; i++)
            {
                if (!inPeak)
                {
                    if (data[i] > noiseLevel * activationThreshold)
                    {
                        start = i;
                        inPeak = true;
                    }
                }
                else
                {
                    if (data[i] > highest)
                    {
                        highest = data[i];
                        peak = i;
                    }
                    if (data[i] < noiseLevel * activationThreshold)
                    {
                        end = i;
                        inPeak = false;
                        formants.Add(new FormantRecord(start, end, peak));
                        start = 0;
                        end = 0;
                        peak = 0;
                        highest = 0;
                    }
                }
            }

            return formants.ToArray();
        }

        /*private void HighestPoints()
        {
            f1 = 0;
            f2 = 0;
            float highest = 0;
            for (int i = 0; i < spectrumSize; i++)
            {
                if (spectrum[i] > highest)
                {
                    f1 = i;
                    highest = spectrum[i];
                }
            }
        }*/

        public static float IndexToFrequency(int index)
        {
            return index * 44100 / spectrumSize;
        }

        private float FrequencyLevel(float frequency)
        {
            int index = (int)Mathf.Round(((1 / 48000) * spectrumSize * 2) * frequency);
            return spectrum[index];
        }

        private int FrequencyToIndex(float frequency)
        {
            return (int)Mathf.Round(((1 / 48000) * spectrumSize * 2) * frequency);
        }

        private float SumSpectrumArea(float low, float high)
        {
            int min = FrequencyToIndex(low);
            int max = spectrum.Length - 1;
            if (high != 0)
                max = FrequencyToIndex(high);

            float sum = 0;
            for (int i = min; i <= max; i++)
            {
                sum += spectrum[i];
            }
            return sum;
        }

    }
}
