﻿using System;

namespace MicTools
{
    public class SyllableDetectionAlgorithm
    {
        //private int samples;
        //public float caution;
        private bool syllableDetected;
        private int totalSyllables;
        //public bool Syllable { get {  return syllable; } }

        //public const float activationMultiple = 1;//1.5848931924611136f;//unfiltered = 1; //0dB   filtered = 1.5848931924611136f;  //2dB
        //public const float highActivationMultiple = 1.5848931924611136f; //1.9952623149688797f; //3dB
        //public const float deactivationMultiple = 1.5848931924611136f; // 1f; //0dB
        //public const float presenceMultiple = 1f;

        public const float dipMultiple = 1.5848931924611136f; //2dB
        public const int windowSize = 1024;
        public const float npaThreshold = 0.4f;

        private readonly int sampleRate;

        private float peak = 0f;
        private float dip = 0f;
        private bool dipped = true;

        //private int bufferReadPos = 0;
        //private const float timeStep = 0.05f;

        //private int inputDetectionTimeout = 0;

        /*private float noiseIntensity;
        public float NoiseIntensity { get { return noiseIntensity; } }
        private float standardDeviation;
        public float StandardDeviation { get { return standardDeviation; } }*/

        //private int samplesSoFar = 0;
       // private int windowsSoFar = 0;

        //private bool inputDetected;
        //public bool InputDetected { get { return inputDetected; } }
        private float level = 0;
        public float Level { get { return level; } }

        private float normalisedPeakAutocorrelation;    
        public float NormalisedPeakAutocorrelation { get { return normalisedPeakAutocorrelation; } }


        public SyllableDetectionAlgorithm(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public bool Run(float[] data, float deltaTime)
        {
            syllableDetected = false;

            //samplesSoFar += data.Length;
            //windowsSoFar++;
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
                FrequencyBandToSampleOffsets(data.Length, sampleRate, 80, 300, out sampleOffsetHigh, out sampleOffsetLow); // was 80,900
                float newNPA = DoNormalisedPeakAutocorrelation(data, mean, sampleOffsetHigh, sampleOffsetLow);
                normalisedPeakAutocorrelation += (newNPA - normalisedPeakAutocorrelation) * deltaTime * 10;

                DipTracking();
                if (normalisedPeakAutocorrelation > npaThreshold) // If we're using the periodicity, check that the normalised value is high before considering it
                {
                    peak = Math.Max(peak, level);
                    PeakPicking();
                }

                //if (windowsSoFar > 20) // To stop getting stuck thinking everything is a syllable if it starts loud
                //    DetectSyllables();
                //DetectPresence(); ----- Not a core part of the Algorithm.

            }
            else
                level = 0;

            if (syllableDetected)
                totalSyllables++;

            LogMT.SendStreamValue("MTdt", deltaTime);
            LogMT.SendStreamValue("MTnpa", normalisedPeakAutocorrelation);
            LogMT.SendStreamValue("MTlvl", level);
            LogMT.SendStreamValue("MTpek", peak);
            LogMT.SendStreamValue("MTdip", dip);
            LogMT.SendStreamValue("MTdpd", Convert.ToInt32(dipped));
            LogMT.SendStreamValue("MTsbs", totalSyllables);
            //LogMT.SendStreamValue("MTnoi", noiseIntensity);
            //LogMT.SendStreamValue("MTsd", standardDeviation);
            //LogMT.SendStreamValue("MTidt", inputDetectionTimeout);
            //LogMT.SendStreamValue("MTssf", samplesSoFar);
            //LogMT.SendStreamValue("MTwsf", windowsSoFar);

            return syllableDetected;
        }

        /// <summary>
        /// Peak-picking, checks whether a peak has been found.
        /// </summary>
        private void PeakPicking()
        {
            if (((peak - dip) * dipMultiple > peak) && (dipped) && (peak > level))// && (level > noiseIntensity + caution * standardDeviation)) // Removed because should no longer have an effect
            {
                dipped = false;
                syllableDetected = true;
                dip = peak;
            }
        }

        /// <summary>
        /// Keeps track of dip and peak for peak-picking
        /// </summary>
        private void DipTracking()
        {
            dip = Math.Min(dip, level);

            if (((peak - dip) * dipMultiple > peak) && (!dipped) && (dip < level))
            {
                dipped = true;
                peak = dip;
            }
        }

        //Not sure I'm doing the right thing here...
        private static void FrequencyBandToSampleOffsets(int windowSize,
                                                          int sampleRate,
                                                          float lowFrequencyBound,
                                                          float highFrequencyBound,
                                                          out int sampleOffsetHigh,
                                                          out int sampleOffsetLow)
        {
            float timeStepsPerSecond = 1 / ((float)windowSize / (float)sampleRate);
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

            float[] gammaA = new float[sampleOffsetHigh - sampleOffsetLow + 1];

            float sumZero = 0;
            for (int t = 0; t < window.Length - 0; t++)
                sumZero += window[t + 0] * window[t];

            float gammaZero = sumZero / window.Length;

            for (int h = sampleOffsetHigh; h >= sampleOffsetLow; h--)
            {
                float sum = 0;
                for (int t = 0; t < window.Length - h; t++)
                    sum += (window[t + h] - mean) * (window[t] - mean);

                float gamma = (sum / window.Length) / (window.Length - h);
                if (gamma > highest)
                    highest = gamma;

                float norm = highest / (gammaZero / window.Length);
                gammaA[h - sampleOffsetLow] = norm;
            }

            // Here we normalise the peak value so it is between 0 and 1

            float normalised = highest / (gammaZero / window.Length);

            LogMT.SendStreamValue("MTsoL", sampleOffsetLow);
            LogMT.SendStreamValue("MTsoH", sampleOffsetHigh);
            LogMT.SendStreamValueBlock("MTatc", gammaA);

            return normalised;
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
                levelMax = Math.Max(Math.Abs(data[i]), levelMax);
            return levelMax;
        }

        private static float SumAbsIntensity(float[] data)
        {
            float sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += Math.Abs(data[i]);
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