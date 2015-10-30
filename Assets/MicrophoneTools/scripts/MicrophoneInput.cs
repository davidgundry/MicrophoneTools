using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MicrophoneBuffer))]
[AddComponentMenu("MicrophoneTools/MicrophoneInput")]
public class MicrophoneInput : MonoBehaviour {

    private MicrophoneBuffer microphoneBuffer;

    public int syllables = 0;
    private bool syllable = false;
    public bool Syllable
    {
        get
        {
            return syllable;
        }
    }

    public const float activationMultiple = 1.5848931924611136f;//0dB   filtered = 1.5848931924611136f;  //2dB
    public const float highActivationMultiple = 2f;
    public const float dipMultiple = 1.5848931924611136f; //2dB
    public const float deactivationMultiple = 1f; //0dB
    public const float presenceMultiple = 1f;

    private float peak = 0f;
    private float dip = 0f;
    private bool dipped = true;

    private int bufferReadPos = 0;
    private float timeStep = 0.02f;
    private double elapsedTime = 0;

    private int inputDetectionTimeout = 0;

    public float noiseIntensity = 1f;
    public float NoiseIntensity
    {
        get
        {
            return noiseIntensity;
        }
    }
    public float standardDeviation;

    private int samplesSoFar = 0;
    private int windowsSoFar = 0;

    private bool inputDetected;
    public bool InputDetected
    {
        get
        {
            return inputDetected;
        }
    }
    private float level = 0;
    public float Level
    {
        get
        {
            return level;
        }
    }

    void Start()
    {
        microphoneBuffer = GetComponent<MicrophoneBuffer>();
    }

    void Update()
    {
        elapsedTime += AudioSettings.dspTime;
        if (elapsedTime >= timeStep)
        {
            float[] data = NewWindow();
            float sumIntensity = 0;
            if (!SinglePolaity(data))
            {
                sumIntensity = SumAbsIntensity(data);
                level = sumIntensity / data.Length;
                if (!syllable)
                {
                    standardDeviation = Mathf.Sqrt(
                            (Mathf.Pow(standardDeviation, 2) * (Mathf.Min(20, windowsSoFar)-1)
                            + Mathf.Pow(level - noiseIntensity,2))
                        / Mathf.Min(20, windowsSoFar));
                    noiseIntensity += (sumIntensity - noiseIntensity * data.Length) / Mathf.Min(44100 * 4, samplesSoFar);
                }

                DetectNuclei();
                DetectSyllables();
                DetectPresence();
            }
            else
                level = 0;
        }
    }

    float[] NewWindow()
    {
        windowsSoFar++;
        float[] buffer = microphoneBuffer.Buffer;
        elapsedTime = 0;
        int newSamples = 0;
        if (bufferReadPos > microphoneBuffer.BufferPos)
            newSamples = buffer.Length - bufferReadPos + microphoneBuffer.BufferPos;
        else
            newSamples = microphoneBuffer.BufferPos - bufferReadPos;

        if (newSamples > 0)
        {
            samplesSoFar += newSamples;

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
    }

    void DetectPresence()
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
    }

    void DetectNuclei()
    {
        dip = Mathf.Min(dip, level);
        peak = Mathf.Max(peak, level);

        if (((peak - dip) * dipMultiple > peak) && (!dipped) && (dip < level))
        {
            dipped = true;
            peak = dip;
        }

        if (((peak - dip) * dipMultiple > peak) && (dipped) && (peak > level) && (level > noiseIntensity * activationMultiple))
        {
            dipped = false;
            syllables++;
            gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllablePeak, SendMessageOptions.DontRequireReceiver);
            dip = peak;
        }
    }

    void DetectSyllables()
    {
        if ((level > noiseIntensity * highActivationMultiple))
        {
            if (!syllable)
            {
                gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllableStart, SendMessageOptions.DontRequireReceiver);
                syllable = true;
            }
        }

        if ((level < noiseIntensity * deactivationMultiple))
        {
            if (syllable)
            {
                syllable = false;
                gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllableEnd, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private static float Peak(float[] data)
    {
        float levelMax = 0;
        for (int i = 0; i < data.Length; i++)
            levelMax = Mathf.Max(Mathf.Abs(data[i]),levelMax);
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
