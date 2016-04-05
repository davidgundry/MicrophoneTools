using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

namespace TelemetryTools
{
    public class DataManagementPane : MonoBehaviour {

        public Text infoText;
        public Text URLInputText;
        private TelemetryMonitor telemetryMonitor;

	    // Use this for initialization
	    void Start () {
            Screen.orientation = ScreenOrientation.Portrait;
            telemetryMonitor = GameObject.FindObjectOfType<TelemetryMonitor>();
	    }

        void OnEnable()
        {
            infoText.text = MakeText();
        }

	    // Update is called once per frame
	    void Update () {
            if (TelemetryTools.Telemetry.Instance.HTTPPostEnabled)
                infoText.text = MakeText();
	    }

        public void Quit()
        {
            Application.LoadLevel("launcher");
        }

        public void SetURL()
        {
            TelemetryTools.Telemetry.Instance.UploadURL = URLInputText.text + "/import.php";
            TelemetryTools.Telemetry.Instance.KeyManager.KeyServer = URLInputText.text + "/key.php";
            TelemetryTools.Telemetry.Instance.UserDataURL = URLInputText.text + "/userdata.php";
            infoText.text = MakeText();
            PlayerPrefs.SetString("URL", URLInputText.text);
        }

        private string MakeText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP Post Enabled ");
            sb.Append(TelemetryTools.Telemetry.Instance.HTTPPostEnabled.ToString());
            sb.Append("\nFiles ");
            sb.Append(TelemetryTools.Telemetry.Instance.CachedFiles.ToString());
            sb.Append("\nUser Data Files ");
            sb.Append(TelemetryTools.Telemetry.Instance.UserDataFiles.ToString());
            sb.Append("\nKeys Used ");
            sb.Append(TelemetryTools.Telemetry.Instance.KeyManager.NumberOfUsedKeys.ToString());
            sb.Append("\nKeys Fetched ");
            sb.Append(TelemetryTools.Telemetry.Instance.KeyManager.NumberOfKeys.ToString());
            for (int i = 0; i < TelemetryTools.Telemetry.Instance.KeyManager.Keys.Length; i++)
                sb.Append("\n" + i + ":" + TelemetryTools.Telemetry.Instance.KeyManager.Keys[i]);

            sb.Append("\nUpload URL ");
            sb.Append(TelemetryTools.Telemetry.Instance.UploadURL);
            sb.Append("\nKey Server ");
            sb.Append(TelemetryTools.Telemetry.Instance.KeyManager.KeyServer);
            sb.Append("\nUser Data URL ");
            sb.Append(TelemetryTools.Telemetry.Instance.UserDataURL);

            sb.Append("\nHTTP Requests Sent ");
            sb.Append(TelemetryTools.ConnectionLogger.Instance.TotalHTTPRequestsSent);
            sb.Append("\nHTTP Success ");
            sb.Append(TelemetryTools.ConnectionLogger.Instance.TotalHTTPSuccess);
            sb.Append("\nHTTP Errors ");
            sb.Append(TelemetryTools.ConnectionLogger.Instance.TotalHTTPErrors);
            return sb.ToString();
        }

        public void UploadButtonToggle()
        {
            telemetryMonitor.gameObject.SetActive(true);
            TelemetryTools.Telemetry.Instance.HTTPPostEnabled = !TelemetryTools.Telemetry.Instance.HTTPPostEnabled;
            infoText.text = MakeText();
        }
    }
}