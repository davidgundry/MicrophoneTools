using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MicrophoneController))]
//[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("MicrophoneTools/MicrophoneInput")]
public class MicrophoneInput : MonoBehaviour {

    public int syllables = 0;
    private bool syllable = false;

    public const float activationMultiple = 1.5848931924611136f;  //2dB
    public const float highActivationMultiple = 2f;
    public const float dipMultiple = 1.5848931924611136f; //2dB
    public const float deactivationMultiple = 3f; //0dB
    public const float presenceMultiple = 1f;

    private float peak = 0f;
    private float dip = 0f;
    private bool dipped = true;

    private float noiseIntensity = 1f;
    public float NoiseIntensity
    {
        get
        {
            return noiseIntensity;
        }
    }
    private int samplesSoFar = 0;

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

    //private float[] spectrum;
    //private const int sampleWindow = 128;
    //private const int spectrumSize = 8192;
    private bool audioPlaying = false;

    private int inputDetectionTimeout = 0;

    private float[] buffer;
    private int bufferPos;
    private int bufferReadPos;
    private float timeStep = 0.02f;
    private double elapsedTime = 0;

	void Awake()
    {
        buffer = new float[44100];
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 50;
        //audioSource = this.GetComponent<AudioSource>();
        //spectrum = new float[spectrumSize];
	}

    void OnSoundEvent(SoundEvent soundEvent)
    {
        switch (soundEvent)
        {
            case SoundEvent.AudioStart:
                audioPlaying = true;
                break;
            case SoundEvent.AudioEnd:
                audioPlaying = false;
                break;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (audioPlaying)
            Buffer(data);
    }

    private void Buffer(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            buffer[bufferPos] = data[i];
            bufferPos = (bufferPos + 1) % buffer.Length;
        }
    }

    void Update()
    {
        elapsedTime += AudioSettings.dspTime;
        if (elapsedTime >= timeStep)
        {
            NewWindow();
            elapsedTime = 0;
        }
    }

    void NewWindow()
    {
        int newSamples = 0;
        if (bufferReadPos > bufferPos)
            newSamples = buffer.Length - bufferReadPos + bufferPos;
        else
            newSamples = bufferPos - bufferReadPos;

        if (newSamples > 0)
        {
            samplesSoFar += newSamples;

            float[] data = new float[newSamples];
            int i = 0;
            while ((bufferReadPos != bufferPos) && (i < newSamples))
            {
                data[i] = buffer[bufferReadPos];
                i++;
                bufferReadPos = (bufferReadPos + 1) % buffer.Length;
            }
            float sumIntensity = 0;
            if (!SinglePolaity(data))
            {
                sumIntensity = SumAbsIntensity(data);
                level = sumIntensity / newSamples;
                if (!syllable)
                    noiseIntensity += (sumIntensity - noiseIntensity * newSamples) / Mathf.Min(44100 * 4, samplesSoFar);

                DetectNuclei();
                DetectSyllables();
                DetectPresence();
            }
            else
                level = 0;
        }
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

    private static float SumIntensity(float[] data)
    {
        float sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    private static bool SinglePolaity(float[] data) //Poor-man's high-pass filter to remove v.low frequency noice
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
        return true;
    }

   /* private float FrequencyLevel(float frequency)
    {
        int index = (int) Mathf.Round(((1 / 48000) * spectrumSize * 2) * frequency);
        return spectrum[index];
    }

    private int FrequencyToIndex(float frequency)
    {
        return (int)Mathf.Round(((1 / 48000) * spectrumSize * 2) * frequency);
    }

    private float SumSpectrumArea(float low, float high)
    {
        int min = FrequencyToIndex(low);
        int max = spectrum.Length-1;
        if (high != 0)
            max = FrequencyToIndex(high);

        float sum = 0;
        for (int i = min; i <= max; i++)
        {
            sum += spectrum[i];
        }
        return sum;
    }*/

}
