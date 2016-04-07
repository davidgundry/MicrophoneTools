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
        /// <summary>
        /// An array of floats between -1 and 1 representing audio samples. This is a circular buffer, the write head is BufferPos
        /// </summary>
        public float[] Buffer { get { return buffer; } }

        private int bufferPos;
        /// <summary>
        /// The write head of the circular buffer in Buffer. The most recent data preceeds this index, non-inclusive.
        /// </summary>
        public int BufferPos { get { return bufferPos; } }

        private int sampleRate;
        /// <summary>
        /// The sample rate of the data in the buffer. This may or may not be the same as AudioSettings.outputSampleRate, depending on where MicrophoneBuffer is sourcing the data from.
        /// </summary>
        public int SampleRate { get { return sampleRate; } }

        private AudioClip audioClip;
        private bool audioPlaying = false;
        private double previousDSPTime;
        private double deltaDSPTime;

        void OnSoundEvent(SoundEvent soundEvent)
        {
            switch (soundEvent)
            {
                case SoundEvent.AudioStart:
                    audioPlaying = true;
                    audioClip = GetComponent<MicrophoneController>().audioClip;
                    buffer = new float[audioClip.samples*audioClip.channels];
                    sampleRate = audioClip.frequency;
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.BufferReady, SendMessageOptions.DontRequireReceiver);
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
                    BufferData(newData);
                }
            }
            else
                previousDSPTime = AudioSettings.dspTime;
        }

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
