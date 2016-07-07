using UnityEngine;
using System.Collections;
using MicTools;
using System.IO;
using System.Collections.Generic;

public class Tester : MonoBehaviour {

    private MicrophoneInput microphoneInput;

    private Dictionary<FileInfo, int> audioFilesToTest;

	// Use this for initialization
	void Start ()
    {
        microphoneInput = GetComponent<MicrophoneInput>();
        StartCoroutine(RunTestCoroutine());
	}

    IEnumerator RunTestCoroutine()
    {
        //DirectoryInfo levelDirectoryPath = new DirectoryInfo(Application.dataPath + "/Resources/batchaudio/");
        //FileInfo[] fileInfo = levelDirectoryPath.GetFiles("*.*", SearchOption.AllDirectories);

        string[] files = { };
        TelemetryTools.Telemetry.Instance.KeyManager.ChangeKey();

        foreach (string file in files)
        {
            //if (file.Extension == ".wav")
           // {
                AudioClip newClip = Resources.Load("batchaudio/" + file) as AudioClip;//file.Name.Substring(0,file.Name.Length-4)) as AudioClip;
                if (newClip != null)
                {
                    newClip.LoadAudioData();
                    if (newClip != null)
                        Debug.Log(file + " " + MicrophoneInput.RunTest(newClip));

                    TelemetryTools.Telemetry.Instance.SendFrame();
                    TelemetryTools.Telemetry.Instance.SendStreamString("file", file);
                    TelemetryTools.Telemetry.Instance.SendStreamValue("syllables", MicrophoneInput.RunTest(newClip));
                    newClip.UnloadAudioData();
                    //audioFilesToTest.Add(file, MicrophoneInput.RunTest(newClip));
                    yield return null;// new WaitForSeconds(1f);
                }
                else
                {
                    Debug.Log("Null file: " + file);
                    TelemetryTools.Telemetry.Instance.SendFrame();
                    TelemetryTools.Telemetry.Instance.SendStreamString("file", file);
                    TelemetryTools.Telemetry.Instance.SendStreamString("error", "nullerror");
                    yield return null;
                }
          //  }
        }
        TelemetryTools.Telemetry.Instance.WriteEverything();
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
