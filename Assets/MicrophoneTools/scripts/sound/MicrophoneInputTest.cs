#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace MicTools
{
    [CustomEditor(typeof(MicTools.MicrophoneInput))]
    public class MicrophoneInputTest : Editor
    {
        public AudioClip testClip;

        protected MicrophoneInput microphoneInput;

        void Awake()
        {
            microphoneInput = (MicrophoneInput)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            testClip = (AudioClip)EditorGUILayout.ObjectField("Test Clip", testClip, typeof(AudioClip), false);
            if (GUILayout.Button("Test"))
            {
                Debug.Log("Test Result: Syllables: " + microphoneInput.RunTest(testClip));
            }

            Repaint();

        }
    }
}
#endif