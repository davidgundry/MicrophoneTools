using UnityEngine;
using System.Collections;

public class Launcher : MonoBehaviour {

    public TelemetryTools.TelemetryMonitor telemetryMonitor;

    void Awake()
    {
        DontDestroyOnLoad(this);

        if (FindObjectsOfType(GetType()).Length > 1)
        {
            Destroy(gameObject);
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (TelemetryTools.Telemetry.Exists)
        {
            TelemetryTools.Telemetry.Instance.KeyManager.ChangeKey();
            telemetryMonitor.gameObject.SetActive(true);
        }

        Application.LoadLevel("data-management");
    }

    void OnLevelWasLoaded(int level)
    {
        if (level == 0)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Application.LoadLevel("wordplane");
        }
    }
}
