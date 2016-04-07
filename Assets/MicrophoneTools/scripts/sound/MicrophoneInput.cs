﻿using UnityEngine;
using System.Collections;
using System;
using MicTools;

namespace MicTools
{
    [RequireComponent(typeof(MicrophoneBuffer))]
    [AddComponentMenu("MicrophoneTools/MicrophoneInput")]
    public class MicrophoneInput : MonoBehaviour
    {
        public int samples;

        private MicrophoneBuffer microphoneBuffer;

        public float caution;
        public int syllables = 0;
        //private bool syllable = false;
        //public bool Syllable { get {  return syllable; } }

        public const float activationMultiple = 1;//1.5848931924611136f;//unfiltered = 1; //0dB   filtered = 1.5848931924611136f;  //2dB
        public const float highActivationMultiple = 1.5848931924611136f; //1.9952623149688797f; //3dB
        public const float dipMultiple = 1.5848931924611136f; //2dB
        public const float deactivationMultiple = 1.5848931924611136f; // 1f; //0dB
        public const float presenceMultiple = 1f;

        private const int maxWindowLengthForAutocorrelation = 1024;
        private const int windowSize = 1024;

        private const float npaThreshold = 0.4f;

        private float peak = 0f;
        private float dip = 0f;
        private bool dipped = true;

        private int bufferReadPos = 0;
        //private const float timeStep = 0.05f;

        private int inputDetectionTimeout = 0;

        private float noiseIntensity;
        public float NoiseIntensity { get { return noiseIntensity; } }
        private float standardDeviation;
        public float StandardDeviation { get { return standardDeviation; } }

        private int samplesSoFar = 0;
        private int windowsSoFar = 0;

        //private bool inputDetected;
        //public bool InputDetected { get { return inputDetected; } }
        private float level = 0;
        public float Level { get { return level; } }

        private float normalisedPeakAutocorrelation;
        public float NormalisedPeakAutocorrelation { get { return normalisedPeakAutocorrelation; } }

        public bool test;

        public float pitch;

        private Yin yin;

        void Start()
        {
            microphoneBuffer = GetComponent<MicrophoneBuffer>();
        
        }

        public void OnSoundEvent(SoundEvent e)
        {
            switch (e)
            {
                case SoundEvent.BufferReady:
                    yin = new Yin(microphoneBuffer.SampleRate, windowSize);
                    LogMT.Log("Window Size for Algorithm: " + windowSize);
                    LogMT.Log("Max Window Length for Autocorrelation: " + maxWindowLengthForAutocorrelation);
                    break;
            }
        }

        private int TestHarness()
        {
            int startingSyllables = syllables;
            syllables = 0;
            //bool startingSyllable = syllable;
            //syllable = false;
            float startingNoiseIntensity = noiseIntensity;
            noiseIntensity = 0;
            float startingStandardDeviation = standardDeviation;
            standardDeviation = 0;
            int startingSamplesSoFar = samplesSoFar;
            samplesSoFar = 0;
            int startingWindowsSoFar = windowsSoFar;
            windowsSoFar = 0;
            float startingPeak = peak;
            peak = 0;
            float startingDip = dip;
            dip = 0;
            bool startingDipped = dipped;
            dipped = true;

            AudioClip testClip = GetComponent<AudioSource>().clip;
            int length = windowSize;// (int)(GetComponent<MicrophoneController>().SampleRate * timeStep);
            float[] samples = new float[length];

            if (testClip != null)
                for (int i = 0; i < testClip.samples; i += samples.Length)
                {
                    if (i + samples.Length > testClip.samples)
                        samples = new float[testClip.samples - i];
                    else if (samples.Length != length)
                        samples = new float[length];

                    windowsSoFar++;
                    samplesSoFar += samples.Length;
                    testClip.GetData(samples, i);
                    Algorithm(samples);
                }

            int totalS = syllables;
            syllables = startingSyllables;
            noiseIntensity = startingNoiseIntensity;
            standardDeviation = startingStandardDeviation;
            samplesSoFar = startingSamplesSoFar;
            windowsSoFar = startingWindowsSoFar;
            peak = startingPeak;
            dip = startingDip;
            dipped = startingDipped;

            return totalS;
        }

