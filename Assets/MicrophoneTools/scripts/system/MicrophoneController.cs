using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using MicTools;
using System;
using System.IO;


namespace MicTools
{
/// <summary>
/// A MonoBehaviour to control detecting, configuring, starting and stopping the microphone. When
/// the microphone is active, provides an AudioClip with microphone data. Also provides methods
/// to access the latest samples from the microphone.
/// </summary>
[AddComponentMenu("MicrophoneTools/MicrophoneController")]
public class MicrophoneController : MonoBehaviour
{
    /// <summary>
    /// Controls whether to start or stop the microphone.
    /// </summary>
    public bool microphoneActive;
    /// <summary>
    /// Controls whether to log all incoming audio samples with LogMT
    /// </summary>
    public bool logAudioData = false;
    /// <summary>
    /// A clip to play in place of microphone input.
    /// </summary>
    public AudioClip testClip;

    private MicrophoneUI microphoneUI;
    private AudioClip audioClip;
    /// <summary>
    /// The AudioClip which is holds data from the microphone.
    /// </summary>
    public AudioClip AudioClip { get { return audioClip; } }

    private bool microphoneChoiceSent = false;
    private string microphoneDeviceName = "";
    private bool microphoneAvailable = true;
    private bool authorizationRequestSent = false;
    private bool microphoneDeviceSet = false;
    public bool MicrophoneDeviceSet { get { return microphoneDeviceSet; } }
    private bool prewarned = false;
    private bool prewarningSent = false;
    private bool listening = false;

    private const int defaultSampleRate = 44100;
    private int sampleRate = -1;
    /// <summary>
    /// The sample rate of the microphone
    /// </summary>
    public int SampleRate { get { return sampleRate; } }
    private int channels = 0;
    /// <summary>
    /// The number of audio channels interleaved in microphone data.
    /// </summary>
    public int Channels { get { return channels; } }

    /// <summary>
    /// The write head of the circular buffer in Buffer. The most recent data preceeds this index,
    /// non-inclusive.
    /// </summary>
    private int bufferPos;
    private double previousDSPTime;
    private double deltaDSPTime;
    private bool waitingForAudio = true;


    // For recording to file
    private FileStream fileStream;
    private const int headerSize = 44;
    public string saveDirectory = "MicrophoneTools";
    public string filesuffix = "";

    void Awake()
    {
        microphoneUI = transform.GetComponent<MicrophoneUI>();
        if (microphoneUI != null)
            prewarned = !microphoneUI.AskPermission();
        else
        {
            LogMT.Log("No UI detected, defulting to skip permission step");
            prewarned = true;
        }

        /*if (FindObjectOfType<AudioListener>() == null)
        {
            LogMT.LogWarning("MicrophoneController: No AudioListener found, creating one");
            GameObject audioListener = new GameObject();
            audioListener.name = "AudioListener";
            audioListener.AddComponent<AudioListener>();
        }

        AudioMixer audioMixer = Resources.Load("MicrophoneToolsMixer") as AudioMixer;
        if (audioMixer == null)
            LogMT.LogError("MicrophoneController: Could not find Audio Mixer");*/
    }

    /// <summary>
    /// Called every update when there is a configured microphone that is active
    /// </summary>
    private void MicrophoneUpdate()
    {
        if (!listening)
            StartListening();
    }

    void OnSoundEvent(SoundEvent soundEvent)
    {
        LogMT.Log(soundEvent.ToString());
        switch (soundEvent)
        {
            case SoundEvent.PermissionGranted:
                prewarned = true;
                break;
        }
    }

    void Update()
    {
        if (microphoneDeviceSet)
        {
            if (microphoneActive)
                MicrophoneUpdate();
            else if (listening) // If we're listening, but the microphone shouldn't be active
                StopListening();
        }
        else if (microphoneActive) // If the microphone should be active, but it is not configured
        {
            LogMT.LogWarning("MicrophoneController: Microphone active yet no microphone device set!");
            //microphoneActive = false;
        }

        if (listening)
            BufferTrackingUpdate();
        else
            MicrophoneConfigurationUpdate();

        previousDSPTime = AudioSettings.dspTime;
    }

