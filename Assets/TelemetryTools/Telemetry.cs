#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

#define POSTENABLED

using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

#if LOCALSAVEENABLED
using System.IO;
#endif

using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Ticks = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using KeyID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;
using UniqueKey = System.String;



namespace TelemetryTools
{

    public static class Event
    {
        public const string TelemetryStart = "TTStart";
        public const string Frame = "Frame";
        public const string ApplicationPause = "AppPause";
        public const string ApplicationUnpause = "AppUnpause";
        public const string ApplicationQuit = "AppQuit";
    }

    public static class Stream
    {
        public const string FrameTime = "FT";
        public const string DeltaTime = "DT";
        public const string LostData = "LD";
    }

    public static class UserDataKeys
    {
        public const string RequestTime = "RequestTime";
        public const string Platform = "Platform";
        public const string WebPlayerURL = "WebPlayerURL";
        public const string Version = "Version";
        public const string UnityVersion = "UnityVersion";
        public const string Genuine = "Genuine";
    }

    public class Telemetry
    {
        private static Telemetry instance;
        public static Telemetry Instance
        {
            get
            {
                if (instance == null)
                    SelfStart();
                return instance;
            }
        }

        private readonly Ticks startTicks = 0;
        private readonly SessionID sessionID = 0;
        private SequenceID sequenceID = 0;
        private FrameID frameID = 0;

        private const FilePath fileExtension = "telemetry";

        private UniqueKey[] keys;
        private uint usedKeys;
        private KeyID currentKeyID;

#if LOCALSAVEENABLED
        // Local
        private const FilePath cacheDirectory = "cache";
        private const FilePath cacheListFilename = "cache.txt";
        private List<FilePath> cachedFilesList;
#endif


#if POSTENABLED 
        // Remote
        //private UniqueKey uniqueKey;
        private bool httpPostEnabled = false;
        private readonly URL uploadURL;
        private readonly URL keyServer;
        private WWW keywww;
        private WWW www;
        private byte[] wwwData;
        private SequenceID wwwSequenceID;
        private SessionID wwwSessionID;
        private bool wwwBusy = false;
        private UniqueKey wwwKey;
        private KeyID wwwKeyID;
#endif
        private readonly Bytes minSendingThreshold;
        private const Bytes defaultMinSendingThreshold = 1024;

        // Buffers
        private const Bytes defaultBufferSize = 1048576;
        private const Bytes defaultFrameBufferSize = 1024*128;
        private readonly Bytes bufferSize;
        private byte[] outboxBuffer1;
        private byte[] outboxBuffer2;
        private int bufferPos = 0;
        private bool buffer1Active = true;
        private bool offBufferFull = false;
        private byte[] frameBuffer;
        private int frameBufferPos = 0;
        private Bytes lostData = 0;
        public Bytes LostData { get { return lostData; } }

        // Logging and Transfer Rate
        private Ticks lastLoggingUpdate;

        private Megabytes dataLogged;
        public Megabytes DataLogged { get { return dataLogged; } }
        private Bytes dataLoggedSinceUpdate;
        public Bytes DataLoggedSinceUpdate { get { return dataLoggedSinceUpdate; } }
        private Bytes dataSavedToFileSinceUpdate;
        public Bytes DataSavedToFileSinceUpdate { get { return dataSavedToFileSinceUpdate; } }
        private Bytes dataSentByHTTPSinceUpdate;
        public Bytes DataSentByHTTPSinceUpdate { get { return dataSentByHTTPSinceUpdate; } }

        private BytesPerSecond loggingRate;
        public BytesPerSecond LoggingRate { get { return loggingRate; } }
        private BytesPerSecond httpPostRate;
        public BytesPerSecond HTTPPostRate { get { return httpPostRate; } }
        private BytesPerSecond localFileSaveRate;
        public BytesPerSecond LocalFileSaveRate { get { return LocalFileSaveRate; } }

