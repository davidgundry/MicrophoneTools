using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TelemetryTools
{
    [CustomEditor(typeof(MicTools.Telemetry))]
    public class TMonitorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MicTools.Telemetry myScript = (MicTools.Telemetry)target;
            if (GUILayout.Button("Switch Key"))
            {
                myScript.ChangeToNewKey();
            }
        }
    }
}