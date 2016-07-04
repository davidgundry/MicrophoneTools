using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MicTools;

[RequireComponent(typeof(Text))]
public class SyllableCounter : MonoBehaviour {

    Text syllableText;
    MicrophoneInput mInput;

    void Start()
    {
        syllableText = GetComponent<Text>();
        mInput = GameObject.FindObjectOfType<MicrophoneInput>();
    }

	void Update ()
    {
        if (mInput != null)
            syllableText.text = mInput.syllableCount.ToString();
	}
}