        public Telemetry(URL uploadURL, URL keyServer, Bytes bufferSize = defaultBufferSize, Bytes frameBufferSize = defaultFrameBufferSize, Bytes minSendingThreshold = defaultMinSendingThreshold)
        {
            this.bufferSize = bufferSize;
            this.minSendingThreshold = minSendingThreshold;

            outboxBuffer1 = new byte[bufferSize];
            outboxBuffer2 = new byte[bufferSize];
            frameBuffer = new byte[frameBufferSize];

            Array.Clear(outboxBuffer1, 0, outboxBuffer1.Length);
            Array.Clear(outboxBuffer2, 0, outboxBuffer2.Length);
            Array.Clear(frameBuffer, 0, frameBuffer.Length);

#if LOCALSAVEENABLED
            cachedFilesList = ReadStringsFromFile(GetFileInfo(cacheDirectory, cacheListFilename));
#endif

            sessionID = (SessionID)PlayerPrefs.GetInt("sessionID");
            PlayerPrefs.SetInt("sessionID", (int)sessionID+1);
            PlayerPrefs.Save();

            keys = new string[0];
#if POSTENABLED
            this.uploadURL = uploadURL;
            this.keyServer = keyServer;

            //uniqueKey = PlayerPrefs.GetString("key");
            int numKeys = 0;
            if (Int32.TryParse(PlayerPrefs.GetString("numkeys"), out numKeys))
                keys = new string[numKeys];

            int usedKeysParsed;
            if (Int32.TryParse(PlayerPrefs.GetString("usedkeys"), out usedKeysParsed))
            {
                usedKeys = (uint) usedKeysParsed;
            }
            int cki;
            if (Int32.TryParse(PlayerPrefs.GetString("currentkeyid"), out cki))
                currentKeyID = (UInt32)cki;
            else
            {
                currentKeyID = 0;
                PlayerPrefs.SetString("currentkeyid", currentKeyID.ToString());
            }

            if (usedKeys == 0)
            {
                usedKeys = 1;
                PlayerPrefs.SetString("usedkeys", usedKeys.ToString());
            }
            PlayerPrefs.Save();
            
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = PlayerPrefs.GetString("key" + i);
                if (!string.IsNullOrEmpty(keys[i]))
                    httpPostEnabled = true;
            }
            //if (!httpPostEnabled) //Should be handled in update as with any missing key
            //    keywww = RequestUniqueKey(this.keyServer, GetUserData());
#endif

            startTicks = System.DateTime.UtcNow.Ticks;

            SendFrame();
            SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.UtcNow.Ticks);
            SendKeyValuePair(Event.TelemetryStart, System.DateTime.UtcNow.ToString("u"));


