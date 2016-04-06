using UnityEngine;
using System.Collections;
using MicTools;
using System;

namespace MicTools
{
    [RequireComponent(typeof(MicrophoneController))]
    [AddComponentMenu("MicrophoneTools/MicrophoneBuffer")]
    public class MicrophoneBuffer : MonoBehaviour
    {
        private float[] buffer;
        public float[] Buffer { get { return buffer; } }
        private int bufferPos;
        public int BufferPos { get { return bufferPos; } }

        private bool audioPlaying = false;

        private double previousDSPTime;
        private double deltaDSPTime;

        AudioClip audioClip;
        private int sampleRate;
        /// <summary>
        /// The sample rate of the data in the buffer. This may or may not be the same as AudioSettings.outputSampleRate, depending on where MicrophoneBuffer is sourcing the data from.
        /// </summary>
        public int SampleRate { get { return sampleRate; } }

        void Start()
        {
            //buffer = new float[44100];
        }

        void OnSoundEvent(SoundEvent soundEvent)
        {
            switch (soundEvent)
            {
                case SoundEvent.AudioStart:
                    audioPlaying = true;
                    audioClip = GetComponent<MicrophoneController>().audioClip;
                    buffer = new float[audioClip.samples*audioClip.channels];
                    sampleRate = audioClip.frequency;
                    break;
                case SoundEvent.AudioEnd:
                    audioPlaying = false;
                    break;
            }
        }

        void Update()
        {
            if (audioPlaying)
            {
                deltaDSPTime = (AudioSettings.dspTime - previousDSPTime);
                previousDSPTime = AudioSettings.dspTime;

                int samplesPassed = (int) Math.Ceiling(deltaDSPTime*audioClip.frequency);
                if (samplesPassed > 0)
                {
                    float[] newData = new float[samplesPassed];
                    audioClip.GetData(newData, bufferPos);
                    //TelemetryTools.Telemetry.Instance.SendStreamValue("samplesPassed", samplesPassed);

                    //TelemetryTools.Telemetry.Instance.SendStreamValue("newData.Length", newData.Length);
                    //TelemetryTools.Telemetry.Instance.SendStreamValue("buffer.Length", buffer.Length);
                    //TelemetryTools.Telemetry.Instance.SendStreamValue("bufferPos", bufferPos);

                    BufferData(newData);

                    /*if (newData.Length < buffer.Length - bufferPos)
                        System.Buffer.BlockCopy(newData, 0, buffer, bufferPos, newData.Length);
                    else
                    {
                        System.Buffer.BlockCopy(newData, 0, buffer, bufferPos, buffer.Length - bufferPos);
                        //System.Buffer.BlockCopy(newData, buffer.Length - bufferPos, buffer, 0, newData.Length - (buffer.Length - bufferPos)-1);
                    }
                    bufferPos = (bufferPos + samplesPassed) % buffer.Length;*/
                    //TelemetryTools.Telemetry.Instance.SendStreamValueBlock("buffer", buffer);
                }
            }
            else
                previousDSPTime = AudioSettings.dspTime;
        }

        /*void OnAudioFilterRead(float[] data, int channels)
        {
            if (audioPlaying)
                BufferData(data);
        }*/

        private void BufferData(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer[bufferPos] = data[i];
                bufferPos = (bufferPos + 1) % buffer.Length;
            }
        }

    }
}
