using UnityEngine;
using System.Collections;
using System.IO;
using MicTools;

namespace MicTools
{
    public enum SoundEvent
    {
        PermissionRequired,
        PermissionGranted,
        MicrophoneReady,
        SyllableStart,
        SyllableEnd,
        InputStart,
        InputEnd,
        AudioStart,
        AudioEnd,
        SyllablePeak
    }

    public class Telemetry : MonoBehaviour
    {
        public bool showRates;

        void Update()
        {
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.LostData, TelemetryTools.Telemetry.Instance.LostData);
            TelemetryTools.Telemetry.Instance.SendFrame();
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.Now.Ticks);

            TelemetryTools.Telemetry.Update();

            if (showRates)
                Debug.Log(TelemetryTools.Telemetry.GetPrettyLoggingRate());
        }

        public void ChangeToNewKey()
        {
            TelemetryTools.Telemetry.Instance.ChangeToNewKey();
        }

        public void ChangeToKey(uint key)
        {
            TelemetryTools.Telemetry.Instance.ChangeToKey(key);
        }

        public void UpdateUserData(string key, string value)
        {
            TelemetryTools.Telemetry.Instance.UpdateUserData(key,value);
        }

        public void UploadUserData()
        {
            TelemetryTools.Telemetry.Instance.UploadUserData();
        }

        void OnDestroy()
        {
            TelemetryTools.Telemetry.Stop();
        }

        void OnSoundEvent(SoundEvent soundEvent)
        {
            TelemetryTools.Telemetry.Instance.SendEvent(SoundEventToString(soundEvent), System.DateTime.Now.Ticks);
        }

        void OnApplicationPause(bool pauseStatus)
        {
           if (pauseStatus)
               TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationUnpause, System.DateTime.Now.Ticks);
            else
               TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationPause, System.DateTime.Now.Ticks);
        }

        void OnApplicationQuit()
        {
            TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationQuit, System.DateTime.Now.Ticks);
        }

        private static string SoundEventToString(SoundEvent e)
        {
            switch (e)
            {
                case SoundEvent.PermissionRequired:
                    return "Permission Required";
                case SoundEvent.PermissionGranted:
                    return "Permission Granted";
                case SoundEvent.MicrophoneReady:
                    return "Microphone Ready";
                case SoundEvent.SyllableStart:
                    return "Syllable Start";
                case SoundEvent.SyllableEnd:
                    return "Syllable End";
                case SoundEvent.InputStart:
                    return "Input Start";
                case SoundEvent.InputEnd:
                    return "Input End";
                case SoundEvent.AudioStart:
                    return "Audio Start";
                case SoundEvent.AudioEnd:
                    return "Audio End";
                case SoundEvent.SyllablePeak:
                    return "Syllable Peak";
            }
            return "Unrecognised Event";
        }
    }
}