        void Update()
        {
            if (test)
            {
                LogMT.Log("Syllables: " + TestHarness());
                test = false;
            }

            float[] window = GetMostRecentSamples(windowSize);
            samplesSoFar += windowSize;
            windowsSoFar++;

            if (yin != null)
                pitch = yin.getPitch(window);

            Algorithm(window);
                    
            //LogMT.SendStreamValueBlock("MTaudio", window);
            LogMT.SendStreamValue("MTnpa", normalisedPeakAutocorrelation);
            LogMT.SendStreamValue("MTlvl", level);
            LogMT.SendStreamValue("MTnoi", noiseIntensity);
            LogMT.SendStreamValue("MTsd", standardDeviation);
            LogMT.SendStreamValue("MTpek", peak);
            LogMT.SendStreamValue("MTdip", dip);
            LogMT.SendStreamValue("MTdpd", Convert.ToInt32(dipped));
            LogMT.SendStreamValue("MTsbs", syllables);
            LogMT.SendStreamValue("MTidt", inputDetectionTimeout);
            LogMT.SendStreamValue("MTssf", samplesSoFar);
            LogMT.SendStreamValue("MTwsf", windowsSoFar);

            LogMT.SendStreamValue("MTdt", Time.deltaTime);
        }
        

        private void Algorithm(float[] data)
        {
            samples = data.Length;
            float sumIntensity = 0;
            if (!SinglePolaity(data)) // Here to easily take out artifacts found on my poor desktop mic
            {
                sumIntensity = SumAbsIntensity(data);
                level = sumIntensity / data.Length;
                /*if (!syllable)
                {
                    standardDeviation = Mathf.Sqrt(
                            (Mathf.Pow(standardDeviation, 2) * (Mathf.Min(20, windowsSoFar) - 1)
                            + Mathf.Pow(level - noiseIntensity, 2))
                        / Mathf.Min(20, windowsSoFar));
                    noiseIntensity += (sumIntensity - noiseIntensity * data.Length) / Mathf.Min(microphoneBuffer.SampleRate * 4, samplesSoFar);
                }*/
                float mean = SumIntensity(data) / data.Length;

                //noiseIntensity = 0;

                int sampleOffsetHigh;
                int sampleOffsetLow;
                FrequencyBandToSampleOffsets(data.Length, microphoneBuffer.SampleRate, 80, 300, out sampleOffsetHigh, out sampleOffsetLow); // was 80,900
                float newNPA = DoNormalisedPeakAutocorrelation(data, mean, sampleOffsetHigh, sampleOffsetLow);
                normalisedPeakAutocorrelation += (newNPA - normalisedPeakAutocorrelation) * Time.deltaTime;

                DipTracking();
                if (normalisedPeakAutocorrelation > npaThreshold) // If we're using the periodicity, check that the normalised value is high before considering it
                {
                    peak = Mathf.Max(peak, level);
                    PeakPicking();
                }

                //if (windowsSoFar > 20) // To stop getting stuck thinking everything is a syllable if it starts loud
                //    DetectSyllables();
                //DetectPresence(); ----- Not a core part of the Algorithm.
               
            }
            else
                level = 0;
        }

        /// <summary>
        /// Peak-picking, checks whether a peak has been found.
        /// </summary>
        private void PeakPicking()
        {
            if (((peak - dip) * dipMultiple > peak) && (dipped) && (peak > level))// && (level > noiseIntensity + caution * standardDeviation)) // Removed because should no longer have an effect
            {
                dipped = false;
                syllables++;
                gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllablePeak, SendMessageOptions.DontRequireReceiver);
                dip = peak;
            }
        }

        /// <summary>
        /// Keeps track of dip and peak for peak-picking
        /// </summary>
        private void DipTracking()
        {
            dip = Mathf.Min(dip, level);

            if (((peak - dip) * dipMultiple > peak) && (!dipped) && (dip < level))
            {
                dipped = true;
                peak = dip;
            }
        }

        //Not sure I'm doing the right thing here...
        private static void FrequencyBandToSampleOffsets( int windowSize,
                                                          int sampleRate, 
                                                          float lowFrequencyBound,
                                                          float highFrequencyBound,
                                                          out int sampleOffsetHigh,
                                                          out int sampleOffsetLow)
        {
            float timeStepsPerSecond = 1 / ((float) windowSize / (float) sampleRate);
            sampleOffsetHigh = (int)(windowSize * (timeStepsPerSecond / lowFrequencyBound));
            sampleOffsetLow = (int)(windowSize * (timeStepsPerSecond / highFrequencyBound));
        }

        /// <summary>
        ///     Calculates the peak of the normalised autocorrelation of a window of samples,
        ///     with an offset within a given band.
        /// </summary>
        private static float DoNormalisedPeakAutocorrelation(float[] window,
                                                             float mean,
                                                             int sampleOffsetHigh,
                                                             int sampleOffsetLow)
        {
            float highest = 0;

            //int windowLength = window.Length;
            int windowLength = Math.Min(window.Length, maxWindowLengthForAutocorrelation);

            float[] gammaA = new float[sampleOffsetHigh-sampleOffsetLow+1];

            float sumZero = 0;
            for (int t = 0; t < windowLength - 0; t++)
                sumZero += window[t + 0] * window[t];

            float gammaZero = sumZero / windowLength;

            for (int h = sampleOffsetHigh; h >= sampleOffsetLow; h--)
            {
                float sum = 0;
                for (int t = 0; t < windowLength - h; t++)
                    sum += (window[t + h] - mean) * (window[t] - mean);

                float gamma = (sum / windowLength) / (windowLength - h);
                if (gamma > highest)
                    highest = gamma;

                float norm = highest / (gammaZero / windowLength);
                gammaA[h - sampleOffsetLow] = norm;
            }

            // Here we normalise the peak value so it is between 0 and 1

            float normalised = highest / (gammaZero / windowLength);

            LogMT.SendStreamValue("MTsoL", sampleOffsetLow);
            LogMT.SendStreamValue("MTsoH", sampleOffsetHigh);
            LogMT.SendStreamValueBlock("MTatc", gammaA);

            return normalised;
        }

