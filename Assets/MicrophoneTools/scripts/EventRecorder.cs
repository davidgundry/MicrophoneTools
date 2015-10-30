using UnityEngine;
using System.Collections;
using System.IO;

public class EventRecorder : MonoBehaviour {

    private EventRecord[] buffer;
    private int bufferPos;
    private int bufferReadPos;

    public string saveDirectory = "MicrophoneTools";
    public string filesuffix = "";

    private FileStream fileStream;

    public bool recording = true;
    private bool paused = false;
    private bool audioPlaying;

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

        buffer[bufferPos] = new EventRecord(soundEvent);
        bufferPos = (bufferPos + 1) % buffer.Length;
    }

    public void OnGameEvent(string e)
    {


    }

	void Start ()
    {
        buffer = new EventRecord[64];
	}
	
	void Update ()
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
                EndWrite();
        }
	}


    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (fileStream != null)
                EndWrite();
            paused = true;
        }
        else
            paused = false;
    }

    void OnDisable()
    {
        if (fileStream != null)
            EndWrite();
    }

    void OnDestroy()
    {
        if (fileStream != null)
            EndWrite();
    }

    void OnApplicationQuit()
    {
        if (fileStream != null)
            EndWrite();
    }

    private void StartWrite()
    {
        Debug.Log("EventRecorder: Started write");

        string directory = MicrophoneRecorder.LocalFilePath(saveDirectory);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string filePath = MicrophoneRecorder.LocalFilePath(directory + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + filesuffix + ".json");
        fileStream = new FileStream(filePath, FileMode.Create);
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

            string[] lines = new string[dataLength];

            int i = 0;
            while ((bufferReadPos != bufferPos) && (i < dataLength))
            {
                lines[i] = buffer[bufferReadPos] + "\n";
                i++;
                bufferReadPos = (bufferReadPos + 1) % buffer.Length;
            }

            for (int j=0;j<lines.Length;j++)
            {
                byte[] bytes = StringToBytes(lines[j]);
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }
        else
            Debug.LogError("Attempted to write to fileStream but it is null!");
    }


    private static byte[] StringToBytes(string str)
    {
        byte[] bytes = new byte[str.Length * sizeof(char)];
        System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private void EndWrite()
    {
        if (fileStream != null)
        {
            fileStream.Close();
            fileStream = null;
            Debug.Log("MicrophoneRecorder: Completed write");
        }
        else
            Debug.LogError("MicrophoneRecorder: Attempted to write header but fileStream was null!");
    }
}
