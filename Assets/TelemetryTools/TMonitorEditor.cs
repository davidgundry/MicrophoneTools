#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TelemetryTools
{
    [CustomEditor(typeof(MicTools.Telemetry))]
    public class TMonitorEditor : Editor
    {
        private int keyToChangeTo;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MicTools.Telemetry telemetryMonitor = (MicTools.Telemetry)target;

            //EditorGUILayout.LabelField("Log Input", Mathf.Round(TelemetryTools.Telemetry.Instance.LoggingRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("HTTP", Mathf.Round(TelemetryTools.Telemetry.Instance.HTTPPostRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("File", Mathf.Round(TelemetryTools.Telemetry.Instance.LocalFileSaveRate / 1024) + " KB/s");
            EditorGUILayout.LabelField("Total", Mathf.Round(TelemetryTools.Telemetry.Instance.DataLogged / 1024) + " KB");
            EditorGUILayout.LabelField("Cached Files", TelemetryTools.Telemetry.Instance.CachedFiles.ToString());
            EditorGUILayout.LabelField("Lost Data", Mathf.Round(TelemetryTools.Telemetry.Instance.LostData / 1024) + " KB");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Used Keys", TelemetryTools.Telemetry.Instance.NumberOfUsedKeys.ToString());
            EditorGUILayout.LabelField("Keys", TelemetryTools.Telemetry.Instance.NumberOfKeys.ToString());
            EditorGUILayout.LabelField("Current Key", "ID:" + TelemetryTools.Telemetry.Instance.CurrentKeyID.ToString() + " " + TelemetryTools.Telemetry.Instance.CurrentKey);

            /*EditorGUILayout.IntField("Key", keyToChangeTo);
            if (GUILayout.Button("Change Key"))
            {
                myScript.ChangeToKey((uint) keyToChangeTo);
            }*/
            if (GUILayout.Button("New Key"))
            {
                telemetryMonitor.ChangeToNewKey();
                telemetryMonitor.UpdateUserData("test", "test");
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Upload User Data"))
            {
                telemetryMonitor.UploadUserData();
            }

            Repaint();
        }
    }
}

#endif