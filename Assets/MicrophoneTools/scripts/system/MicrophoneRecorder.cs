using UnityEngine;
using System.Collections;
using System;
using System.IO;
using MicTools;

namespace MicTools
{
    [RequireComponent(typeof(MicrophoneController))]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("MicrophoneTools/MicrophoneRecorder")]
    public class MicrophoneRecorder : MonoBehaviour
    {

        public bool recording = true;

        public string saveDirectory = "MicrophoneTools";
        public string filesuffix = "";

        private bool paused = false;

        private MicrophoneController microphoneController;
        private bool audioPlaying = false;

        private FileStream fileStream;
        private const int headerSize = 44;

        private float[] buffer;
        private int bufferPos;
        private int bufferReadPos;

        void Awake()
        {
            microphoneController = this.GetComponent<MicrophoneController>();
            buffer = new float[44100];
        }

        void Update()
        {
            if (!paused)
            {
                if ((recording) && (audioPlaying))
                {
                    if (fileStream == null)
                        StartWrite();
                    else
                        WriteFromBuffer();
                }
                else if (fileStream != null)
                    WriteHeader();
            }
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

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (fileStream != null)
                    WriteHeader();
                paused = true;
            }
            else
                paused = false;
        }

        void OnDisable()
        {
            if (fileStream != null)
                WriteHeader();
        }

        void OnDestroy()
        {
            if (fileStream != null)
                WriteHeader();
        }

        void OnApplicationQuit()
        {
            if (fileStream != null)
                WriteHeader();
        }

        /*
         * In the editor, the application loses focus when toggling variables. 
         */
        /*void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                if (fileStream != null)
                    WriteHeader();
                paused = true;
            }
            else
                paused = false;
        }*/

        void OnAudioFilterRead(float[] data, int channels)
        {
            Buffer(data);
        }

        private void Buffer(float[] data)
        {
            //TelemetryTools.Telemetry.Instance.SendStreamValueBlock("mic", data);
            for (int i = 0; i < data.Length; i++)
            {
                buffer[bufferPos] = data[i];
                bufferPos = (bufferPos + 1) % buffer.Length;
            }
        }

        private void WriteFromBuffer()
        {
            if (fileStream != null)
            {
                int dataLength = 0;
                if (bufferReadPos > bufferPos)
                    dataLength = buffer.Length - bufferReadPos + bufferPos;
                else
                    dataLength = bufferPos - bufferReadPos;

                short[] intData = new short[dataLength];
                //converting in 2 steps : float[] to Int16[], //then Int16[] to Byte[]

                byte[] bytesData = new byte[dataLength * 2];
                //bytesData array is twice the size of
                //dataSource array because a float converted in Int16 is 2 bytes.

                int rescaleFactor = 32767; //to convert float to Int16

                int i = 0;
                while ((bufferReadPos != bufferPos) && (i < dataLength))
                {
                    intData[i] = (short)(buffer[bufferReadPos] * rescaleFactor);
                    byte[] byteArr = new byte[2];
                    byteArr = BitConverter.GetBytes(intData[i]);
                    byteArr.CopyTo(bytesData, i * 2);
                    i++;
                    bufferReadPos = (bufferReadPos + 1) % buffer.Length;
                }

                fileStream.Write(bytesData, 0, bytesData.Length);
            }
            else
                Debug.LogError("Attempted to write to fileStream but it is null!");
        }

        private void StartWrite()
        {
            Debug.Log("MicrophoneRecorder: Started write");

            string directory = LocalFilePath(saveDirectory);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = LocalFilePath(directory + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + filesuffix + ".wav");
            fileStream = new FileStream(filePath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < headerSize; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }
        }

        private void WriteHeader()
        {
            if (fileStream != null)
            {
                fileStream.Seek(0, SeekOrigin.Begin);

                Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
                fileStream.Write(riff, 0, 4);

                Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
                fileStream.Write(chunkSize, 0, 4);

                Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
                fileStream.Write(wave, 0, 4);

                Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
                fileStream.Write(fmt, 0, 4);

                Byte[] subChunk1 = BitConverter.GetBytes(16);
                fileStream.Write(subChunk1, 0, 4);

                Byte[] audioFormat = BitConverter.GetBytes((Int16)1);
                fileStream.Write(audioFormat, 0, 2);

                Byte[] numChannels = BitConverter.GetBytes((Int16)microphoneController.Channels);
                fileStream.Write(numChannels, 0, 2);

                Byte[] sampleRateWrite = BitConverter.GetBytes(microphoneController.SampleRate);
                fileStream.Write(sampleRateWrite, 0, 4);

                Byte[] byteRate = BitConverter.GetBytes(microphoneController.SampleRate * 2 * microphoneController.Channels);
                // sampleRate * bytesPerSample*number of channels, here 44100*2*2
                fileStream.Write(byteRate, 0, 4);

                Byte[] blockAlign = BitConverter.GetBytes((Int16)4);
                fileStream.Write(blockAlign, 0, 2);

                Byte[] bitsPerSample = BitConverter.GetBytes((Int16)16);
                fileStream.Write(bitsPerSample, 0, 2);

                Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
                fileStream.Write(dataString, 0, 4);

                Byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
                fileStream.Write(subChunk2, 0, 4);

                fileStream.Close();
                fileStream = null;
                Debug.Log("MicrophoneRecorder: Completed write");
            }
            else
                Debug.LogError("MicrophoneRecorder: Attempted to write header but fileStream was null!");
        }

        public static string LocalFilePath(string filename)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                string path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(Path.Combine(path, "Documents"), filename);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                string path = Application.persistentDataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                Debug.Log(Path.Combine(path, filename));
                return Path.Combine(path, filename);
            }
            else
            {
                string path = Application.dataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                Debug.Log(Path.Combine(path, filename));
                return Path.Combine(path, filename);
            }
        }
    }
}