    /// <summary>
    /// Watch for changes in microphone configuration status, possibly due to user interaction, and
    /// configure appropriately
    /// </summary>
    private void MicrophoneConfigurationUpdate()
    {
        if (microphoneAvailable)
        {
            if (!microphoneChoiceSent)
            {
                if (!prewarned)
                {
                    if (!prewarningSent)
                    {
                        gameObject.SendMessage("OnSoundEvent", SoundEvent.PermissionRequired, SendMessageOptions.DontRequireReceiver);
                        prewarningSent = true;
                    }
                }
                else if (Application.HasUserAuthorization(UserAuthorization.Microphone))
                {
                    if (Microphone.devices.Length > 0)
                    {
                        if (microphoneUI != null)
                        {
                            if (microphoneUI.UseDefaultMic())
                                UseDefaultDevice();
                            else
                                microphoneUI.ChooseDevice(Microphone.devices);
                        }
                        else
                            UseDefaultDevice();
                        microphoneChoiceSent = true;
                    }
                    else
                    {
                        if (microphoneUI != null)
                            microphoneUI.NoMicrophonesFound();
                        LogMT.LogWarning("MicrophoneController: No microphones found");
                        microphoneAvailable = false;
                    }
                }
                else if (!authorizationRequestSent)
                {
                    Application.RequestUserAuthorization(UserAuthorization.Microphone);
                    LogMT.Log("MicrophoneController: User authorization requested for microphone");
                    authorizationRequestSent = true;
                }
            }
        }
    }

    /// <summary>
    /// Track where we should consider the current position in the AudioClip for accessing samples.
    /// </summary>
    private void BufferTrackingUpdate()
    {
        deltaDSPTime = (AudioSettings.dspTime - previousDSPTime);

        if (waitingForAudio)
        {
            float[] newData = new float[audioClip.samples];
            audioClip.GetData(newData, 0);
            for (int i = newData.Length - 1; i >= 0; i--) // going backwards find end
                //for (int i=0;i<buffer.Length;i++) // going forwards, find beginning
                if (newData[i] != 0)
                {
                    bufferPos = 0;
                    waitingForAudio = false;
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.BufferReady, SendMessageOptions.DontRequireReceiver);
                    break;
                }
        }
        else
        {
            int samplesPassed = (int)Math.Ceiling(deltaDSPTime * audioClip.frequency);
            bufferPos = (bufferPos + samplesPassed) % audioClip.samples;

            if (logAudioData)
            {
                float[] newData = new float[samplesPassed];
                audioClip.GetData(newData, bufferPos);
                LogMT.SendByteDataBase64("MTaudio", EncodeFloatBlockTo16BitPCM(newData));
            }
        }
    }

    /// <summary>
    /// Start microphone access and collecting samples. Wait for a SoundEvent.BufferReady signal 
    /// through OnSoundEvent() before attempting to access sample data.
    /// </summary>
    private void StartListening()
    {
        if (testClip != null)
        {
            audioClip = testClip;
            channels = audioClip.channels;
            sampleRate = audioClip.frequency;
        }
        else
        {
            audioClip = Microphone.Start(microphoneDeviceName, true, 1, sampleRate);
            channels = 1; // Fetching the number of channels from the audio clip gives incorrect
                          // results, possibly due to a Unity bug
        }

        LogMT.Log("Audio Channels: " + channels);

        listening = true;
        waitingForAudio = true;
        gameObject.SendMessage("OnSoundEvent", SoundEvent.AudioStart, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// End microphone access.
    /// </summary>
    private void StopListening()
    {
        if (testClip == null)
            Destroy(audioClip);
        else
        {
            Microphone.End(microphoneDeviceName);
            audioClip = null;
        }

        listening = false;
        gameObject.SendMessage("OnSoundEvent", SoundEvent.AudioEnd, SendMessageOptions.DontRequireReceiver);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if ((microphoneDeviceSet) && (pauseStatus))
            StopListening();
    }

    void OnDisable()
    {
        StopListening();
    }

    void OnDestroy()
    {
        StopListening();
    }

    void OnApplicationQuit()
    {
        StopListening();
    }

    void OnApplicationFocus(bool focus)
    {
        if (!focus)
            StopListening();
    }

    /// <summary>
    /// Change the microphone to the given ID. Will fail if user authorisation has not been obtained
    /// or device doesn't exist.
    /// </summary>
    public void SetDevice(int id)
    {
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            if ((id >= 0) && (Microphone.devices.Length > 0))
            {
                microphoneDeviceName = Microphone.devices[id];
                LogMT.Log("MicrophoneController: Using microphone: " + microphoneDeviceName);
                microphoneDeviceSet = true;
                SetSamplingRate();
            }
            else
                LogMT.LogError("MicrophoneController: Cannot set device: Device not available", this);
        }
        else
            LogMT.LogError("MicrophoneController: Cannot set device: User Authorization required", this);
    }
        
    /// <summary>
    /// Change the microphone to the default microphone. Will fail if user authorisation has not
    /// been obtained or there are no microphones.
    /// </summary>
    public void UseDefaultDevice()
    {
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            if (Microphone.devices.Length > 0)
            {
                microphoneDeviceName = "";
                microphoneDeviceSet = true;
                SetSamplingRate();
                LogMT.Log("MicrophoneController: Using default microphone");
            }
            else
                LogMT.LogError("MicrophoneController: Cannot set device: No devices available", this);
        }
        else
            LogMT.LogError("MicrophoneController: Cannot set device: User Authorization required", this);
    }

