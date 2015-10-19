﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MicrophoneInput))]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("MicrophoneTools/SoundVisualiser")]
public class SoundVisualiser : MonoBehaviour {

    private MicrophoneInput microphoneInput;

    private float[] visualiserPoints;
    private byte[] pointFeatures;
    private int visualiserPosition = 0;

    private float halfCameraHeight;
    private float halfCameraWidth;
    
    private float magnification;
    private bool audioPlaying;

	void Awake ()
    {
        microphoneInput = GetComponent<MicrophoneInput>();

        halfCameraHeight = this.GetComponent<Camera>().orthographicSize;
        halfCameraWidth = this.GetComponent<Camera>().aspect * halfCameraHeight;
        visualiserPoints = new float[(int)halfCameraWidth * 2];
        pointFeatures = new byte[visualiserPoints.Length];
	}

    void Update()
    {
        if (audioPlaying)
        {
            visualiserPoints[visualiserPosition] = microphoneInput.Level;

            float noiseIntensity = microphoneInput.NoiseIntensity;

            float highest = 0;
            Color color = Color.white;
            for (int i = 0; i < visualiserPoints.Length; i++)
            {
                highest = Mathf.Max(highest, visualiserPoints[i]);

                if ((pointFeatures[i] & (1 << 0)) != 0)
                    color = Color.green;
                else if ((pointFeatures[i] & (1 << 1)) != 0)
                    color = Color.white;
                else
                    color = Color.grey;

                GLDebug.DrawLine(new Vector3(transform.position.x + i - halfCameraWidth, transform.position.y - halfCameraHeight, transform.position.z + 10), new Vector3(transform.position.x + i - halfCameraWidth, transform.position.y - halfCameraHeight + visualiserPoints[i] * magnification, transform.position.z + 10), color, 0, false);

                if ((pointFeatures[i] & (1 << 2)) != 0)
                {
                    GLDebug.DrawLine(new Vector3(transform.position.x + i - halfCameraWidth, transform.position.y - halfCameraHeight + visualiserPoints[i] * magnification, transform.position.z + 10), new Vector3(transform.position.x + i - halfCameraWidth, transform.position.y - halfCameraHeight + visualiserPoints[i] * magnification + 10, transform.position.z + 10), Color.blue, 0, false);
                }
            }
            magnification = 20 / highest;

            GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.activationMultiple * magnification, transform.position.z + 10), new Vector3(transform.position.x + visualiserPoints.Length - halfCameraWidth, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.activationMultiple * magnification, transform.position.z + 10), Color.red, 0, false);
            GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.deactivationMultiple * magnification, transform.position.z + 10), new Vector3(transform.position.x + visualiserPoints.Length - halfCameraWidth, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.deactivationMultiple * magnification, transform.position.z + 10), Color.red, 0, false);

            if (microphoneInput.InputDetected)
                GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.presenceMultiple * magnification, transform.position.z + 10), new Vector3(transform.position.x - halfCameraWidth + visualiserPoints.Length, transform.position.y - halfCameraHeight + noiseIntensity * MicrophoneInput.presenceMultiple * magnification, transform.position.z + 10), Color.blue, 0, false);

            visualiserPosition++;
            if (visualiserPosition >= visualiserPoints.Length)
            {
                visualiserPosition = 0;
                pointFeatures[visualiserPosition] = pointFeatures[pointFeatures.Length - 1];
            }
            else
                pointFeatures[visualiserPosition] = pointFeatures[visualiserPosition - 1];
            pointFeatures[visualiserPosition] = (byte)(pointFeatures[visualiserPosition] & (255 - (1 << 2)));
        }
    }

    void OnSoundEvent(SoundEvent soundEvent)
    {
        switch (soundEvent)
        {
            case SoundEvent.SyllableStart:
                pointFeatures[visualiserPosition] = (byte) (pointFeatures[visualiserPosition] | (1 << 0));
                break;
            case SoundEvent.SyllableEnd:
                pointFeatures[visualiserPosition] = (byte) (pointFeatures[visualiserPosition] & (255 - (1 << 0)));
                break;
            case SoundEvent.InputStart:
                pointFeatures[visualiserPosition] = (byte) (pointFeatures[visualiserPosition] | (1 << 1));
                break;
            case SoundEvent.InputEnd:
                pointFeatures[visualiserPosition] = (byte) (pointFeatures[visualiserPosition] & (255 - (1 << 0)));
                break;
            case SoundEvent.AudioStart:
                audioPlaying = true;
                break;
            case SoundEvent.AudioEnd:
                audioPlaying = false;
                visualiserPosition = 0;
                visualiserPoints = new float[(int)halfCameraWidth * 2];
                break;
            case SoundEvent.SyllablePeak:
                pointFeatures[visualiserPosition] = (byte)(pointFeatures[visualiserPosition] | (1 << 2));
                break;
        }
    }
}
