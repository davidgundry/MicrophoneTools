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
        private float[] buffer = new float[0];
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

                    LogMT.SendByteDataBase64("MTaudio", EncodeFloatBlockToRawAudioBytes(newData));

                    if (bufferPos + samplesPassed < buffer.Length)
                        System.Buffer.BlockCopy(newData, 0, buffer, bufferPos * sizeof(float), samplesPassed * sizeof(float));
                    else
                    {
                        int firstSamples = buffer.Length-bufferPos;
                        int secondSamples = samplesPassed - firstSamples;
                        System.Buffer.BlockCopy(newData, 0,                            buffer, bufferPos * sizeof(float), firstSamples * sizeof(float));
                        System.Buffer.BlockCopy(newData, firstSamples * sizeof(float), buffer, 0,                         secondSamples * sizeof(float));
                    }

                    bufferPos = (bufferPos + samplesPassed) % buffer.Length;
                }
            }
            else
                previousDSPTime = AudioSettings.dspTime;
        }

        private static byte[] EncodeFloatBlockToRawAudioBytes(float[] data)
        {
            byte[] bytes = new byte[data.Length * 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < data.Length; i++)
            {
                short intData;
                intData = (short)(data[i] * rescaleFactor);
                byte[] byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(intData);
                byteArr.CopyTo(bytes, i * 2);
            }

            return bytes;
        }
    }
}
