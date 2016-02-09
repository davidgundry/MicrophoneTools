using UnityEngine;

namespace TelemetryTools
{
    public class TelemetryMonitor : MonoBehaviour
    {
        public bool showLogging;

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
            TelemetryTools.Telemetry.Instance.UpdateUserData(key, value);
        }

        public void UploadUserData()
        {
            TelemetryTools.Telemetry.Instance.UploadUserData();
        }

        void Awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        void Update()
        {
            TelemetryTools.Telemetry.Update();

            if (showLogging)
                Debug.Log(TelemetryTools.Telemetry.GetPrettyLoggingRate());
        }

        void OnDestroy()
        {
           TelemetryTools.Telemetry.Stop();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationUnpause);
            else
                TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationPause);
        }

        void OnApplicationQuit()
        {
            TelemetryTools.Telemetry.Instance.SendEvent(TelemetryTools.Event.ApplicationQuit);
        }

    }
}