            Debug.Log("Persistant Data Path: " + Application.persistentDataPath);
        }

        private static void SelfStart()
        {
			instance = new Telemetry(uploadURL: "127.0.0.1", keyServer: "127.0.0.1");//"http://192.168.43.101/ttsrv/import.php", keyServer: "http://192.168.43.101/ttsrv/key.php");
        }

        private static KeyValuePair<UserDataKey, string>[] GetUserData()
        {
            List<KeyValuePair<UserDataKey, string>> userData = new List<KeyValuePair<UserDataKey, string>>();
            userData.Add(new KeyValuePair<UserDataKey, string>(UserDataKeys.Platform, Application.platform.ToString()));
            userData.Add(new KeyValuePair<UserDataKey, string>(UserDataKeys.Version, Application.version));
            userData.Add(new KeyValuePair<UserDataKey, string>(UserDataKeys.UnityVersion, Application.unityVersion));
            userData.Add(new KeyValuePair<UserDataKey, string>(UserDataKeys.Genuine, Application.genuine.ToString()));
            if (Application.isWebPlayer)
                userData.Add(new KeyValuePair<UserDataKey, string>(UserDataKeys.WebPlayerURL, Application.absoluteURL));

            return userData.ToArray();
        }

        public void ChangeToNewKey()
        {
            currentKeyID = usedKeys;
            usedKeys++;
            PlayerPrefs.SetString("currentkeyid", currentKeyID.ToString());
            PlayerPrefs.SetString("usedkeys", usedKeys.ToString());
            PlayerPrefs.Save();

            SendFrame();

            if (offBufferFull)
            {
                if (buffer1Active)
                    offBufferFull = !SendBuffer(RemoveTrailingNulls(outboxBuffer2));
                else
                    offBufferFull = !SendBuffer(RemoveTrailingNulls(outboxBuffer1));
            }

            SendBuffer(GetDataInActiveBuffer());
            bufferPos = 0;
        }

        public static void Update() { Instance.UpdateP(); }

        private void UpdateP()
        {
            if (offBufferFull)
            {
                if (buffer1Active)
                    offBufferFull = !SendBuffer(RemoveTrailingNulls(outboxBuffer2));
                else
                    offBufferFull = !SendBuffer(RemoveTrailingNulls(outboxBuffer1));
            }

#if POSTENABLED
            if (keywww != null)
            {
                UniqueKey newKey = null;
                bool? success = GetReturnedKey(ref keywww, ref httpPostEnabled, ref newKey);
                if (success != null)
                {
                    if (success == true)
                    {
                        //string[] keys = new string[1];
                        //keys[0] = uniqueKey;
                        Array.Resize(ref keys, keys.Length + 1);
                        keys[keys.Length - 1] = newKey;
                        PlayerPrefs.SetString("key" + (keys.Length - 1), newKey);
                        PlayerPrefs.SetString("numkeys", keys.Length.ToString());
                        PlayerPrefs.Save();
                    }
                    keywww.Dispose();
                    keywww = null;
                }
            }
            else if (usedKeys > keys.Length)
            {
                keywww = RequestUniqueKey(this.keyServer, GetUserData());
            }

            SaveDataOnWWWErrorIfWeCan();

            if (httpPostEnabled)
            {
    #if LOCALSAVEENABLED
                if ((!offBufferFull) && (!wwwBusy))
                {
                    if (cachedFilesList.Count > 0)
                    {
                        byte[] data;
                        SessionID snID;
                        SequenceID sqID;
                        KeyID keyID;
                        if (LoadFromCacheFile(cacheDirectory, cachedFilesList[0], out data, out snID, out sqID, out keyID))
                            if ((data.Length > 0) && (snID != null) && (sqID != null) && (keyID != null)) // key here could be empty because it was not known when the file was saved
                            {
                                if (keyID < keys.Length)
                                {
                                    SendByHTTPPost(data, snID, sqID, fileExtension, keys[(uint)keyID], keyID, uploadURL, ref www, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                                    File.Delete(GetFileInfo(cacheDirectory, cachedFilesList[0]).FullName);
                                    cachedFilesList.RemoveAt(0);
                                }
                                else
                                    Debug.LogWarning("KeyID " + keyID + " does not match an actual key");
                            }
                    }
                }
    #endif

                if ((!offBufferFull) && (!wwwBusy))
                {
                    if (bufferPos > minSendingThreshold)
                    {
                        SendBuffer(GetDataInActiveBuffer());
                        bufferPos = 0;
                    }
                }
            }
#endif
            UpdateLogging();
        }

        public static void Stop() { Instance.StopP();}

        private void StopP()
        {
            SendFrame();
            byte[] dataInBuffer = GetDataInActiveBuffer();
            bool savedBuffer = false;

#if LOCALSAVEENABLED
            WriteCacheFile(dataInBuffer, sessionID, sequenceID, currentKeyID);
            savedBuffer = true;
#endif

            bufferPos = 0;
            sequenceID++;

#if POSTENABLED
            SaveDataOnWWWErrorIfWeCan();

            if (www != null)
            {
    #if LOCALSAVEENABLED
                if (!www.isDone)
                    WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID);
    #endif
                DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy);
            }

            if (httpPostEnabled)
            {
                if (currentKeyID != null)
                    if (currentKeyID < keys.Length)
                    {
                        WWW stopwww = null;
                        SendByHTTPPost(dataInBuffer, sessionID, sequenceID, fileExtension, keys[(uint) currentKeyID], currentKeyID, uploadURL, ref stopwww, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                        savedBuffer = true;
                    }
            }
#endif

            if (!savedBuffer)
                lostData += (uint) dataInBuffer.Length;
        }


        private void UpdateLogging()
        {
            dataLogged += dataLoggedSinceUpdate;

            BytesPerSecond bytePerSecond = 10000000 / Mathf.Max((System.DateTime.UtcNow.Ticks - lastLoggingUpdate),1);
            loggingRate = bytePerSecond * dataLoggedSinceUpdate;
            httpPostRate = bytePerSecond * dataSentByHTTPSinceUpdate;
            localFileSaveRate = bytePerSecond * dataSavedToFileSinceUpdate;

            lastLoggingUpdate = System.DateTime.UtcNow.Ticks;
            dataLoggedSinceUpdate = 0;
            dataSavedToFileSinceUpdate = 0;
            dataSentByHTTPSinceUpdate = 0;
        }

        private bool SendBuffer(byte[] data)
        {
#if POSTENABLED
            if (httpPostEnabled)
            {
                SaveDataOnWWWErrorIfWeCan();

                if (!wwwBusy)
                {
                    if (currentKeyID != null)
                        if (currentKeyID < keys.Length)
                        {
                            SendByHTTPPost(data, sessionID, sequenceID, fileExtension, keys[(uint) currentKeyID], currentKeyID, uploadURL, ref www, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                            dataSentByHTTPSinceUpdate += (uint)data.Length;
                            sequenceID++;
                            return true;
                        }
                }
            }
#endif
#if LOCALSAVEENABLED
            if (WriteCacheFile(data, sessionID, sequenceID, currentKeyID))
            {
                sequenceID++;
                return true;
            }
#endif

            Debug.LogWarning("Could not deal with buffer: " + BytesToString(data));

            return false;
        }

        private void BufferData(byte[] data, bool newFrame = false)
        {
            dataLoggedSinceUpdate += (uint) data.Length;

            if (newFrame)
            {
                if (frameID != 0)
                {
                    byte[] endFrame = StringToBytes("},");
                    System.Buffer.BlockCopy(endFrame, 0, frameBuffer, frameBufferPos, endFrame.Length);
                    frameBufferPos += endFrame.Length;
                }

                if (frameBufferPos + bufferPos > bufferSize)
                {
                    if (offBufferFull)
                    {
                        Debug.LogWarning("Overflow local telemetry buffer, data overwritten");
                        if (buffer1Active)
                            lostData += (uint) RemoveTrailingNulls(outboxBuffer2).Length;
                        else
                            lostData += (uint) RemoveTrailingNulls(outboxBuffer1).Length;
                    }

                    if (buffer1Active)
                        Array.Clear(outboxBuffer1, bufferPos, outboxBuffer1.Length - bufferPos);
                    else
                        Array.Clear(outboxBuffer2, bufferPos, outboxBuffer2.Length - bufferPos);

                    buffer1Active = !buffer1Active;
                    offBufferFull = true;
                    bufferPos = 0;
                }

                if (buffer1Active)
                    System.Buffer.BlockCopy(frameBuffer, 0, outboxBuffer1, bufferPos, frameBufferPos);
                else
                    System.Buffer.BlockCopy(frameBuffer, 0, outboxBuffer2, bufferPos, frameBufferPos);
                bufferPos += frameBufferPos;
                frameBufferPos = 0;

                frameID++;
            }

            if (frameBufferPos + data.Length < frameBuffer.Length)
            {
                System.Buffer.BlockCopy(data, 0, frameBuffer, frameBufferPos, data.Length);
                frameBufferPos += data.Length;
            }
            else
            {
                Debug.LogWarning("Overflow frame buffer, data lost");
                lostData += (uint) data.Length;
            }
        }

        private long GetTimeFromStart()
        {
            return System.DateTime.UtcNow.Ticks - startTicks;
        }

        private byte[] GetDataInActiveBuffer()
        {
            byte[] partBuffer = new byte[bufferPos];
            if (bufferPos > 0)
            {
                if (buffer1Active)
                    System.Buffer.BlockCopy(outboxBuffer1, 0, partBuffer, 0, partBuffer.Length);
                else
                    System.Buffer.BlockCopy(outboxBuffer2, 0, partBuffer, 0, partBuffer.Length);
            }
            return partBuffer;
        }

        public static string GetPrettyLoggingRate() { return Instance.GetPrettyLoggingRateP(); }

        private string GetPrettyLoggingRateP()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Log Input: ");
            sb.Append(Mathf.Round((loggingRate / 1024)));
            sb.Append(" KiB/s");

#if POSTENABLED
           sb.Append("    HTTP: ");
            sb.Append(Mathf.Round((httpPostRate / 1024)));
            sb.Append(" KiB/s");
#endif
#if LOCALSAVEENABLED
            sb.Append("    File: ");
            sb.Append(Mathf.Round((localFileSaveRate / 1024)));
            sb.Append(" KiB/s");
#endif
            sb.Append("    Total: ");
            sb.Append((dataLogged / 1024));
            sb.Append(" KiB");
#if LOCALSAVEENABLED
            sb.Append("    Cached Files: ");
            if (cachedFilesList != null)
                sb.Append(cachedFilesList.Count);
            else
                sb.Append("0");
#endif

            sb.Append("    Lost Data: ");
            sb.Append((lostData / 1024));
            sb.Append(" KiB");

            return sb.ToString();
        }

        public void SendFrame()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"id\":");
            sb.Append(frameID);
            sb.Append("");
            BufferData(StringToBytes(sb.ToString()), newFrame: true);
        }

        public void SendEvent(string name, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append((time - startTicks).ToString());
            sb.Append("\":\"");
            sb.Append(name);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }


        public void SendEvent(string name)
        {
            SendEvent(name, System.DateTime.UtcNow.Ticks);
        }

        public void SendKeyValuePair(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value, Ticks time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":{\"v\":");
            sb.Append(value.ToString());
            sb.Append(",\"t\":");
            sb.Append((time - startTicks).ToString());
            sb.Append("}");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value)
        {
            SendValue(key, value, System.DateTime.UtcNow.Ticks);
        }

        public void SendStreamString(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }


        public void SendStreamValue(string key, ValueType value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString());
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, byte[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, short[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, int[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public static void SendStreamValueBlock(string key, long[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i<values.Length-1)
                    sb.Append(",");
            }
            sb.Append("]");

            Instance.BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, float[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, double[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendByteDataBase64(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(data.Length);
            sb.Append(",");
            sb.Append(System.Convert.ToBase64String(data));
            sb.Append("\"");

            BufferData(StringToBytes(sb.ToString()));
        }

        //Looking at the Mongo spec, it seems that it doesn't support this sort of encoding, use SendByteDataBase64 instead.
        public void SendByteDataBinary(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            byte[] b1 = StringToBytes(sb.ToString());

            byte[] b2 = BitConverter.GetBytes((System.Int32) data.Length);
            byte nullByte = (byte) 0;
            byte[] b3 = StringToBytes("\"");

            byte[] output = new byte[data.Length + b1.Length + b2.Length + 1 + b3.Length];
            System.Buffer.BlockCopy(b1,0,output,0,b1.Length);
            System.Buffer.BlockCopy(b2,0,output,b1.Length,b2.Length);
            output[b1.Length] = nullByte;
            System.Buffer.BlockCopy(data,0,output,b1.Length+b2.Length+1,data.Length);
            System.Buffer.BlockCopy(b3,0,output,b1.Length+b2.Length+data.Length+1,b3.Length);

            BufferData(output);
        }

#if POSTENABLED
        private static void SendByHTTPPost( byte[] data,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            FilePath fileExtension,
                                            UniqueKey uniqueKey,
                                            KeyID uniqueKeyID,
                                            URL uploadURL,
                                            ref WWW www,
                                            out byte[] wwwData,
                                            out SequenceID wwwSequenceID,
                                            out SessionID wwwSessionID,
                                            out bool wwwBusy,
                                            out UniqueKey wwwKey,
                                            out KeyID wwwKeyID)
        {

            WWWForm form = new WWWForm();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(sessionID);
            sb.Append(".");
            sb.Append(sequenceID);
            sb.Append(".");
            sb.Append(fileExtension);
            form.AddField("key", uniqueKey);
            form.AddField("session", sessionID.ToString());
            form.AddBinaryData(fileExtension, data, sb.ToString());
            
            www = new WWW(uploadURL, form);
            wwwBusy = true;
            wwwData = new byte[data.Length];
            System.Buffer.BlockCopy(data, 0, wwwData, 0, data.Length);
            wwwSequenceID = sequenceID;
            wwwSessionID = sessionID;
            wwwKey = uniqueKey;
            wwwKeyID = uniqueKeyID;
        }

        private static bool HandleWWWErrors(ref WWW www, ref byte[] wwwData, ref SessionID wwwSessionID, ref SequenceID wwwSequenceID, ref bool wwwBusy)
        {
            if (www != null)
            {
                if ((www.isDone) && (!string.IsNullOrEmpty(www.error)))
                {
                    Debug.LogWarning("Send Data Error: " + www.error);
                    return false;
                }
                else if (www.isDone)
				{
					if (!string.IsNullOrEmpty(www.text.Trim()))
					{
						Debug.LogWarning ("Response from server: " + www.text);
					}
                    DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy);
				}
            }
            return true;
        }

        private static WWW RequestUniqueKey(URL keyServer, KeyValuePair<string, string>[] userData)
        {
            WWWForm form = new WWWForm();

			form.AddField(UserDataKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);

            return new WWW(keyServer, form);
        }


        private static bool? GetReturnedKey(ref WWW keywww, ref bool httpPostEnabled, ref string uniqueKey)
        {
            if (keywww != null)
                if (keywww.isDone)
                {
                    if (string.IsNullOrEmpty(keywww.error))
                    {
						if (keywww.text.StartsWith("key:"))
						{
							uniqueKey = keywww.text.Substring(4);
							Debug.Log("Key retrieved: " + uniqueKey);
							httpPostEnabled = true;
							return true;
						}
						else
						{
							Debug.LogWarning("Invalid key retrieved: " +  keywww.text);
							httpPostEnabled = false;
							return false;
						}
                    }
                    else
                    {
                        Debug.LogWarning("Error connecting to key server");
                        httpPostEnabled = false;
						return false;
                    }
                }
            return null;
        }

        private static void DisposeWWW(ref WWW www, ref byte[] wwwData, ref SessionID wwwSessionID, ref SequenceID wwwSequenceID, ref bool wwwBusy)
        {
            wwwBusy = false;
            wwwData = new byte[0];
            wwwSessionID = null;
            wwwSequenceID = null;
        }
#endif

#if LOCALSAVEENABLED
        private static void WriteDataToFile(byte[] data, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);
                fileStream.Write(data, 0, data.Length);
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

        private static bool LoadFromCacheFile(FilePath directory, FilePath filename, out byte[] data, out SessionID sessionID, out SequenceID sequenceID, out KeyID keyID)
        {
            data = new byte[0];
            sessionID = null;
            sequenceID = null;
            keyID = null;
            directory = LocalFilePath(directory);
            if (Directory.Exists(directory))
            {
                uint sqID = 0;
                uint snID = 0;
                uint kID = 0;
                string[] separators = new string[1];
                separators[0] = ".";
                string[] fileDetails = filename.Split(separators, 3, System.StringSplitOptions.None);
                bool parsed = UInt32.TryParse(fileDetails[0], out snID) && UInt32.TryParse(fileDetails[1], out sqID) && UInt32.TryParse(fileDetails[2], out kID);

                if (parsed)
                {
                    sessionID = snID;
                    sequenceID = sqID;
                    keyID = kID;

                    FilePath cacheFile = directory + "/" + filename;
                    if (File.Exists(cacheFile))
                    {
                        data = File.ReadAllBytes(cacheFile);
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("Attempted to load from from non-existant cache file: " + cacheFile);
                        return false;
                    }
                }
            }
            return false;
        }


        private static void WriteStringsToFile(string[] stringList, FileInfo file)
        {
            FileStream fileStream = null;
            byte[] newLine = StringToBytes("\n");
            try
            {
                fileStream = file.Open(FileMode.Create);

                foreach (FilePath str in stringList)
                {
                    byte[] bytes = StringToBytes(str);
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


        private static List<FilePath> ReadStringsFromFile(FileInfo file)
        {
            List<FilePath> list = new List<FilePath>();

            if (file.Exists)
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = BytesToString(bytes);
                    string[] separators = new string[1];
                    separators[0] = "\n";
                    string[] lines = s.Split(separators, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                    list = new List<FilePath>(lines);
                    return list;
                }
                catch (IOException ex)
                {
                    return list;
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
            Debug.LogWarning("Attempted to read strings from non-existant file: " + file.FullName);
            return list;
        }

        private static T ReadValueFromFile<T>(FileInfo file)
        {
            if (file.Exists)
            {

                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = BytesToString(bytes);

                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(s);

                }
                catch
                {
                    return default(T);
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

            Debug.LogWarning("Attempted to read value from non-existant file: " + file.FullName);

            return default(T);
        }

        private static void WriteValueToFile(ValueType value, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);

                byte[] bytes = StringToBytes(value.ToString() + "\n");
                fileStream.Write(bytes, 0, bytes.Length);
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

        private static FileInfo GetFileInfo(FilePath directory, FilePath filename)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            FilePath filePath = LocalFilePath(directory + "/" + filename);
            return new FileInfo(filePath);
        }

        private static FileInfo GetFileInfo(FilePath directory,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            KeyID keyID,
                                            long time,
                                            FilePath fileExtension)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(sessionID);
            sb.Append(".");
            sb.Append(sequenceID);
            sb.Append(".");
            sb.Append(keyID);
            sb.Append(".");
            sb.Append(time);
            sb.Append(".");
            sb.Append(fileExtension);

            FilePath filePath = directory + "/" +sb.ToString();
            return new FileInfo(filePath);
        }

        private static FilePath LocalFilePath(FilePath filename)
        {
           /* if (Application.platform == RuntimePlatform.IPhonePlayer)
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
            {*/
                //TODO: check what's going on here.
                FilePath path = Application.persistentDataPath + "/";
                //path = path.Substring(0, path.LastIndexOf('/')+1);
                //return path + filename;
                return Path.Combine(path, filename);
            //}
        }

        private bool WriteCacheFile(byte[] data, SessionID sessionID, SequenceID sequenceID, KeyID key)
        {
                FileInfo file = GetFileInfo(cacheDirectory, sessionID, sequenceID, key, GetTimeFromStart(), fileExtension);
                if ((!File.Exists(file.FullName)) || (!IsFileOpen(file)))
                {
                    WriteDataToFile(data, file);
                    dataSavedToFileSinceUpdate += (uint) data.Length;

                    cachedFilesList.Add(file.Name);
                    //TODO: Append rather than rewrite everything
                    WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                    return true;
                }
            return false;
        }

#endif

#if POSTENABLED
        private void SaveDataOnWWWErrorIfWeCan()
        {
            if ((www != null) && (wwwBusy))
            {
                if (!HandleWWWErrors(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy))
                {
#if LOCALSAVEENABLED
                    if (WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID))
                        DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy);
#else
                    lostData += (uint) wwwData.Length;
                    DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy);
#endif
                }
            }
        }
#endif

        private static byte[] StringToBytes(string str)
        {
            //byte[] bytes = new byte[str.Length * sizeof(char)];
            //System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);

            return Encoding.ASCII.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
            //return d.GetChars(bytes, 0, bytes.Length);
            
            //char[] chars = new char[bytes.Length / sizeof(char)];
            //System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            //return new string(chars);
        }

        private static byte[] RemoveTrailingNulls(byte[] data)
        {
            int i=data.Length-1;
            while (data[i] == 0)
                i--;
            byte[] trimmed = new byte[i+2];
            System.Buffer.BlockCopy(data,0,trimmed,0,trimmed.Length);
            return trimmed;
        }
    }
}
