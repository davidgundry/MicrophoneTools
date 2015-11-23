using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using BytesPerSecond = System.Single;
using Bytes = System.Int32;
using Megabytes = System.Int32;
using Ticks = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Int32;

namespace TelemetryTools
{
    public class Telemetry
    {
        private static Ticks startTicks = 0;
        private static SequenceID sequenceID = 0;

        // Local
        private const FilePath cacheDirectory = "cache";
        private const FilePath cacheListFilename = "cache.txt";
        private static FileInfo file;
        private static List<FilePath> cachedFiles;

        // Remote
        private const URL uploadURL = "http://localhost";
        private static WWW www;
        private static byte[] wwwData;
        private static SequenceID wwwSequenceID;

        // Buffers
        private const Bytes bufferSize = 1024;//1048576; //1MiB
        private static byte[] outboxBuffer1 = new byte[bufferSize];
        private static byte[] outboxBuffer2 = new byte[bufferSize];
        private static int bufferPos = 0;
        private static bool buffer1Active = true;
        private static bool offBufferFull = false;

        // Logging and Transfer Rate
        private static Ticks lastLoggingUpdate;

        private static Megabytes dataLogged;
        private static Bytes dataLoggedSinceUpdate;
        private static Bytes dataSavedToFileSinceUpdate;
        private static Bytes dataSentByHTTPSinceUpdate;

        private static BytesPerSecond loggingRate;
        private static BytesPerSecond HTTPPostRate;
        private static BytesPerSecond LocalFileSaveRate;


        public static void Start()
        {
            cachedFiles = new List<FilePath>();
            ReadLocalCacheList();
            startTicks = System.DateTime.Now.Ticks;

            for (int i = 0; i < outboxBuffer1.Length; i++) { outboxBuffer1[i] = 0; }
            for (int i = 0; i < outboxBuffer2.Length; i++) { outboxBuffer2[i] = 0; }

            SendEvent("TTStartTick", startTicks * 2);
        }

        public static void Update()
        {
            if (offBufferFull)
            {
                if (buffer1Active)
                    offBufferFull = !SendBuffer(outboxBuffer2);
                else
                    offBufferFull = !SendBuffer(outboxBuffer1);
            }
            else if (www != null)
                if (www.isDone)
                {
                    if (!((www.error == null) || (www.error == "")))
                        SaveDataOnSendFailure(wwwData, wwwSequenceID);

                    if (cachedFiles.Count > 0)
                        SendFromCache();
                }

            UpdateLogging();
            Debug.Log("Logging: " + Mathf.Round((loggingRate / 1024)) + " KiB/s    HTTP: " + (HTTPPostRate / 1024) + " KiB/s    File: " + (LocalFileSaveRate / 1024) + " KiB/s    Total: " + (dataLogged / 1048576) + " MiB    Cached Files: " + cachedFiles.Count);
        }

        private static void SendFromCache()
        {
            FilePath name = cachedFiles[0];
            cachedFiles.RemoveAt(0);

            FilePath directory = LocalFilePath(cacheDirectory);
            if (Directory.Exists(directory))
            {
                SequenceID seqID = 0;
                string[] separators = new string[1];
                separators[0] = ".";
                bool parsed = Int32.TryParse(name.Split(separators, 1, System.StringSplitOptions.None)[0], out seqID); ;

                if (parsed)
                {
                    Debug.Log("seqID " + seqID);
                    FilePath cacheFile = LocalFilePath(cacheDirectory + "/" + name);
                    byte[] bytes = System.IO.File.ReadAllBytes(cacheFile);
                    www = SendByHTTPPost(bytes);
                    wwwSequenceID = seqID;
                    wwwData = bytes;
                    System.IO.File.Delete(cacheFile);
                }
            }
        }

        private static void SaveDataOnSendFailure(byte[] data, SequenceID id)
        {
            Debug.Log("Send Data Error:" + www.error);
            WriteDataToFile(data, id, NewFileInfo(id));
        }

        public static void End()
        {
            WriteLocalCacheList();
        }

        private static FileInfo localCacheInfo()
        {
            FilePath directory = LocalFilePath(cacheDirectory);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            FilePath filePath = LocalFilePath(directory + "/" + cacheListFilename);
            return new FileInfo(filePath);
        }

        private static void ReadLocalCacheList()
        {
            FilePath directory = LocalFilePath(cacheDirectory);
            if (Directory.Exists(directory))
            {
                string[] lines = System.IO.File.ReadAllLines(LocalFilePath(cacheDirectory + "/" + cacheListFilename));
                cachedFiles = new List<FilePath>(lines);
            }
        }

