using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using MicTools;


namespace MicTools
{
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("MicrophoneTools/MicrophoneController")]
    public class MicrophoneController : MonoBehaviour
    {

        public bool microphoneActive = false;
        public AudioClip testClip;

        private bool microphoneChoiceSent = false;
        private string microphoneDeviceName = "";
        private bool microphoneAvailable = true;
        private bool authorizationRequestSent = false;
        private bool microphoneDeviceSet = false;
        public bool MicrophoneDeviceSet { get { return microphoneDeviceSet; } }
        private bool prewarned = false;
        private bool prewarningSent = false;

        private AudioSource audioSource;
        private bool listening = false;
        private const int defaultSampleRate = 44100;
        public int sampleRate = defaultSampleRate;
        public int SampleRate { get { return sampleRate; } }
        public int channels = 0;
        public int Channels { get { return channels; } }

        private MicrophoneUI microphoneUI;


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

            audioSource = this.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.Stop();
            audioSource.loop = true;
            audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
        }

        /*
         * Called every update when there is a configured microphone that is active
         */
        private void MicrophoneUpdate()
        {
            if (listening)
            {
                if (!audioSource.isPlaying)
                    if (Microphone.GetPosition(microphoneDeviceName) > 0) // check if microphone is initialised
                        audioSource.Play();
            }
            else
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
                        if ((listening) || (audioSource.isPlaying))
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
                audioSource.clip = testClip;
                channels = audioSource.clip.channels;
                sampleRate = audioSource.clip.frequency;
                audioSource.Play();
            }
            else
            {
                audioSource.clip = Microphone.Start(microphoneDeviceName, true, 1, sampleRate);
                channels = 1; // Fetching the number of channels from the audio clip gives incorrect results (for some reason)
            }

            LogMT.Log("Audio Channels: " + channels);

            listening = true;
            gameObject.SendMessage("OnSoundEvent", SoundEvent.AudioStart, SendMessageOptions.DontRequireReceiver);
        }

        private void StopListening()
        {
            audioSource.Stop();

            if (testClip == null)
                Destroy(audioSource.clip);
            else
            {
                Microphone.End(microphoneDeviceName);
                audioSource.clip = null;
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

        /*
         * Change the microphone to the given ID. Will fail if user authorisation has not been obtained or device doesn't exist.
         */
        public void SetDevice(int id)
        {
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                if ((id >= 0) && (Microphone.devices.Length > 0))
                {
                    microphoneDeviceName = Microphone.devices[0];
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

        /*
         * Change the microphone to the default microphone. Will fail if user authorisation has not been obtained or there are no microphones.
         */
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

        /*
         * Make microphone be reconfigured next Update. Will re-request authorisation, offer choice if configured, but will not repeat pre-warning.
         */
        public void ResetMicrophoneDevice()
        {
            microphoneDeviceSet = false;
            microphoneDeviceName = "";
            microphoneAvailable = true;
            authorizationRequestSent = false;
            microphoneChoiceSent = false;
        }

        /*
         * Will set the sampling rate to the default, or the highest if max available is lower
         */
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