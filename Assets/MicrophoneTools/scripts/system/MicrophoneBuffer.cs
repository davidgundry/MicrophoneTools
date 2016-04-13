﻿using UnityEngine;
using System.Collections;
using MicTools;
using System;

namespace MicTools
{
/// <summary>
/// Watches AudioClip provided by MicrohoneController and provides access to the latest samples
/// </summary>
[RequireComponent(typeof(MicrophoneController))]
[AddComponentMenu("MicrophoneTools/MicrophoneBuffer")]
public class MicrophoneBuffer : MonoBehaviour
{
    /// <summary>
    /// The sample rate of the data in the buffer. This may or may not be the same as
    /// AudioSettings.outputSampleRate, depending on where MicrophoneBuffer is sourcing the data from.
    /// </summary>
    public int SampleRate { get { if (audioClip != null) return audioClip.frequency; else return -1; } }

    /// <summary>
    /// The number of audio channels interleaved in the buffer.
    /// </summary>
    public int Channels { get { if (audioPlaying) return audioClip.channels; else return 0; } }

    /// <summary>
    /// The write head of the circular buffer in Buffer. The most recent data preceeds this index,
    /// non-inclusive.
    /// </summary>
    private int bufferPos;
    private AudioClip audioClip;
    private bool audioPlaying = false;
    private double previousDSPTime;
    private double deltaDSPTime;
    private bool waitingForAudio = true;

    void OnSoundEvent(SoundEvent soundEvent)
    {
        switch (soundEvent)
        {
            case SoundEvent.AudioStart:
                audioPlaying = true;
                audioClip = GetComponent<MicrophoneController>().AudioClip;
                waitingForAudio = true;
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

            if (waitingForAudio)
            {
                float[] newData = new float[audioClip.samples];
                audioClip.GetData(newData, 1);
                for (int i = newData.Length - 1; i >= 0; i--) // going backwards find end
                    //for (int i=0;i<buffer.Length;i++) // going forwards, find beginning
                    if (newData[i] != 0)
                    {
                        bufferPos = 0;
                        waitingForAudio = false;
                        gameObject.SendMessage("OnSoundEvent", SoundEvent.BufferReady, SendMessageOptions.DontRequireReceiver);
                        break;
                    }
            }
            else
            {
                int samplesPassed = (int) Math.Ceiling(deltaDSPTime * audioClip.frequency);
                bufferPos = (bufferPos + samplesPassed) % audioClip.samples;
            }
        }
        else
            previousDSPTime = AudioSettings.dspTime;
    }

    /// <summary>
    /// Encode an array of floats into 16-bit PCM
    /// </summary>
    /// <param name="data">An array of audio samples</param>
    /// <returns></returns>
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

    /// <summary>
    /// Return the n most recent samples from the AudioClip.
    /// </summary>
    /// <param name="count">The number of samples to fetch</param>
    /// <returns>Array of length provided of samples, -1 to 1</returns>
    public float[] GetMostRecentSamples(int count)
    {
        if (count > audioClip.samples)
            throw new ArgumentOutOfRangeException("Samples requested exceeds size of AudioClip.");
        if (count < 0)
            throw new ArgumentOutOfRangeException("Cannot fetch a negative number of samples.");

        float[] newSamples = new float[count];

        audioClip.GetData(newSamples, (bufferPos - count) % audioClip.samples);
        return newSamples;
    }
}
}