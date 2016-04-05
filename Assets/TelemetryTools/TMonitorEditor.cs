#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TelemetryTools
{
    [CustomEditor(typeof(TelemetryTools.TelemetryMonitor))]
    public class TMonitorEditor : Editor
    {
        private int keyToChangeTo;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TelemetryTools.TelemetryMonitor telemetryMonitor = (TelemetryTools.TelemetryMonitor)target;

            EditorGUILayout.LabelField("UploadURL", TelemetryTools.Telemetry.Instance.UploadURL);
            EditorGUILayout.LabelField("Key Server", TelemetryTools.Telemetry.Instance.KeyManager.KeyServer);
            EditorGUILayout.LabelField("User Data URL", TelemetryTools.Telemetry.Instance.UserDataURL);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Total HTTP Requests", TelemetryTools.ConnectionLogger.Instance.TotalHTTPRequestsSent.ToString());
            EditorGUILayout.LabelField("Total HTTP Success", TelemetryTools.ConnectionLogger.Instance.TotalHTTPSuccess.ToString());
            EditorGUILayout.LabelField("Total HTTP Errors", TelemetryTools.ConnectionLogger.Instance.TotalHTTPErrors.ToString());

            EditorGUILayout.Space();

            //EditorGUILayout.LabelField("Log Input", Mathf.Round(TelemetryTools.Telemetry.Instance.LoggingRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("HTTP", Mathf.Round(TelemetryTools.Telemetry.Instance.HTTPPostRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("File", Mathf.Round(TelemetryTools.Telemetry.Instance.LocalFileSaveRate / 1024) + " KB/s");
            EditorGUILayout.LabelField("Total", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.DataLogged / 1024) + " KB");
            EditorGUILayout.LabelField("Cached Files", TelemetryTools.Telemetry.Instance.CachedFiles.ToString());
            EditorGUILayout.LabelField("User Data Files", TelemetryTools.Telemetry.Instance.UserDataFiles.ToString());
            EditorGUILayout.LabelField("Lost Data", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.LostData / 1024) + " KB");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Used Keys", TelemetryTools.Telemetry.Instance.KeyManager.NumberOfUsedKeys.ToString());
            EditorGUILayout.LabelField("Keys", TelemetryTools.Telemetry.Instance.KeyManager.NumberOfKeys.ToString());
            EditorGUILayout.LabelField("Current Key", "ID:" + TelemetryTools.Telemetry.Instance.KeyManager.CurrentKeyID.ToString() + " " + TelemetryTools.Telemetry.Instance.KeyManager.CurrentKey);

            /*EditorGUILayout.IntField("Key", keyToChangeTo);
            if (GUILayout.Button("Change Key"))
            {
                myScript.ChangeKey((uint) keyToChangeTo);
            }*/
            if (GUILayout.Button("New Key"))
            {
                telemetryMonitor.ChangeKey();
                telemetryMonitor.UpdateUserData("test", "test");
            }

            Repaint();

        }
    }
}
#endif