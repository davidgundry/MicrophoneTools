using UnityEngine;

namespace TelemetryTools
{
    public class TelemetryMonitor : MonoBehaviour
    {
        public bool showLogging;

        void Awake()
        {
            DontDestroyOnLoad(this);

            if (FindObjectsOfType(GetType()).Length > 1)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            string baseurl = PlayerPrefs.GetString("URL");
            TelemetryTools.Telemetry.Instance.UploadURL = baseurl + "/import.php";
            TelemetryTools.Telemetry.Instance.KeyManager.KeyServer = baseurl + "/key.php";
            TelemetryTools.Telemetry.Instance.UserDataURL = baseurl + "/userdata.php";
        }

        public void ChangeKey()
        {
            TelemetryTools.Telemetry.Instance.KeyManager.ChangeKey();
        }

        public void ChangeKey(uint key)
        {
            TelemetryTools.Telemetry.Instance.KeyManager.ChangeKey(key);
        }

        public void WriteEverything()
        {
            TelemetryTools.Telemetry.Instance.WriteEverything();
        }

        public void UpdateUserData(string key, string value)
        {
            TelemetryTools.Telemetry.Instance.UpdateUserData(key, value);
        }

        void Update()
        {
            TelemetryTools.Telemetry.Update();

            if (showLogging)
                Debug.Log(TelemetryTools.ConnectionLogger.GetPrettyLoggingRate());
        }

        void OnDestroy()
        {
           TelemetryTools.Telemetry.Instance.WriteEverything();
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