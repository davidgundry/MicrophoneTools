using System;

namespace MicTools
{
public class SyllableDetectionAlgorithm
{
    private bool syllableDetected;
    private int totalSyllables;

    public const float dipMultiple = 1.5848931924611136f; //2dB
    public const int windowSize = 1024;
    public const float npaThreshold = 0.4f;

    private readonly int sampleRate;

    private float peak = 0f;
    private float dip = 0f;
    private bool dipped = true;

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

        float sumIntensity = 0;
        if (!SinglePolaity(data)) // Here to easily take out artifacts found on my poor desktop mic
        {
            sumIntensity = SumAbsIntensity(data);
            level = sumIntensity / data.Length;

            float mean = SumIntensity(data) / data.Length;

            int sampleOffsetHigh;
            int sampleOffsetLow;
            FrequencyBandToSampleOffsets(data.Length, sampleRate, 80, 300, out sampleOffsetHigh, out sampleOffsetLow); // was 80,900
            float newNPA = DoNormalisedPeakAutocorrelation(data, mean, sampleOffsetHigh, sampleOffsetLow);
            normalisedPeakAutocorrelation += (newNPA - normalisedPeakAutocorrelation) * deltaTime * 10;

            DipTracking();

            // If we're using the periodicity, check that the normalised value is high before
            // considering it
            if (normalisedPeakAutocorrelation > npaThreshold) 
            {
                peak = Math.Max(peak, level);
                PeakPicking();
            }

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

        return syllableDetected;
    }

    /// <summary>
    /// Peak-picking, checks whether a peak has been found.
    /// </summary>
    private void PeakPicking()
    {
        if (((peak - dip) * dipMultiple > peak) && (dipped) && (peak > level))
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

    private static float SumIntensity(float[] data)
    {
        float sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    /// <summary>
    /// Poor-man's high-pass filter to remove very low frequency noice
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static bool SinglePolaity(float[] data)
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