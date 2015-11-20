using UnityEngine;
using System.Collections;
using System;
using MicTools;

namespace TeleTools
{

    public class HTTPSender : MonoBehaviour
    {

        private const string uploadURL = "http://localhost";
        private float[] audioBuffer;
        private bool audioBufferUpdated;

        // Use this for initialization
        void Start()
        {
            audioBuffer = new float[2048];
        }

        // Update is called once per frame
        void Update()
        {
            if (audioBufferUpdated) // For testing, unlikely to be reliable enough, buffering will be needed.
            {
                SendSoundFile(audioBuffer);
                audioBufferUpdated = false;
            }
        }

        void OnSoundEvent(SoundEvent soundEvent)
        {
            SendEvent(new EventRecord(soundEvent));
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            audioBuffer = data;
            audioBufferUpdated = true;
        }


        private void SendEvent(EventRecord e)
        {

            WWWForm form = new WWWForm();
            form.AddField(e.Key, e.Value);
            form.AddField("time", e.Time.ToString());
            //form.AddBinaryData("fileUpload", bytes, "screenShot.png", "image/png");
            WWW w = new WWW(uploadURL, form);
            /*yield return w;

            if (!string.IsNullOrEmpty(w.error))
            {
                print(w.error);
            }
            else
            {
                print("Finished Uploading Screenshot");
            }*/
        }

        private void SendSoundFile(float[] samples)
        {
            byte[] data = new byte[samples.Length * 4];
            int next = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(samples[i]);
                for (int j = 0; j < bytes.Length; j++)
                {
                    data[next] = bytes[j];
                    next++;
                }
            }

            WWWForm form = new WWWForm();
            form.AddField("time", "");
            form.AddBinaryData("audio", data, "audio", "audio/wav");
            WWW w = new WWW(uploadURL, form);
        }
    }
}