        private static void WriteLocalCacheList()
        {
            FileStream fileStream = null;
            byte[] newLine = StringToBytes("\n");
            try
            {
                fileStream = localCacheInfo().Open(FileMode.Create);

                foreach (FilePath filename in cachedFiles)
                {
                    byte[] bytes = StringToBytes(filename);
                    fileStream.Write(bytes, 0, bytes.Length);
                    fileStream.Write(newLine, 0, newLine.Length);
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
        }

        private static void UpdateLogging()
        {
            dataLogged += dataLoggedSinceUpdate;

            BytesPerSecond bytePerSecond = 10000000 / Mathf.Max((System.DateTime.Now.Ticks - lastLoggingUpdate),1);
            loggingRate = bytePerSecond * dataLoggedSinceUpdate;
            HTTPPostRate = bytePerSecond * dataSentByHTTPSinceUpdate;
            LocalFileSaveRate = bytePerSecond * dataSavedToFileSinceUpdate;

            lastLoggingUpdate = System.DateTime.Now.Ticks;
            dataLoggedSinceUpdate = 0;
            dataSavedToFileSinceUpdate = 0;
            dataSentByHTTPSinceUpdate = 0;
        }

        public static void SendEvent(string name, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append((time - startTicks).ToString());
            sb.Append("\":\"");
            sb.Append(name);
            sb.Append("\"}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendEventID(byte id, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append((time - startTicks).ToString());
            sb.Append("\":\"");
            sb.Append(id.ToString());
            sb.Append("\"}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendKeyValue(string key, ValueType value, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString());
            sb.Append(",\"t\":");
            sb.Append((time - startTicks).ToString());
            sb.Append("}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendIDValue(byte id, ValueType value, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(id.ToString());
            sb.Append("\":");
            sb.Append(value.ToString());
            sb.Append(",\"t\":");
            sb.Append((time - startTicks).ToString());
            sb.Append("}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendStreamIDValue(byte id, ValueType value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(id.ToString());
            sb.Append("\":");
            sb.Append(value.ToString());
            sb.Append("}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendStreamKeyValue(string key, ValueType value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString());
            sb.Append("}");
            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendStreamIDValueBlock(byte id, ValueType[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(id.ToString());
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                sb.Append(",");
            }
            sb.Append("]}");

            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendStreamKeyValueBlock(string key, ValueType[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                sb.Append(",");
            }
            sb.Append("]}");

            BufferForSending(StringToBytes(sb.ToString()));
        }

        public static void SendStreamIDFloatBlock(byte id, float[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(id.ToString());
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                sb.Append(",");
            }
            sb.Append("]}");

            BufferForSending(StringToBytes(sb.ToString()));
        }


        public static void SendStreamKeyFloatBlock(string id, float[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"");
            sb.Append(id);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                sb.Append(",");
            }
            sb.Append("]}");

            BufferForSending(StringToBytes(sb.ToString()));
        }

        private static void BufferForSending(byte[] data)
        {
            dataLoggedSinceUpdate += data.Length;

            for (int i = 0; i < data.Length; i++)
            {
                if (buffer1Active)
                    outboxBuffer1[bufferPos] = data[i];
                else
                    outboxBuffer2[bufferPos] = data[i];

                bufferPos = (bufferPos + 1) % bufferSize;
                if (bufferPos == 0)
                {
                    if (offBufferFull)
                        throw new System.ArgumentException("Overflow local telemetry buffer, data overwritten");
                    buffer1Active = !buffer1Active;
                    offBufferFull = true;
                }
            }
        }

        private static bool SendBuffer(byte[] data)
        {
            if (www == null)
            {
                www = SendByHTTPPost(data);
                sequenceID++;
                return true;
            }
            else if (www.isDone)
            {
                if (!((www.error == null) || (www.error == "")))
                    SaveDataOnSendFailure(wwwData, wwwSequenceID);

                www = SendByHTTPPost(data);
                sequenceID++;
                return true;
            }

            if (!IsFileOpen(file))
            {
                file = NewFileInfo(sequenceID);
                if (WriteDataToFile(data, sequenceID, file))
                {
                    sequenceID++;
                    return true;
                }
            }

            char[] chars = new char[data.Length / sizeof(char)];
            System.Buffer.BlockCopy(data, 0, chars, 0, data.Length);
            Debug.Log("Could not save buffer: " + new string(chars));

            return false;
        }

        private static WWW SendByHTTPPost(byte[] data)
        {
            WWWForm form = new WWWForm();
            //form.AddField("time", (System.DateTime.Now.Ticks-startTicks) + "");
            form.AddBinaryData("telemetry", data, sequenceID + ".telemetry");
            WWW w = new WWW(uploadURL, form);
            
            wwwData = new byte[data.Length];
            System.Buffer.BlockCopy(data, 0, wwwData, 0, data.Length);

            wwwSequenceID = sequenceID;
            dataSentByHTTPSinceUpdate += data.Length;
            return w;
        }

        private static bool WriteDataToFile(byte[] data, int id, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);
                fileStream.Write(data, 0, data.Length);
                cachedFiles.Add(file.Name);
            }
            catch (IOException e)
            {
                Debug.Log(e.Message);
                return false;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }

            dataSavedToFileSinceUpdate += data.Length;
            return true;
        }

        private static FileInfo NewFileInfo(int sequenceID)
        {
            FilePath directory = LocalFilePath(cacheDirectory);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            FilePath filePath = LocalFilePath(directory + "/" + sequenceID + "." + (System.DateTime.Now.Ticks - startTicks) + ".telemetry");
            return new FileInfo(filePath);
        }

        private static bool IsFileOpen(FileInfo file)
        {
            if (file != null)
            {
                FileStream stream = null;

                try
                {
                    stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    return true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

        private static FilePath LocalFilePath(FilePath filename)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                FilePath path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(Path.Combine(path, "Documents"), filename);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                FilePath path = Application.persistentDataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(path, filename);
            }
            else
            {
                FilePath path = Application.dataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(path, filename);
            }
        }

        private static byte[] StringToBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
