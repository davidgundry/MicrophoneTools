using UnityEngine;
using System.Collections;

namespace MicTools
{
    public class LogMT
    {
        public static void Log(string text)
        {
            //if (Debug.isDebugBuild)
            //    Debug.Log(text);
        }
        public static void LogWarning(string text)
        {
            //if (Debug.isDebugBuild)
            //     Debug.LogWarning(text);
        }
        public static void LogError(string text)
        {
            //if (Debug.isDebugBuild)
            //     Debug.LogError(text);
        }
        public static void LogError(string text, Object o)
        {
            //if (Debug.isDebugBuild)
            //     Debug.LogError(text,o);
        }
        public static void SendStreamValue(string tag, System.ValueType value)
        {
            TelemetryTools.Telemetry.Instance.SendStreamValue(tag, value);
        }

        public static void SendByteDataBase64(string tag, byte[] data)
        {
            TelemetryTools.Telemetry.Instance.SendByteDataBase64(tag, data);
        }
        
    }
}