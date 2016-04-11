using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MicTools;

namespace MicTools
{
    [RequireComponent(typeof(FFTPitchDetector))]
    //[RequireComponent(typeof(VowelFinder))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GLDebug))]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("MicrophoneTools/VowelVisualiser")]
    public class SpectrumVisualiser : MonoBehaviour
    {

        //private Text vowelText;
        private FFTPitchDetector fftPitchDetector;
        //private VowelFinder vowelFinder;
        //private Canvas canvas;

        private float halfCameraHeight;
        private float halfCameraWidth;

        //private const int activationThreshold = 20;

        private const float magnification = 10;
        private const int zPos = 10;

        void Start()
        {
            fftPitchDetector = GetComponent<FFTPitchDetector>();
            //vowelFinder = GetComponent<VowelFinder>();
            halfCameraHeight = this.GetComponent<Camera>().orthographicSize;
            halfCameraWidth = this.GetComponent<Camera>().aspect * halfCameraHeight;
            //Canvas canvas = GetComponent<Canvas>();

            //vowelText = Instantiate(Resources.Load("vowel/VowelText", typeof(Text))) as Text;
            //vowelText.transform.SetParent(transform, false);
            //vowelText.transform.position = transform.position;
        }

        void Update()
        {
            float[] spectrum = fftPitchDetector.Spectrum;
            //int formant = 0;
            Color color = Color.red;
            for (int i = 1; i < spectrum.Length - 1; i++)
                GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth + i, transform.position.y - halfCameraHeight + 10, transform.position.z + zPos), new Vector3(transform.position.x - halfCameraWidth + i, transform.position.y - halfCameraHeight + spectrum[i] * magnification + 10, transform.position.z + zPos), color, 0, true);

            //{
                /*if (formant < formantFinder.Formants.Length)
                    if (i == formantFinder.Formants[formant].LowerBound)
                        color = Color.yellow;
                    else if (i - 1 == formantFinder.Formants[formant].HigherBound)
                    {
                        color = Color.red;
                        formant++;
                    }
                    else if (i == formantFinder.Formants[formant].Peak)
                        color = Color.white;
                    else if (i - 1 == formantFinder.Formants[formant].Peak)
                        color = Color.yellow;
                */
             //}

            // Draw Noise Line
            //GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth, transform.position.y - halfCameraHeight + formantFinder.NoiseLevel * activationThreshold * magnification + 10, transform.position.z + zPos), new Vector3(transform.position.x + halfCameraWidth, transform.position.y - halfCameraHeight + formantFinder.NoiseLevel * activationThreshold * magnification + 10, transform.position.z + zPos), Color.blue, 0, true);

            //vowelText.text = vowelFinder.vowel;
        }
    }
}