        private float[] GetMostRecentSamples(int count)
        {
            float[] newSamples = new float[count];

            if (microphoneBuffer.Buffer.Length > 0)
            {
                if (microphoneBuffer.BufferPos - count >= 0)
                    System.Buffer.BlockCopy(microphoneBuffer.Buffer, (microphoneBuffer.BufferPos - count) * sizeof(float), newSamples, 0, count * sizeof(float));
                else
                {
                    int headSamples = microphoneBuffer.BufferPos;
                    int tailSamples = count - microphoneBuffer.BufferPos;
                    int tailStart = microphoneBuffer.Buffer.Length - tailSamples;
                    System.Buffer.BlockCopy(microphoneBuffer.Buffer, 0, newSamples, tailSamples*sizeof(float), headSamples*sizeof(float));
                    System.Buffer.BlockCopy(microphoneBuffer.Buffer, tailStart*sizeof(float), newSamples, 0,   tailSamples*sizeof(float));
                    
                }
            }
            return newSamples;
        }

        /*private int NewSamples
        {
            get
            {
                int newSamples = 0;
                if (bufferReadPos > microphoneBuffer.BufferPos)
                    newSamples = microphoneBuffer.Buffer.Length - bufferReadPos + microphoneBuffer.BufferPos;
                else
                    newSamples = microphoneBuffer.BufferPos - bufferReadPos;
                return newSamples;
            }
        }

        private float[] NewWindow()
        {
            float[] buffer = microphoneBuffer.Buffer;

            int newSamples = NewSamples;

            if (newSamples > 0)
            {
                samplesSoFar += newSamples;
                windowsSoFar++;

                float[] data = new float[newSamples];
                int i = 0;
                while ((bufferReadPos != microphoneBuffer.BufferPos) && (i < newSamples))
                {
                    data[i] = buffer[bufferReadPos];
                    i++;
                    bufferReadPos = (bufferReadPos + 1) % buffer.Length;
                }
                return data;
            }

            return new float[0];
        }*/

        /*private void DetectPresence()
        {
            if (level > noiseIntensity * presenceMultiple)
            {
                if (!inputDetected)
                {
                    inputDetected = true;
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.InputStart, SendMessageOptions.DontRequireReceiver);
                    inputDetectionTimeout = 10;         // Having a short timeout gets rid of some of the noise
                }
            }
            else if (inputDetectionTimeout == 0)
            {
                if (inputDetected)
                {
                    inputDetected = false;
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.InputEnd, SendMessageOptions.DontRequireReceiver);
                }
            }
            else
                inputDetectionTimeout--;
        }*/

        /*private void DetectSyllables()
        {
            if (level > noiseIntensity + standardDeviation/2)//* highActivationMultiple))
            {
                if (!syllable)
                {
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllableStart, SendMessageOptions.DontRequireReceiver);
                    syllable = true;
                }
            }

            if (level < noiseIntensity + standardDeviation/2)//* deactivationMultiple))
            {
                if (syllable)
                {
                    syllable = false;
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllableEnd, SendMessageOptions.DontRequireReceiver);
                }
            }
        }*/

        private static float Peak(float[] data)
        {
            float levelMax = 0;
            for (int i = 0; i < data.Length; i++)
                levelMax = Mathf.Max(Mathf.Abs(data[i]), levelMax);
            return levelMax;
        }

        private static float SumAbsIntensity(float[] data)
        {
            float sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += Mathf.Abs(data[i]);
            return sum;
        }

        public static float SumIntensity(float[] data)
        {
            float sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += data[i];
            return sum;
        }

        private static bool SinglePolaity(float[] data) //Poor-man's high-pass filter to remove v.low frequency noice
        {
            if (data.Length > 0)
            {
                bool polarity = (data[0] >= 0);
                for (int i = 1; i < data.Length; i++)
                    if (polarity)
                    {
                        if (data[i] < 0)
                            return false;
                    }
                    else
                        if (data[i] > 0)
                            return false;
            }
            return true;
        }
    }
}
