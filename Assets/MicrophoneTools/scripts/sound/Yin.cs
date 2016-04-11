using UnityEngine;

namespace MicTools
{
    public class Yin
    {
        double threshold;
        int bufferSize;
        int halfBufferSize;
        float sampleRate;
        float[] yinBuffer;
        float probability;

        void initialize(float yinSampleRate, int yinBufferSize)
        {
            bufferSize = yinBufferSize;
            sampleRate = yinSampleRate;
            halfBufferSize = bufferSize / 2;
            threshold = 0.6; //was 0.15
            probability = 0.0f;
            //initialize array and set it to zero
            yinBuffer = new float[halfBufferSize];
            for (int i = 0; i < halfBufferSize; i++)
            {
                yinBuffer[i] = 0;
            }

            LogMT.Log("Yin sample rate: " + yinSampleRate);
            LogMT.Log("Yin buffer size: " + yinBufferSize);
            LogMT.Log("Yin threshold: " + threshold);
        }

        public Yin(float yinSampleRate, int yinBufferSize)
        {
            initialize(yinSampleRate, yinBufferSize);
        }

        float getProbability()
        {
            return probability;
        }

        public float getPitch(float[] buffer)
        {
            int tauEstimate = -1;
            float pitchInHertz = -1;

            MicTools.LogMT.SendStreamValueBlock("YINOrig", buffer);
            //step 2
            difference(buffer);
            MicTools.LogMT.SendStreamValueBlock("YINDiff", yinBuffer);

            // step 3
            cumulativeMeanNormalizedDifference();
            MicTools.LogMT.SendStreamValueBlock("YINcmnd", yinBuffer);

            //step 4
            tauEstimate = absoluteThreshold();
            MicTools.LogMT.SendStreamValue("YINtauest", tauEstimate);

            //step 5
            if (tauEstimate != -1)
            {

                pitchInHertz = sampleRate / parabolicInterpolation(tauEstimate);
            }

            MicTools.LogMT.SendStreamValue("YINpitchinhz", pitchInHertz);

            return pitchInHertz;
        }

        float parabolicInterpolation(int tauEstimate)
        {
            float betterTau;
            int x0;
            int x2;

            if (tauEstimate < 1)
            {
                x0 = tauEstimate;
            }
            else
            {
                x0 = tauEstimate - 1;
            }
            if (tauEstimate + 1 < halfBufferSize)
            {
                x2 = tauEstimate + 1;
            }
            else
            {
                x2 = tauEstimate;
            }
            if (x0 == tauEstimate)
            {
                if (yinBuffer[tauEstimate] <= yinBuffer[x2])
                {
                    betterTau = tauEstimate;
                }
                else
                {
                    betterTau = x2;
                }
            }
            else if (x2 == tauEstimate)
            {
                if (yinBuffer[tauEstimate] <= yinBuffer[x0])
                {
                    betterTau = tauEstimate;
                }
                else
                {
                    betterTau = x0;
                }
            }
            else
            {
                float s0, s1, s2;
                s0 = yinBuffer[x0];
                s1 = yinBuffer[tauEstimate];
                s2 = yinBuffer[x2];
                // fixed AUBIO implementation, thanks to Karl Helgason:
                // (2.0f * s1 - s2 - s0) was incorrectly multiplied with -1
                betterTau = tauEstimate + (s2 - s0) / (2 * (2 * s1 - s2 - s0));
            }
            return betterTau;
        }

        void cumulativeMeanNormalizedDifference()
        {
            yinBuffer[0] = 1;
            float runningSum = 0.1f; // This was 0 but causing NaNs!
            for (int tau = 1; tau < halfBufferSize; tau++)
            {
                runningSum += yinBuffer[tau];
                yinBuffer[tau] *= tau / runningSum;
            }
        }

        void difference(float[] buffer)
        {
            float delta;
            for (int tau = 0; tau < halfBufferSize; tau++)
            {
                for (int index = 0; index < halfBufferSize; index++)
                {
                    delta = buffer[index] - buffer[index + tau];
                    yinBuffer[tau] += delta * delta;
                }
            }
        }

        int absoluteThreshold()
        {
            int tau;
            // first two positions in yinBuffer are always 1
            // So start at the third (index 2)
            for (tau = 2; tau < halfBufferSize; tau++)
            {
                if (yinBuffer[tau] < threshold)
                {
                    while (tau + 1 < halfBufferSize && yinBuffer[tau + 1] < yinBuffer[tau])
                    {
                        tau++;
                    }
                    // found tau, exit loop and return
                    // store the probability
                    // From the YIN paper: The threshold determines the list of
                    // candidates admitted to the set, and can be interpreted as the
                    // proportion of aperiodic power tolerated
                    // within a ëëperiodicíí signal.
                    //
                    // Since we want the periodicity and and not aperiodicity:
                    // periodicity = 1 - aperiodicity
                    probability = 1 - yinBuffer[tau];
                    break;
                }
            }
            // if no pitch found, tau => -1
            if (tau == halfBufferSize || yinBuffer[tau] >= threshold)
            {
                tau = -1;
                probability = 0;
            }
            return tau;
        }
    }
}