using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MicTools;
using System;


namespace MicTools
{
/* Much of the interacting with FFT code has been adapted from http://forum.unity3d.com/threads/fft-how-to.253192/*/

[RequireComponent(typeof(MicrophoneController))]
[AddComponentMenu("MicrophoneTools/FormantFinder")]
public class FFTPitchDetector : MonoBehaviour
{
    private MicrophoneController microphoneController;

    private float[] spectrum;
    public float[] Spectrum
    {
        get
        {
            return spectrum;
        }
    }
    /*private const int sampleWindow = 128;
    private static int spectrumSize = 8192;

    private const int activationThreshold = 1;

    private int windowsSoFar;
    public float noiseLevel = 1f;
    public float NoiseLevel
    {
        get
        {
            return noiseLevel;
        }
    }*/

    public float f0;

    /*private FormantRecord[] formants;
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
    public float f2freq;*/

    FFT2 fft;
    private const int windowSize = 1024*4;

    public int channel = 0; // the channel to analyze

    public int OutputLength
    {
        get
        {
            if (_doFFT == false)
                return 0;

            return windowSize / 2;
        }
    }

    private float[] arrRe;
    private float[] arrI;
    private float[] window;
    private bool _doFFT; // set to true when all is initialised. That way, no need for an expensive try catch.

    // Init
    void Awake()
    {
        microphoneController = GetComponent<MicrophoneController>();

        arrRe = new float[windowSize];
        arrI = new float[windowSize];
        window = new float[windowSize];
        window = Hanning(window);
        spectrum = new float[windowSize / 2];

        fft = new FFT2();
        uint logN = (uint)Math.Log(windowSize, 2);
        fft.init(logN);

        _doFFT = true;
    }


    void DoFFT(float[] data, int channels)
    {
        if (_doFFT == false)
            return;

        // Step 1 : de-interleave
        int j = 0;
        for (int i = channel; i < data.Length; i += channels)
        {
            arrRe[j] = data[i];
            j++;
        }

        // Apply precalculated windowing
        for (int i = 0; i < arrRe.Length; i++)
            arrRe[i] *= window[i];

        System.Array.Clear(arrI, 0, windowSize);

        fft.run(arrRe, arrI);

        // Compute magnitude, in place
        // Only compute half, as other half is useless
        for (int i = 0; i < windowSize / 2; i++)
            arrRe[i] = Mathf.Sqrt(arrRe[i] * arrRe[i] + arrI[i] * arrI[i]);

        System.Array.Copy(arrRe, spectrum, windowSize / 2);
    }

    public void GetSpectrumDataSynched(float[] data)
    {
        if (data.Length < windowSize / 2)
        {
            Debug.LogWarning("Provided array length should be at least " + (windowSize / 2).ToString());
            return;
        }
        System.Array.Copy(spectrum, data, windowSize / 2); //No need to copy the mirrored part
    }

    private float[] Hanning(float[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = (0.5f * (1.0f - Mathf.Cos((2.0f * Mathf.PI * i) / windowSize)));
        }
        return input;
    }

    void Update()
    {
        float[] window = microphoneController.GetMostRecentSamples(windowSize);
        DoFFT(window, 1);//microphoneBuffer.Channels);
        //windowsSoFar++;

        f0 = IndexToFrequency(HighestPoint(spectrum));


        /*float mean = 0; MicrophoneInput.SumIntensity(spectrum) / spectrum.Length;
        noiseLevel += (mean - noiseLevel) / windowsSoFar;
        formants = PeakPicking(spectrum, 0);

        if (formants.Length > 1)
        {
            f1 = formants[1].Peak;
            if (formants.Length > 2)
                f2 = formants[2].Peak;
            else
                f2 = 0;
        }
        else
        {
            f1 = 0;
            f2 = 0;
        }
        f1freq = IndexToFrequency(f1);
        f2freq = IndexToFrequency(f2);*/

    }

    /*private FormantRecord[] PeakPicking(float[] data, float noiseLevel)
    {
        List<FormantRecord> formants = new List<FormantRecord>();

        int start = 0;
        int end = 0;
        int peak = 0;
        float highest = 0;

        bool inPeak = false;

        for (int i = 5; i < data.Length; i++)
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
    }*/

    private static int HighestPoint(float[] data)
    {
        int index = 0;
        float highest = 0;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] > highest)
            {
                index = i;
                highest = data[i];
            }
        }
        return index;
    }

    public static float IndexToFrequency(int index)
    {
        return index * 44100 / windowSize;
    }

    private static float FrequencyLevel(float[] spectrum, float frequency)
    {
        int index = (int)Mathf.Round(((1 / 48000) * windowSize * 2) * frequency);
        return spectrum[index];
    }

    private static int FrequencyToIndex(float frequency)
    {
        return (int)Mathf.Round(((1 / 48000) * windowSize * 2) * frequency);
    }

    private static float SumSpectrumArea(float[] spectrum, float low, float high)
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
