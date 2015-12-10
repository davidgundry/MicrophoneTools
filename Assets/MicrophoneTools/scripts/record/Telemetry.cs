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
        void Update()
        {
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.LostData, TelemetryTools.Telemetry.Instance.LostData);
            TelemetryTools.Telemetry.Instance.SendFrame();
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.Now.Ticks);

            TelemetryTools.Telemetry.Update();

            //Debug.Log(TelemetryTools.Telemetry.GetPrettyLoggingRate());
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