using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using MicTools;


namespace MicTools
{
[AddComponentMenu("MicrophoneTools/MicrophoneController")]
public class MicrophoneController : MonoBehaviour
{
    public bool microphoneActive = false;
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
    /// The number of channels of the microphone
    /// </summary>
    public int Channels { get { return channels; } }

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        microphoneUI = transform.GetComponent<MicrophoneUI>();
        if (microphoneUI != null)
            prewarned = !microphoneUI.AskPermission();
        else
        {
            LogMT.Log("No UI detected, defulting to skip permission step");
            prewarned = true;
        }

        if (FindObjectOfType<AudioListener>() == null)
        {
            LogMT.LogWarning("MicrophoneController: No AudioListener found, creating one");
            GameObject audioListener = new GameObject();
            audioListener.name = "AudioListener";
            audioListener.AddComponent<AudioListener>();
        }

        AudioMixer audioMixer = Resources.Load("MicrophoneToolsMixer") as AudioMixer;
        if (audioMixer == null)
            LogMT.LogError("MicrophoneController: Could not find Audio Mixer");
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
            else if (microphoneDeviceSet)
            {
                if (microphoneActive)
                    MicrophoneUpdate();
                else
                {
                    if ((listening))
                        StopListening();
                }
            }
        }

        if (!microphoneDeviceSet)
        {
            if (microphoneActive)
            {
                LogMT.LogWarning("MicrophoneController: Microphone active yet no microphone device set!");
                microphoneActive = false;
            }
        }
    }

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
        gameObject.SendMessage("OnSoundEvent", SoundEvent.AudioStart, SendMessageOptions.DontRequireReceiver);
    }

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


}
}