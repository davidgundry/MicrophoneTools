using UnityEngine;
using System.Collections;
using MicTools;

namespace MicTools
{
    [AddComponentMenu("MicrophoneTools/MicrophoneBuffer")]
    public class MicrophoneBuffer : MonoBehaviour
    {

        private float[] buffer;
        public float[] Buffer
        {
            get
            {
                return buffer;
            }
        }
        private int bufferPos;
        public int BufferPos
        {
            get
            {
                return bufferPos;
            }
        }

        private bool audioPlaying = false;

        void Start()
        {
            buffer = new float[44100];
        }

        void OnSoundEvent(SoundEvent soundEvent)
        {
            switch (soundEvent)
            {
                case SoundEvent.AudioStart:
                    audioPlaying = true;
                    break;
                case SoundEvent.AudioEnd:
                    audioPlaying = false;
                    break;
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (audioPlaying)
                BufferData(data);
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
