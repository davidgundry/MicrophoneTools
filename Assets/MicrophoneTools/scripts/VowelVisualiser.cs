using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(MicrophoneInput))]
[RequireComponent(typeof(FormantFinder))]
[RequireComponent(typeof(VowelFinder))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GLDebug))]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("MicrophoneTools/VowelVisualiser")]
public class VowelVisualiser : MonoBehaviour {

    private Text vowelText;
    private FormantFinder formantFinder;
    private VowelFinder vowelFinder;
    private Canvas canvas;

    private float halfCameraHeight;
    private float halfCameraWidth;

    private const int activationThreshold = 20;

    private float magnification = 10000;

    private const int zPos = 10;

	void Start ()
    {
        formantFinder = GetComponent<FormantFinder>();
        vowelFinder = GetComponent<VowelFinder>();
        halfCameraHeight = this.GetComponent<Camera>().orthographicSize;
        halfCameraWidth = this.GetComponent<Camera>().aspect * halfCameraHeight;
        Canvas canvas = GetComponent<Canvas>();

        vowelText = Instantiate(Resources.Load("vowel/VowelText", typeof(Text))) as Text;
        vowelText.transform.SetParent(transform, false);
        vowelText.transform.position = transform.position;
	}
	
	void Update ()
    {
        float[] spectrum = formantFinder.Spectrum;
        int formant = 0;
        Color color = Color.red;
        for (int i = 1; i < spectrum.Length - 1; i++)
        {
            if (formant < formantFinder.Formants.Length)
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

            GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth + i, transform.position.y - halfCameraHeight + 10, transform.position.z + zPos), new Vector3(transform.position.x - halfCameraWidth + i, transform.position.y - halfCameraHeight + spectrum[i] * magnification + 10, transform.position.z + zPos), color, 0, true);
        }

        // Draw Noise Line
        GLDebug.DrawLine(new Vector3(transform.position.x - halfCameraWidth, transform.position.y - halfCameraHeight + formantFinder.NoiseLevel * activationThreshold * magnification + 10, transform.position.z + zPos), new Vector3(transform.position.x + halfCameraWidth, transform.position.y - halfCameraHeight + formantFinder.NoiseLevel * activationThreshold * magnification + 10, transform.position.z + zPos), Color.blue, 0, true);

        vowelText.text = vowelFinder.vowel;


    }
}
