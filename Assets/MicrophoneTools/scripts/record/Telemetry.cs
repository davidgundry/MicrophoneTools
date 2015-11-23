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
        void Start()
        {
            TelemetryTools.Telemetry.Start();
        }

        void Update()
        {
            TelemetryTools.Telemetry.Update();
        }

        void OnSoundEvent(SoundEvent soundEvent)
        {
            TelemetryTools.Telemetry.SendEvent(SoundEventToString(soundEvent), System.DateTime.Now.Ticks);
        }

        void OnApplicationPause(bool pauseStatus)
        {

        }

        void OnDisable()
        {

        }

        void OnDestroy()
        {

        }

        void OnApplicationQuit()
        {
            TelemetryTools.Telemetry.End();
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