using UnityEngine;
using System.Collections;
using System;
using MicTools;

namespace MicTools
{
    [RequireComponent(typeof(MicrophoneController))]
    [AddComponentMenu("MicrophoneTools/MicrophoneInput")]
    public class MicrophoneInput : MonoBehaviour
    {
        private MicrophoneController microphoneController;

        private bool test;
        public float pitch;

        private Yin yin;
        private SyllableDetectionAlgorithm syllableDetectionAlgorithm;

        public float Level { get { if (syllableDetectionAlgorithm != null) return syllableDetectionAlgorithm.Level; else return -1; } }
        public float NormalisedPeakAutocorrelation { get { if (syllableDetectionAlgorithm != null) return syllableDetectionAlgorithm.NormalisedPeakAutocorrelation; else return -1; } }

        void Awake()
        {
            microphoneController = GetComponent<MicrophoneController>();
        }

        public void OnSoundEvent(SoundEvent e)
        {
            switch (e)
            {
                case SoundEvent.BufferReady:
                    yin = new Yin(microphoneController.SampleRate, SyllableDetectionAlgorithm.windowSize);
                    LogMT.Log("Window Size for Algorithm: " + SyllableDetectionAlgorithm.windowSize);

                    syllableDetectionAlgorithm = new SyllableDetectionAlgorithm(microphoneController.SampleRate);
                    break;
            }
        }

        
        public static int RunTest(AudioClip testClip)
        {
            SyllableDetectionAlgorithm testSDA = new SyllableDetectionAlgorithm(testClip.frequency);

            int syllables = 0;
            if (testClip != null)
            {
                int length = SyllableDetectionAlgorithm.windowSize;// (int)(GetComponent<MicrophoneController>().SampleRate * timeStep);
                float[] samples = new float[length];

                if (testClip != null)
                    for (int i = 0; i < testClip.samples; i += length)
                    {
                        if (i + length > testClip.samples)
                            samples = new float[testClip.samples - i];
                        //else if (samples.Length != length)
                        //    samples = new float[length];

                        testClip.GetData(samples, i);
                        if (testSDA.Run(samples, samples.Length / testClip.frequency))
                            syllables++;
                    }
            }
            else
                Debug.LogWarning("Cannot test Microphone Input without a test clip.");

             return syllables;
        }

        void Update()
        {
            float[] window = microphoneController.GetMostRecentSamples(SyllableDetectionAlgorithm.windowSize);
            
            if (yin != null)
                pitch = yin.getPitch(window);

            if (syllableDetectionAlgorithm != null)
                if (syllableDetectionAlgorithm.Run(window, Time.deltaTime))
                {
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.SyllablePeak, SendMessageOptions.DontRequireReceiver);
                }
        }
    }
}