    /// <summary>
    /// Make microphone be reconfigured next Update. Will re-request authorisation, offer choice if
    /// configured, but will not repeat pre-warning.
    /// </summary>
    public void ResetMicrophoneDevice()
    {
        microphoneDeviceSet = false;
        microphoneDeviceName = "";
        microphoneAvailable = true;
        authorizationRequestSent = false;
        microphoneChoiceSent = false;
    }

    /// <summary>
    /// Return the n most recent samples from the AudioClip.
    /// </summary>
    /// <param name="count">The number of samples to fetch</param>
    /// <returns>Array of length provided of samples, -1 to 1</returns>
    public float[] GetMostRecentSamples(int count)
    {
        if (audioClip == null)
            return new float[count];
        if (count > audioClip.samples)
            throw new ArgumentOutOfRangeException("Samples requested exceeds size of AudioClip.");
        if (count < 0)
            throw new ArgumentOutOfRangeException("Cannot fetch a negative number of samples.");

        float[] newSamples = new float[count];

        int offset = (bufferPos - count) % audioClip.samples;
        if (offset < 0)
            offset += audioClip.samples;
        audioClip.GetData(newSamples, offset);
        return newSamples;
    }

    /// <summary>
    /// Will set the sampling rate to the default, or the highest available from the microphone if
    /// this is lower than the default
    /// </summary>
    private void SetSamplingRate()
    {
        int min, max;
        Microphone.GetDeviceCaps(microphoneDeviceName, out min, out max);

        if (max == 0)
            sampleRate = defaultSampleRate;
        else if (defaultSampleRate > max)
            sampleRate = max;

        LogMT.Log("MicrophoneController: Sampling rate: " + sampleRate);
    }


    /// <summary>
    /// Encode an array of floats into 16-bit PCM
    /// </summary>
    /// <param name="data">An array of audio samples</param>
    /// <returns></returns>
    public static byte[] EncodeFloatBlockTo16BitPCM(float[] data)
    {
        byte[] bytes = new byte[data.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < data.Length; i++)
        {
            short intData;
            intData = (short)(data[i] * rescaleFactor);
            byte[] byteArr = new byte[2];
            byteArr = BitConverter.GetBytes(intData);
            byteArr.CopyTo(bytes, i * 2);
        }

        return bytes;
    }



    private void WriteDataToFile(float[] data)
    {
        if (fileStream != null)
        {

            short[] intData = new short[data.Length];
            //converting in 2 steps : float[] to Int16[], //then Int16[] to Byte[]

            byte[] bytesData = new byte[data.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i=0;i<data.Length;i++)
            {
                intData[i] = (short)(data[i] * rescaleFactor);
                byte[] byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }
        else
            Debug.LogError("Attempted to write to fileStream but it is null!");
    }

    private void StartWrite()
    {
        Debug.Log("MicrophoneController: Started write");

        string directory = LocalFilePath(saveDirectory);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string filePath = LocalFilePath(directory + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + filesuffix + ".wav");
        fileStream = new FileStream(filePath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < headerSize; i++) //preparing the header
        {
            fileStream.WriteByte(emptyByte);
        }
    }

    private void WriteHeader()
    {
        if (fileStream != null)
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            Byte[] audioFormat = BitConverter.GetBytes((Int16)1);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes((Int16)Channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRateWrite = BitConverter.GetBytes(SampleRate);
            fileStream.Write(sampleRateWrite, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(SampleRate * 2 * Channels);
            // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            Byte[] blockAlign = BitConverter.GetBytes((Int16)4);
            fileStream.Write(blockAlign, 0, 2);

            Byte[] bitsPerSample = BitConverter.GetBytes((Int16)16);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
            fileStream.Write(subChunk2, 0, 4);

            fileStream.Close();
            fileStream = null;
            Debug.Log("MicrophoneController: Completed write");
        }
        else
            Debug.LogError("MicrophoneController: Attempted to write header but fileStream was null!");
    }

    public static string LocalFilePath(string filename)
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            string path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(Path.Combine(path, "Documents"), filename);
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            string path = Application.persistentDataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            Debug.Log(Path.Combine(path, filename));
            return Path.Combine(path, filename);
        }
        else
        {
            string path = Application.dataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            Debug.Log(Path.Combine(path, filename));
            return Path.Combine(path, filename);
        }
    }

}
}